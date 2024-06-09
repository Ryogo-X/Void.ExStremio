using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Providers.Media.HdRezka {
    public class HdRezkaConfig {
        public const string CONFIG_HOST_URL_KEY = "HDREZKA_HOST_URL";

        public Uri HostUri { get; }

        public HdRezkaConfig(Uri hostUri) {
            HostUri = hostUri;
        }
    }

    class HdRezkaCdnProvider : MediaProviderBase, IMediaProvider, ICustomIdProvider {
        const string PREFIX = "hdr";
        const string IMDB_PREFIX = "tt";

        public override string ServiceName {
            get { return "HdRezka"; }
        }

        readonly HdRezkaApi apiClient;

        readonly TimeSpan DEFAULT_EXPIRATION = TimeSpan.FromMinutes(8 * 60);
        readonly string CACHE_KEY_ITEM_EXT_ID;
        readonly string CACHE_KEY_ITEM_METADATA;
        readonly string CACHE_KEY_STREAMS;

        readonly HdRezkaConfig config;

        public HdRezkaCdnProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, HdRezkaConfig config) : base(httpClientFactory, cache) {
            this.config = config;
            this.apiClient = new HdRezkaApi(config.HostUri, GetHttpClient);

            CACHE_KEY_ITEM_EXT_ID = $"{ServiceName}:ITEM:EXT:ID:[id]";
            CACHE_KEY_ITEM_METADATA = $"{ServiceName}:ITEM:META:[uri]";
            CACHE_KEY_STREAMS = $"{ServiceName}:STREAMS:[id]:[season]:[episode]";
        }

        #region ICustomIdProvider
        public bool CanGetCustomId(ExtendedMeta meta) {
            return meta.Id?.StartsWith(IMDB_PREFIX) == true;
        }

        public async Task<CustomIdResult[]> GetCustomIds(ExtendedMeta meta) {
            if (!CanGetCustomId(meta)) { throw new NotSupportedException($"{ServiceName} cannot get custom id for meta.id: {meta.Id}"); }

            var ckId = CACHE_KEY_ITEM_EXT_ID.Replace("[id]", meta.Id);
            var customIds = cache.Get<CustomIdResult[]>(ckId);
            if (customIds?.Any() != true) {
                var localizedTitle = meta.LocalizedTitles.FirstOrDefault(x => x.LangCode == "ru")?.Title;
                if (!string.IsNullOrWhiteSpace(localizedTitle)) {
                    customIds = await GetCustomIds(localizedTitle, meta.Id, meta.GetYear());
                }
                if (customIds?.Any() != true) {
                    customIds = await GetCustomIds(meta.Name, meta.Id, meta.GetYear());
                }
            }

            return customIds;
        }

        async Task<CustomIdResult[]> GetCustomIds(string searchTitle, string imdbId, int? year) {
            var items = await apiClient.Search(searchTitle);
            var matchedItems = items.Where(item => {
                var isTitleMatch = item.GetSanitizedTitles().Any(title => MediaNameSimilarity.Calculate(searchTitle, title) >= 90);
                var isAdditionalTitleMatch = item.AdditionalTitles.Any(title => MediaNameSimilarity.Calculate(searchTitle, title) >= 90);

                return isTitleMatch || isAdditionalTitleMatch;
            });

            //if (year.HasValue) {
            //    var itemsMatchedByYear = matchedItems.Where(x => x.StartYear == year.Value);
            //    if (itemsMatchedByYear.Any()) {
            //        matchedItems = itemsMatchedByYear;
            //    }
            //}

            var customIds = new List<CustomIdResult>();
            foreach (var item in matchedItems) {
                var ckMeta = CACHE_KEY_ITEM_METADATA.Replace("[uri]", item.Url.ToString());
                var newMeta = cache.Get<HdRezkaApi.HdRezkaMetadata>(ckMeta);
                if (newMeta == null) {
                    newMeta = await apiClient.GetMetadata(item.Url);
                    cache.Set(ckMeta, newMeta, DEFAULT_EXPIRATION);
                }

                if (imdbId == newMeta.ImdbId) {
                    var customId = new CustomIdResult(PREFIX + item.Url.PathAndQuery, DEFAULT_EXPIRATION);
                    customIds.Add(customId);

                    if (newMeta.IsStandaloneTitle()) { break; }
                }
            }

            return customIds.ToArray();
        }
        #endregion

        #region IMediaProvider
        public bool CanGetStreams(string id) {
            return id.StartsWith(PREFIX);
        }

        public async Task<MediaStream[]> GetStreams(string id, int? season = null, int? episode = null) {
            id = id.Replace(PREFIX, "");
            var uri = new Uri(config.HostUri, id);

            var ckMeta = CACHE_KEY_ITEM_METADATA.Replace("[uri]", uri.ToString());
            var meta = cache.Get<HdRezkaApi.HdRezkaMetadata>(ckMeta);
            if (meta == null) {
                meta = await apiClient.GetMetadata(uri);
                cache.Set(ckMeta, meta, DEFAULT_EXPIRATION);
            }
            if (season.HasValue && episode.HasValue && !meta.IsStandaloneTitle() && meta.GetTvSeason() != season) { return []; }

            var ckStreams = CACHE_KEY_STREAMS
                .Replace("[id]", id)
                .Replace("[season]", season?.ToString())
                .Replace("[episode]", episode?.ToString());
            var mediaStreams = cache.Get<MediaStream[]>(ckStreams);
            if (mediaStreams == null) {
                var newMediaStreams = new List<MediaStream>();
                var streamUrls = new List<string>();
                if (season.HasValue && episode.HasValue) {
                    foreach (var item in meta.Details) {
                        var streams = await apiClient.GetSeriesEpisodeStreams(uri, item.Id, item.TranslatorId, season.Value, episode.Value);
                        foreach (var stream in streams) {
                            if (streamUrls.Contains(stream.Url)) { continue; }

                            var mediaStream = new MediaStream() {
                                Name = $"[{ServiceName.ToUpperInvariant()}]\n[{stream.Quality}p]",
                                Title = $"Episode {episode?.ToString("000")}\n{item.Title}",
                                Url = new MediaLink(new Uri(stream.Url), ServiceName.ToLowerInvariant(), MediaFormatType.MP4, stream.Quality, MediaProxyType.Proxy).GetUri().ToString()
                            };
                            newMediaStreams.Add(mediaStream);
                            streamUrls.Add(stream.Url);
                        }
                    }
                } else {
                    foreach (var item in meta.Details) {
                        var streams = await apiClient.GetMovieStreams(uri, item.Id, item.TranslatorId, item.IsCamrip, item.HasAds, item.IsDirectorCut);
                        foreach (var stream in streams) {
                            if (streamUrls.Contains(stream.Url)) { continue; }

                            var mediaStream = new MediaStream() {
                                Name = $"[{ServiceName.ToUpperInvariant()}]\n[{stream.Quality}p]",
                                Title = (string.IsNullOrWhiteSpace(meta.OriginalTitle) ? meta.Title : $"{meta.Title} / {meta.OriginalTitle}") + $"\n{item.Title}",
                                Url = new MediaLink(new Uri(stream.Url), ServiceName.ToLowerInvariant(), MediaFormatType.MP4, stream.Quality, MediaProxyType.Proxy).GetUri().ToString()
                            };
                            newMediaStreams.Add(mediaStream);
                            streamUrls.Add(stream.Url);
                        }
                    }
                }
                mediaStreams = newMediaStreams.ToArray();
                cache.Set(ckStreams, mediaStreams, DEFAULT_EXPIRATION);
            }

            return mediaStreams;
        }

        public bool CanGetMedia(MediaLink link) {
            if (link.SourceType.ToUpperInvariant() != ServiceName.ToUpperInvariant()) { return false; }
            if (link.FormatType != MediaFormatType.MP4) { return false; }

            return true;
        }
        #endregion

        #region IMediaProvider / streaming
        async Task<IMediaSource> IMediaProvider.GetMedia(MediaLink link, RangeHeaderValue range) {
            if (link.FormatType != MediaFormatType.MP4) { throw new NotSupportedException($"[{ServiceName}] fomat '{link.FormatType}' not supported."); }

            using (var client = GetHttpClient()) {
                var message = new HttpRequestMessage(HttpMethod.Get, link.SourceUri);
                message.Headers.Range = range;

                var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode) {
                    // TODO: logging
                    return null;
                }

                var contentType = response.Content.Headers.ContentType.ToString();
                var stream = await response.Content.ReadAsStreamAsync();
                var contentLength = response.Content.Headers.ContentLength.Value;

                return new StreamMediaSource(contentType, stream, contentLength) {
                    AcceptRanges = response.Headers.AcceptRanges?.FirstOrDefault()?.ToString(),
                    ContentRange = response.Content.Headers.ContentRange?.ToString()
                };
            }
        }
        #endregion
    }
}
