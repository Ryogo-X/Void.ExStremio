using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Providers.Media.Hdvb.Models;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Providers.Media.Hdvb {
    public class HdvbConfig {
        public const string CONFIG_API_KEY = "HDVB_API_KEY";

        public string ApiKey { get; }

        public HdvbConfig(string apiKey) {
            ApiKey = apiKey;
        }
    }

    class HdvbCdnProvider : MediaProviderBase, IMediaProvider {
        const string PREFIX = "kp";
        const string baseSearchUri = "https://apivb.info/api/videos.json?token=[token]&id_kp=[id]";
        readonly HdvbConfig config;

        readonly TimeSpan DEFAULT_EXPIRATION = TimeSpan.FromMinutes(8 * 60);
        readonly string CACHE_KEY_API_SEARCH;
        readonly string CACHE_KEY_STREAMS;

        public override string ServiceName {
            get { return "Hdvb"; }
        }

        public HdvbCdnProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, HdvbConfig config) : base(httpClientFactory, cache) {
            this.config = config;

            CACHE_KEY_API_SEARCH = $"{ServiceName}:API:SEARCH:KP:[id]";
            CACHE_KEY_STREAMS = $"{ServiceName}:STREAMS:[uri]:[season]:[episode]";
        }

        public bool CanHandle(string id) {
            return id.StartsWith(PREFIX);
        }

        public bool CanHandle(MediaLink link) {
            throw new NotImplementedException();
        }

        public async Task<MediaStream[]> GetStreams(string id, int? season = null, int? episode = null) {
            if (id.StartsWith(PREFIX)) { id = id.Replace(PREFIX, ""); }

            var ckSearch = CACHE_KEY_API_SEARCH.Replace("[id]", id);
            var searchItems = cache.Get<HdvbSearchItemResponse[]>(ckSearch);
            if (searchItems == null) {
                var uriString = baseSearchUri
                    .Replace("[token]", config.ApiKey)
                    .Replace("[id]", id);

                using (var client = GetHttpClient()) {
                    searchItems = await client.GetFromJsonAsync<HdvbSearchItemResponse[]>(uriString);
                    cache.Set(ckSearch, searchItems, DEFAULT_EXPIRATION);
                }
            }

            var items = searchItems.AsEnumerable();
            if (season.HasValue && episode.HasValue) {
                items = items
                    .Where(x => x.Seasons.FirstOrDefault(y => y.Season == season)?.Episodes.Any(y => y == episode) == true);
            }

            var mediaStreams = new List<MediaStream>();
            foreach (var item in items) {
                var streams = await GetStreams(item.Link, season, episode);
                foreach (var stream in streams) {
                    if (season.HasValue && episode.HasValue) {
                        stream.Title = $"Episode {episode?.ToString("000")}\n{item.Translator}";
                    } else {
                        stream.Title = string.IsNullOrWhiteSpace(item.TitleOriginal) ? $"{item.Title}" : $"{item.Title} / {item.TitleOriginal}";
                        stream.Title += $"\n{item.Translator}";
                    }
                }
                mediaStreams.AddRange(streams);
            }

            return mediaStreams.ToArray();
        }

        public async Task<MediaStream[]> GetStreams(Uri iframeUri, int? season = null, int? episode = null) {
            var ckStreams = CACHE_KEY_STREAMS
                .Replace("[uri]", iframeUri.ToString())
                .Replace("[season]", season?.ToString())
                .Replace("[episode]", episode?.ToString());
            var mediaStreams = cache.Get<MediaStream[]>(ckStreams);
            if (mediaStreams != null) { return mediaStreams; }

            using (var client = GetHttpClient()) {
                var html = await client.GetStringAsync(iframeUri);

                var csrfToken = Regex.Match(html, "\"key\":\"(?<key>.*?)\"")
                                     .Groups["key"].Value.Replace("\\/", "/");
                var fileUriString = Regex.Match(html, "\"file\":\"(?<file>.*?)\"")
                             .Groups["file"].Value.Replace("\\/", "/");
                if (!fileUriString.StartsWith("/")) {
                    fileUriString = "/playlist/" + fileUriString.TrimStart('~') + ".txt";
                }
                var uri = new Uri(iframeUri, fileUriString);

                client.DefaultRequestHeaders.Referrer = iframeUri;
                client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfToken);
                var fileResponse = await client.PostAsync(uri, null);
                var fileResponseString = await fileResponse.Content.ReadAsStringAsync();

                if (season.HasValue && episode.HasValue) {
                    var json = fileResponseString.Replace(",[]", "");
                    var playlistSeasons = await JsonSerializerExt.DeserializeAsync<HdvbPlaylistSeason[]>(json);
                    var episodeItem = playlistSeasons.SelectMany(x => x.Episodes).FirstOrDefault(x => x.Id == $"{season}-{episode}");
                    if (episodeItem == null) { return []; }

                    foreach (var file in episodeItem.Files) {
                        uri = new Uri(iframeUri, "/playlist/" + file.File + ".txt");
                        var playlistFileResponse = await client.PostAsync(uri, null);
                        var playlistFileUri = await playlistFileResponse.Content.ReadAsStringAsync();

                        var playlistFile = await client.GetStringAsync(playlistFileUri);
                        var matches = Regex.Matches(playlistFile, "./(?<quality>[0-9]*?)/index.m3u8");

                        var results = new List<MediaStream>();
                        foreach (Match match in matches) {
                            var quality = match.Groups["quality"].Value;
                            var fileUri = new Uri(new Uri(playlistFileUri), match.Value);

                            results.Add(new MediaStream() {
                                Name = $"[{ServiceName.ToUpperInvariant()}]\n[{quality}p]",
                                Url = new MediaLink(fileUri, ServiceName.ToLowerInvariant(), MediaFormatType.MP4, quality, MediaProxyType.Direct).GetUri().ToString()
                            });
                        }

                        mediaStreams = results.ToArray();
                    }
                } else {
                    var playlistFile = await client.GetStringAsync(fileResponseString);
                    var matches = Regex.Matches(playlistFile, "./(?<quality>[0-9]*?)/index.m3u8");

                    var results = new List<MediaStream>();
                    foreach (Match match in matches) {
                        var quality = match.Groups["quality"].Value;
                        var fileUri = new Uri(new Uri(fileResponseString), match.Value);

                        results.Add(new MediaStream() {
                            Name = $"[{ServiceName.ToUpperInvariant()}]\n[{quality}p]",
                            Url = new MediaLink(fileUri, ServiceName.ToLowerInvariant(), MediaFormatType.MP4, quality, MediaProxyType.Direct).GetUri().ToString()
                        });
                    }

                    mediaStreams = results.ToArray();
                }

                cache.Set(ckStreams, mediaStreams, DEFAULT_EXPIRATION);

                return mediaStreams;
            }
        }

        Task<IMediaSource> IMediaProvider.GetMedia(MediaLink link, RangeHeaderValue range = null) {
            throw new NotImplementedException();
        }
    }
}
