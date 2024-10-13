using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Providers.Media.VideoCdn.Models;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Providers.Media.VideoCdn {
    public class VideoCdnConfig {
        public const string CONFIG_API_KEY = "VCDN_API_KEY";

        public string ApiKey { get; }

        public VideoCdnConfig(string apiKey) {
            ApiKey = apiKey;
        }
    }

    class VideoCdnProvider : MediaProviderBase, IMediaProvider, IKinopoiskIdProvider {
        const string KP_PREFIX = "kp";
        const string IMDB_PREFIX = "tt";

        readonly VideoCdnConfig config;

        const string baseImdbSearchUri = "https://videocdn.tv/api/short?api_token=[token]&imdb_id=[id]";
        const string baseKpSearchUri = "https://videocdn.tv/api/short?api_token=[token]&kinopoisk_id=[id]";

        readonly TimeSpan DEFAULT_EXPIRATION = TimeSpan.FromMinutes(4 * 60);
        readonly string CACHE_KEY_API_SEARCH_IMDB;
        readonly string CACHE_KEY_API_SEARCH_KP;
        readonly string CACHE_KEY_STREAMS;

        public override string ServiceName {
            get { return "VideoCDN"; }
        }

        public VideoCdnProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, VideoCdnConfig config) : base(httpClientFactory, cache) {
            this.config = config;

            CACHE_KEY_API_SEARCH_IMDB = $"{ServiceName}:API:SEARCH:IMDB:[id]";
            CACHE_KEY_API_SEARCH_KP = $"{ServiceName}:API:SEARCH:KP:[id]";
            CACHE_KEY_STREAMS = $"{ServiceName}:STREAMS:[uri]:[season]:[episode]";
        }

        #region IKinopoiskIdProvider
        public async Task<string?> GetKinopoiskIdAsync(string imdbId) {
            string kpId = null;

            var ckSearchImdb = CACHE_KEY_API_SEARCH_IMDB.Replace("[id]", imdbId);
            var searchResponse = cache.Get<VideoCdnSearchResponse>(ckSearchImdb);
            if (searchResponse == null) {
                using (var client = GetHttpClient()) {
                    var uriString = baseImdbSearchUri
                                        .Replace("[token]", config.ApiKey)
                                        .Replace("[id]", imdbId);

                    var response = await client.GetAsync(uriString);
                    if (!response.IsSuccessStatusCode) {
                        // TODO: logging?
                        return null;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    searchResponse = await JsonSerializerExt.DeserializeAsync<VideoCdnSearchResponse>(json);

                    kpId = searchResponse?.Data.FirstOrDefault()?.KpId.ToString();
                    if (kpId == "0") {
                        kpId = null;
                    }

                    cache.Set(ckSearchImdb, searchResponse, DEFAULT_EXPIRATION);
                    if (kpId != null) {
                        var ckSearchKp = CACHE_KEY_API_SEARCH_IMDB.Replace("[id]", kpId);
                        cache.Set(ckSearchKp, searchResponse, DEFAULT_EXPIRATION);
                    }
                }
            }

            kpId = searchResponse?.Data.FirstOrDefault()?.KpId.ToString();
            if (kpId == "0") {
                kpId = null;
            }

            return string.IsNullOrWhiteSpace(kpId) ? null : KP_PREFIX + kpId;
        }
        #endregion

        #region IMediaProvider
        public bool CanGetStreams(string id) {
            return id.StartsWith(IMDB_PREFIX) || id.StartsWith(KP_PREFIX);
        }

        public bool CanGetMedia(MediaLink link) {
            if (link.SourceType.ToUpperInvariant() != ServiceName.ToUpperInvariant()) { return false; }
            if (link.FormatType != MediaFormatType.MP4) { return false; }

            return true;
        }

        public async Task<MediaStream[]> GetStreams(string id, int? season = null, int? episode = null) {
            var isImdb = id.StartsWith(IMDB_PREFIX);
            var isKp = id.StartsWith(KP_PREFIX);

            string uriString = null;
            string ckSearch = null;
            if (isImdb) {
                ckSearch = CACHE_KEY_API_SEARCH_IMDB.Replace("[id]", id);
                uriString = baseImdbSearchUri
                    .Replace("[token]", config.ApiKey)
                    .Replace("[id]", id);
            } else if (isKp) {
                id = id.Replace(KP_PREFIX, "");
                ckSearch = CACHE_KEY_API_SEARCH_KP.Replace("[id]", id);
                uriString = baseKpSearchUri
                    .Replace("[token]", config.ApiKey)
                    .Replace("[id]", id);
            }

            var searchResponse = cache.Get<VideoCdnSearchResponse>(ckSearch);
            if (searchResponse == null) {
                if (string.IsNullOrWhiteSpace(uriString)) { return []; }

                using (var client = GetHttpClient()) {
                    var response = await client.GetAsync(uriString);
                    searchResponse = await response.Content.ReadFromJsonAsync<VideoCdnSearchResponse>();
                    cache.Set(ckSearch, searchResponse, DEFAULT_EXPIRATION);
                }
            }
            if (!searchResponse.Result) { return []; }

            var responseItem = searchResponse.Data.First();
            var iframeUri = new Uri($"https:{responseItem.Link}");
            var ckStreams = CACHE_KEY_STREAMS
                .Replace("[uri]", iframeUri.ToString())
                .Replace("[season]", season?.ToString())
                .Replace("[episode]", episode?.ToString());

            var mediaStreams = cache.Get<MediaStream[]>(ckStreams);
            if (mediaStreams == null) {
                mediaStreams = await GetStreams(iframeUri, season, episode);
                foreach (var mediaStream in mediaStreams) {
                    if (season.HasValue && episode.HasValue) { continue; }

                    mediaStream.Title = (string.IsNullOrWhiteSpace(responseItem.TitleOriginal) ? responseItem.Title : $"{responseItem.Title} / {responseItem.TitleOriginal}") + mediaStream.Title;
                }
                cache.Set(ckStreams, mediaStreams, DEFAULT_EXPIRATION);
            }

            return mediaStreams;
        }

        async Task<MediaStream[]> GetStreams(Uri iframeUri, int? season = null, int? episode = null) {
            using (var client = GetHttpClient()) {
                var html = await client.GetStringAsync(iframeUri);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                var translations = htmlDocument.DocumentNode
                    .SelectNodes("//div[@class=\"translations\"]/select/option")
                    ?.Where(x => x.GetAttributeValue("value", null) != "0")
                    .Select(x => (Id: x.GetAttributeValue("value", null), Name: x.InnerText.Trim()));
                var json = htmlDocument.DocumentNode
                    .SelectNodes("//input[@type='hidden']")
                    .Select(x => x.GetAttributeValue("value", null))
                    .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Contains("&quot;"))
                    .Single(x => x.Contains(".mp4"));

                var mediaStreams = new List<MediaStream>();
                if (season.HasValue && episode.HasValue) {
                    var tvDict = await JsonSerializerExt.DeserializeAsync<Dictionary<string, VideoCdnPlaylistSeasonResponse[]>>(json);
                    foreach (var kv in tvDict) {
                        if (kv.Key == "0") { continue; }

                        var translation = translations?.FirstOrDefault(x => x.Id == kv.Key).Name;
                        if (string.IsNullOrWhiteSpace(translation)) { continue; }

                        var episodeItem = kv.Value.SelectMany(x => x.Episodes).FirstOrDefault(x => x.Id == $"{season}_{episode}");
                        if (episodeItem == null) { continue; }

                        if (string.IsNullOrEmpty(translation)) {
                            translation = Regex.Match(episodeItem.Comment, "<i>(?<value>.*)</i>").Groups["value"].Value;
                        }

                        var links = GetLinks(episodeItem.File);
                        foreach (var link in links) {
                            var mediaStream = new MediaStream() {
                                Name = $"[{ServiceName.ToUpperInvariant()}]\n[{link.quality}]",
                                Url = new MediaLink(link.uri, ServiceName, MediaFormatType.MP4, link.quality, MediaProxyType.Proxy).ToString(),
                                Title = $"Episode {episode?.ToString("000")}\n{translation}"
                            };
                            mediaStreams.Add(mediaStream);
                        }
                    }
                } else {
                    var movieDict = await JsonSerializerExt.DeserializeAsync<Dictionary<string, string>>(json);
                    foreach (var kv in movieDict) {
                        if (kv.Key == "0") { continue; }

                        var translation = translations?.First(x => x.Id == kv.Key).Name;
                        var links = GetLinks(kv.Value);
                        foreach (var link in links) {
                            var mediaStream = new MediaStream() {
                                Name = $"[{ServiceName.ToUpperInvariant()}]\n[{link.quality}]",
                                Url = new MediaLink(link.uri, ServiceName, MediaFormatType.MP4, link.quality, MediaProxyType.Proxy).ToString(),
                                Title = $"\n{translation}"
                            };
                            mediaStreams.Add(mediaStream);
                        }
                    }
                }

                return mediaStreams.ToArray();
            }
        }

        public IEnumerable<(string quality, Uri uri)> GetLinks(string fileString) {
            var lines = fileString?.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines) {
                var match = Regex.Match(line, "\\[(?<quality>.*)\\](?<uri>.*)");

                yield return (match.Groups["quality"].Value, new Uri("https:" + match.Groups["uri"].Value));
            }
        }

        public async Task<IMediaSource> GetMedia(MediaLink link, RangeHeaderValue range = null) {
            if (link.FormatType != MediaFormatType.MP4) { throw new NotSupportedException($"[{ServiceName}] fomat '{link.FormatType}' not supported."); }

            using(var client = GetHttpClient()) {
                var message = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, link.SourceUri);
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
