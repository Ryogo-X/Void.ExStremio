using System.Text.RegularExpressions;
using System.Text;
using Void.EXStremio.Web.Utility;
using Void.EXStremio.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media {
    abstract class MediaProviderBase {
        readonly protected IHttpClientFactory httpClientFactory;
        readonly protected IMemoryCache cache;

        public abstract string ServiceName { get; }

        protected MediaProviderBase(IHttpClientFactory httpClientFactory, IMemoryCache cache) {
            this.httpClientFactory = httpClientFactory;
            this.cache = cache;
        }

        protected virtual HttpClient GetHttpClient() {
            return httpClientFactory.CreateClient(GetType().Name);
        }

        #region Get Playlist Streams
        protected async Task<MediaStream[]> GetPlaylistStreams(Uri uri, MediaFormatType format) {
            if (format != MediaFormatType.DASH && format != MediaFormatType.HLS) {
                throw new NotSupportedException($"Format {format} is not a supported playlist format.");
            }

            return await cache.GetOrCreateAsync(uri.ToString() + ":streams", async entry => {
                entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

                var streams = new MediaStream[0];
                if (format == MediaFormatType.DASH) {
                    streams = await GetDashPlaylistStreams(uri);
                } else if (format == MediaFormatType.HLS) {
                    streams = await GetHlsPlaylistStreams(uri);
                }

                return streams;
            });

        }

        protected async Task<MediaStream[]> GetDashPlaylistStreams(Uri playlistUri) {
            using (var client = GetHttpClient()) {
                var xml = await client.GetStringAsync(playlistUri);
                var document = XDocumentExt.Load(xml);
                var videoSet = document.Root.Descendants().First(x => x.Name.LocalName == "AdaptationSet" && x.Attribute("contentType").Value == "video");

                var qualityItems = videoSet.Elements()
                    .Select(element => {
                        var qualityString = element.Attribute("height").Value;
                        return int.Parse(qualityString);
                    });

                return qualityItems
                    .OrderByDescending(x => x)
                    .Select(quality => {
                        return new MediaStream() {
                            Name = $"[{ServiceName.ToUpperInvariant()}]\n[{quality}p]",
                            Url = new MediaLink(playlistUri, ServiceName, MediaFormatType.DASH, quality.ToString(), MediaProxyType.Proxy).ToString()
                        };
                    }).ToArray();
            }
        }

        protected async Task<MediaStream[]> GetHlsPlaylistStreams(Uri playlistUri, bool proxyStream = true) {
            var streams = new List<MediaStream>();

            using (var client = GetHttpClient()) {
                var content = await client.GetStringAsync(playlistUri);
                var lines = content.Split('\n');
                for (var i = 0; i < lines.Length; i++) {
                    var line = lines[i];
                    if (!line.StartsWith("#EXT-X-STREAM-INF")) { continue; }

                    var match = Regex.Match(line, @"#EXT-X-STREAM-INF:.*RESOLUTION=[0-9]+x(?<vres>[0-9]+).*");
                    var quality = int.Parse(match.Groups["vres"].Value);
                    var url = lines[i + 1];
                    if (url.StartsWith(".") || url.StartsWith("/")) {
                        url = new Uri(playlistUri, url).ToString();
                    }
                    if (proxyStream) {
                        url = new MediaLink(playlistUri, ServiceName, MediaFormatType.HLS, quality.ToString(), MediaProxyType.Proxy).ToString();
                    }

                    streams.Add(new MediaStream() {
                        Name = $"[{ServiceName.ToUpperInvariant()}]\n[{quality}p]",
                        Url = url
                    });
                }
            }

            return streams.ToArray();
        }
        #endregion

        #region Transform Playlist
        protected async Task<string> GetPlaylist(Uri uri, MediaFormatType format, string quality, string[] audioTracks = null) {
            if (format != MediaFormatType.DASH && format != MediaFormatType.HLS) {
                throw new NotSupportedException($"Format {format} is not a supported playlist format.");
            }

            return await cache.GetOrCreateAsync(uri.ToString() + ":playlist", async entry => {
                entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

                string playlist = null;
                if (format == MediaFormatType.DASH) {
                    playlist = await GetDashPlaylist(uri, quality, audioTracks);
                } else if (format == MediaFormatType.HLS) {
                    playlist = await GetHlsPlaylist(uri, quality, audioTracks);
                }

                return playlist;
            });
        }

        protected async Task<string> GetDashPlaylist(Uri playlistUri, string quality, string[] audioTracks = null) {
            using (var client = GetHttpClient()) {
                var content = await client.GetStringAsync(playlistUri);
                var document = XDocumentExt.Load(content);
                var videoSet = document.Root.Descendants().First(x => x.Name.LocalName == "AdaptationSet" && x.Attribute("contentType").Value == "video");
                foreach (var element in videoSet.Elements().ToArray()) {
                    if (element.Attribute("height").Value == quality) { continue; }

                    element.Remove();
                }

                if (audioTracks?.Any() == true) {
                    var audioSets = document.Root.Descendants().Where(x => x.Name.LocalName == "AdaptationSet" && x.Attribute("contentType").Value == "audio").ToArray();
                    if (audioSets.Length == audioTracks.Length) {
                        for (var i = 0; i < audioSets.Length; i++) {
                            var audioSet = audioSets[i];
                            var audioName = audioTracks[i];
                            audioSet.Attribute("lang").SetValue(audioName);
                        }
                    }
                }

                return XDocumentExt.Save(document);
            }
        }

        protected async Task<string> GetHlsPlaylist(Uri playlistUri, string quality, string[] audioTracks = null) {
            using (var client = GetHttpClient()) {
                var content = await client.GetStringAsync(playlistUri);
                var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

                var renameAudioTracks = lines
                    .Where(x => x.StartsWith("#EXT-X-MEDIA:TYPE=AUDIO"))
                    .Where(x => !x.Contains("failover"))
                    .Count() == audioTracks?.Length;

                var builder = new StringBuilder();
                for (var i = 0; i < lines.Length; i++) {
                    var line = lines[i];
                    if (line.StartsWith("#EXT-X-STREAM-INF")) {
                        var vres = Regex.Match(line, @"#EXT-X-STREAM-INF:.*RESOLUTION=[0-9]+x(?<vres>[0-9]+).*").Groups["vres"].Value;
                        if (vres != quality) {
                            i++;
                            continue;
                        }
                    } else if (line.StartsWith("#EXT-X-MEDIA:TYPE=AUDIO") && renameAudioTracks) {
                        var match = Regex.Match(line, "NAME=\"(?<audioName>[a-zA-Z0-9]+)\"");
                        var audioName = match.Groups["audioName"].Value;
                        var audioIdxString = Regex.Replace(audioName, "[^0-9]", "");
                        var audioIdx = 0;
                        if (!string.IsNullOrEmpty(audioIdxString)) { 
                            audioIdx = int.Parse(audioIdxString);
                        }
                        line = Regex.Replace(line, "LANGUAGE=\"[a-zA-Z0-9]+\"", $"LANGUAGE=\"{audioTracks[audioIdx]}\"");
                    }

                    builder.AppendLine(line);
                }

                return builder.ToString();
            }
        }
        #endregion
    }
}
