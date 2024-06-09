using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Providers.Media.Zetflix {
    class ZetflixCdnProvider : MediaProviderBase, IMediaProvider {
        const string PREFIX = "kp";

        const string baseVideoUri = $"https://bwa-cloud.apn.monster/lite/zetflix?kinopoisk_id=[id]";

        public override string ServiceName {
            get { return "Zetflix"; }
        }

        readonly TimeSpan DEFAULT_EXPIRATION = TimeSpan.FromMinutes(8 * 60);
        readonly string CACHE_KEY_STREAMS;

        public ZetflixCdnProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) {
            CACHE_KEY_STREAMS = $"{ServiceName}:STREAMS:KP:[id]:[season]:[episode]";
        }

        public bool CanGetStreams(string id) {
            return id.StartsWith(PREFIX);
        }

        public bool CanGetMedia(MediaLink link) {
            return false;
        }

        public async Task<MediaStream[]> GetStreams(string id, int? season = null, int? episode = null) {
            if (id.StartsWith(PREFIX)) { id = id.Replace(PREFIX, ""); }

            var ckStreams = CACHE_KEY_STREAMS
                .Replace("[id]", id)
                .Replace("[season]", season?.ToString())
                .Replace("[episode]", episode?.ToString());
            var cachedStreams = cache.Get<MediaStream[]>(ckStreams);

            var mediaStreams = new List<MediaStream>();

            var uriString = baseVideoUri
                .Replace("[id]", id);
            if (season.HasValue && episode.HasValue) {
                uriString += $"&s={season}&e={episode}";

                using (var client = GetHttpClient()) {
                    var response = await client.GetAsync(uriString);
                    if (!response.IsSuccessStatusCode) {
                        // TODO: logging?
                        return [];
                    }

                    var html = await response.Content.ReadAsStringAsync();

                    var matches = Regex.Matches(html, "data-json='(?<json>{.+?})'", RegexOptions.Singleline);
                    foreach (Match match in matches) {
                        var json = match.Groups["json"].Value;
                        var item = await JsonSerializerExt.DeserializeAsync<ZetflixResponse>(json);
                        if (item.Method != "link") { continue; }

                        var translator = HttpUtility.UrlDecode(item.Url.Query.Split('&').First(x => x.StartsWith("t=")).Replace("t=", ""));
                        var streams = await GetStreams(item.Url, season, episode);
                        foreach(var stream in streams) {
                            if (season.HasValue && episode.HasValue) {
                                stream.Title += $"\n{translator}";
                            }
                        }

                        mediaStreams.AddRange(streams);
                    }
                }
            } else {
                return await GetStreams(new Uri(uriString));
            }

            var results = mediaStreams.ToArray();
            cache.Set(ckStreams, results);

            return results;
        }

        async Task<MediaStream[]> GetStreams(Uri uri, int? season = null, int? episode = null) {
            var mediaStreams = new List<MediaStream>();

            using (var client = GetHttpClient()) {
                var response = await client.GetAsync(uri);
                if (!response.IsSuccessStatusCode) {
                    // TODO: logging?
                    return [];
                }

                var html = await response.Content.ReadAsStringAsync();
                var matches = Regex.Matches(html, "data-json='(?<json>{.+?})'", RegexOptions.Singleline);
                foreach (Match match in matches) {
                    var json = match.Groups["json"].Value;
                    var item = await JsonSerializerExt.DeserializeAsync<ZetflixResponse>(json);
                    if (item.Method != "play") { continue; }

                    if (episode.HasValue) {
                        var episodeString = Regex.Match(item.Title, "(?<episode>[0-9]+) серия").Groups["episode"].Value;
                        if (episodeString != episode.ToString()) { continue; }
                    }
                    foreach (var videoLink in item.Links) {
                        var quality = videoLink.Key.TrimEnd('p');
                        var videoUri = new Uri(videoLink.Value);

                        var mediaStream = new MediaStream() {
                            Name = $"[{ServiceName.ToUpperInvariant()}]\n[{quality}p]",
                            Url = new MediaLink(videoUri, ServiceName, MediaFormatType.MP4, quality, MediaProxyType.Direct).ToString(),
                        };
                        if (season.HasValue && episode.HasValue) {
                            mediaStream.Title = $"Episode {episode?.ToString("000")}";
                        } else {
                            mediaStream.Title = item.Title;
                        }

                        mediaStreams.Add(mediaStream);
                    }
                }
            }

            return mediaStreams.ToArray();
        }

        public Task<IMediaSource> GetMedia(MediaLink link, RangeHeaderValue range) {
            throw new NotImplementedException();
        }
    }
}