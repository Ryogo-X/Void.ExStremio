using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Providers.Media.Kodik.Models;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Providers.Media.CdnMovies {
    public class CdnMoviesConfig {
        public const string CONFIG_API_KEY = "CDNMOVIES_API_KEY";

        public string ApiKey { get; }

        public CdnMoviesConfig(string apiKey) {
            ApiKey = apiKey;
        }
    }

    class CdnMoviesCdnProvider : MediaProviderBase, IKinopoiskIdProvider, IMediaProvider {
        const string PREFIX = "kp";
        const string baseSearchImdbUri = "https://api.cdnmovies.net/v1/contents?token=[token]&imdb_id=[id]";
        const string baseSearchKpUri = "https://api.cdnmovies.net/v1/contents?token=[token]&kinopoisk_id=[id]";

        readonly CdnMoviesConfig config;

        readonly TimeSpan DEFAULT_EXPIRATION = TimeSpan.FromMinutes(8 * 60);
        readonly string CACHE_KEY_API_SEARCH_KP;
        readonly string CACHE_KEY_API_SEARCH_IMDB;
        readonly string CACHE_KEY_STREAMS;

        public override string ServiceName {
            get { return "CdnMovies"; }
        }

        public CdnMoviesCdnProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, CdnMoviesConfig config) : base(httpClientFactory, cache) {
            this.config = config;

            CACHE_KEY_API_SEARCH_KP = $"{ServiceName}:API:SEARCH:KP:[id]";
            CACHE_KEY_API_SEARCH_IMDB = $"{ServiceName}:API:SEARCH:IMDB:[id]";
            CACHE_KEY_STREAMS = $"{ServiceName}:STREAMS:[id]:[season]:[episode]";
        }

        #region IKinopoiskIdProvider
        public async Task<string?> GetKinopoiskIdAsync(string imdbId) {
            string kpId = null;

            var ckSearchImdb = CACHE_KEY_API_SEARCH_IMDB
                .Replace("[id]", imdbId);
            var searchResponse = cache.Get<CdnMoviesSearchResponse>(ckSearchImdb);
            if (searchResponse == null) {
                using (var client = GetHttpClient()) {
                    var uriString = baseSearchImdbUri
                                        .Replace("[token]", config.ApiKey)
                                        .Replace("[id]", imdbId);

                    var response = await client.GetAsync(uriString);
                    if (!response.IsSuccessStatusCode) {
                        cache.Set(ckSearchImdb, new CdnMoviesSearchResponse() { Data = [] }, DEFAULT_EXPIRATION);

                        // TODO: logging?
                        return null;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    searchResponse = await JsonSerializerExt.DeserializeAsync<CdnMoviesSearchResponse>(json);
                    cache.Set(ckSearchImdb, searchResponse, DEFAULT_EXPIRATION);

                    kpId = searchResponse?.Data.FirstOrDefault()?.KpId?.ToString();
                    if (string.IsNullOrWhiteSpace(kpId)) { return null; }

                    var ckSearchKp = CACHE_KEY_API_SEARCH_KP
                        .Replace("[id]", kpId);
                    cache.Set(ckSearchKp, searchResponse, DEFAULT_EXPIRATION);

                    return PREFIX + kpId;
                }
            }

            kpId = searchResponse?.Data.FirstOrDefault()?.KpId?.ToString();
            if (string.IsNullOrWhiteSpace(kpId)) { return null; }

            return PREFIX + kpId;
        }
        #endregion

        #region IMediaProvider
        public bool CanGetStreams(string id) {
            return id.StartsWith(PREFIX);
        }

        public async Task<MediaStream[]> GetStreams(string id, int? season = null, int? episode = null) {
            id = id.Replace(PREFIX, "");
            var uriString = baseSearchKpUri
                    .Replace("[token]", config.ApiKey)
                    .Replace("[id]", id);

            var ckSearchKp = CACHE_KEY_API_SEARCH_KP
                .Replace("[id]", id);
            var searchResponse = cache.Get<CdnMoviesSearchResponse>(ckSearchKp);
            if (searchResponse == null) {
                using (var client = GetHttpClient()) {
                    var response = await client.GetAsync(uriString);
                    if (!response.IsSuccessStatusCode) {
                        cache.Set(ckSearchKp, new KodikSearchResponse() { Results = [] }, DEFAULT_EXPIRATION);
                        // TODO: logging?
                        return [];
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    searchResponse = await JsonSerializerExt.DeserializeAsync<CdnMoviesSearchResponse>(json);
                    cache.Set(ckSearchKp, searchResponse, DEFAULT_EXPIRATION);
                }
            }
            if (!searchResponse.Data.Any()) { return []; }

            var ckStreams = CACHE_KEY_STREAMS
                .Replace("[id]", id)
                .Replace("[season]", season?.ToString())
                .Replace("[episode]", episode?.ToString());
            var mediaStreams = cache.Get<MediaStream[]>(ckStreams);
            if (mediaStreams == null) {
                var meta = searchResponse.Data.First();
                var iframeUri = meta.Uri;

                using (var client = GetHttpClient()) {
                    client.DefaultRequestHeaders.Referrer = new Uri("https://google.com/");
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/113.0");
                    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.5");
                    var response = await client.GetAsync(iframeUri);
                    var html = await response.Content.ReadAsStringAsync();
                    var dataString = Regex.Match(html, "file: '(?<data>.+)'").Groups["data"].Value;
                    dataString = Regex.Replace(dataString.Replace("#2", ""), "//.{40}", "");
                    var json = Base64Ext.Decode(dataString);

                    var newMediaStreams = new List<MediaStream>();
                    if (season.HasValue && episode.HasValue) {
                        // type.Contains("сериал")
                        var data = await JsonSerializerExt.DeserializeAsync<CdnMoviesTvSeasonResponse[]>(json);
                        var episodeItem = data.FirstOrDefault(x => x.GetNumber() == season.Value)?.Episodes?.FirstOrDefault(x => x.GetNumber() == episode.Value);
                        foreach (var item in episodeItem.Links) {
                            var mediaLinks = item.GetLinks();
                            foreach (var mediaLink in mediaLinks) {
                                var mediaStream = new MediaStream() {
                                    Name = $"[{ServiceName.ToUpperInvariant()}]\n[{mediaLink.Quality}p]",
                                    Title = $"Episode {episode?.ToString("000")}\n{item.Title}",
                                    Url = new MediaLink(new Uri(mediaLink.Url), ServiceName.ToLowerInvariant(), MediaFormatType.MP4, mediaLink.Quality, MediaProxyType.Direct).GetUri().ToString()
                                };
                                newMediaStreams.Add(mediaStream);
                            }
                        }
                    } else {
                        // type = "фильм" // type = "аниме"
                        var items = await JsonSerializerExt.DeserializeAsync<CdnMoviesPlaylistItemResponse[]>(json);
                        foreach (var item in items) {
                            var mediaLinks = item.GetLinks();
                            foreach (var mediaLink in mediaLinks) {
                                var mediaStream = new MediaStream() {
                                    Name = $"[{ServiceName.ToUpperInvariant()}]\n[{mediaLink.Quality}p]",
                                    Title = (string.IsNullOrWhiteSpace(meta.OriginalTitle) ? meta.Title : $"{meta.Title} / {meta.OriginalTitle}") + $"\n{item.Title}",
                                    Url = new MediaLink(new Uri(mediaLink.Url), ServiceName.ToLowerInvariant(), MediaFormatType.MP4, mediaLink.Quality, MediaProxyType.Direct).GetUri().ToString()
                                };
                                newMediaStreams.Add(mediaStream);
                            }
                        }
                    }

                    mediaStreams = newMediaStreams.ToArray();
                    cache.Set(ckStreams, mediaStreams, DEFAULT_EXPIRATION);
                }
            }

            return mediaStreams;
        }
        #endregion

        #region IMediaProvider / streaming
        public bool CanGetMedia(MediaLink link) {
            return false;
        }

        public Task<IMediaSource> GetMedia(MediaLink link, RangeHeaderValue range = null) {
            throw new NotImplementedException();
        }
        #endregion
    }
}
