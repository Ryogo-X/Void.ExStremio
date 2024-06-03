using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Providers.Media.Collaps.Models;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Providers.Media.Collaps {
    public class CollapsConfig {
        public const string CONFIG_API_KEY = "COLLAPS_API_KEY";

        public string ApiKey { get; }

        public CollapsConfig(string apiKey) {
            ApiKey = apiKey;
        }
    }

    class CollapsCdnProvider : MediaProviderBase, IMediaProvider {
        const string PREFIX = "kp";
        const string baseDetailsUri = "https://apicollaps.cc/franchise/details?token=[token]&kinopoisk_id=[id]";

        readonly TimeSpan DEFAULT_EXPIRATION = TimeSpan.FromMinutes(8 * 60);
        readonly string CACHE_KEY_API_SEARCH;
        readonly string CACHE_KEY_API_DETAILS_JSON;
        readonly string CACHE_KEY_API_DETAILS;
        readonly string CACHE_KEY_STREAMS;
        readonly string CACHE_KEY_PLAYLIST;

        readonly CollapsConfig config;

        public override string ServiceName {
            get { return "Collaps"; }
        }

        public CollapsCdnProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, CollapsConfig config) : base(httpClientFactory, cache) {
            this.config = config;

            CACHE_KEY_API_SEARCH = $"{ServiceName}:API:SEARCH:KP:[id]";
            CACHE_KEY_API_DETAILS_JSON = $"{ServiceName}:API:DETAILS:JSON:[uri]";
            CACHE_KEY_API_DETAILS = $"{ServiceName}:API:DETAILS:[uri]";
            CACHE_KEY_STREAMS = $"{ServiceName}:STREAMS:[uri]:[season]:[episode]";
            CACHE_KEY_PLAYLIST = $"{ServiceName}:PLAYLIST:[uri]:[quality]";
        }

        public bool CanHandle(string id) {
            return id.StartsWith(PREFIX);
        }

        public bool CanHandle(MediaLink link) {
            if (link.SourceType.ToUpperInvariant() != ServiceName.ToUpperInvariant()) { return false; }
            if (!link.IsPlaylist()) { return false; }

            return true;
        }

        public async Task<MediaStream[]> GetStreams(string id, int? season = null, int? episode = null) {
            if (id.StartsWith(PREFIX)) { id = id.Replace(PREFIX, ""); }

            var ckSearch = CACHE_KEY_API_SEARCH.Replace("[id]", id);
            var detailsResponse = cache.Get<CollapseDetailsResponse>(ckSearch);
            if (detailsResponse == null) {
                using (var client = GetHttpClient()) {
                    var uriString = baseDetailsUri
                                        .Replace("[token]", config.ApiKey)
                                        .Replace("[id]", id);

                    var response = await client.GetAsync(uriString);
                    if (!response.IsSuccessStatusCode) {
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
                            cache.Set(CACHE_KEY_API_SEARCH.Replace("[id]", id), new CollapseDetailsResponse(), DEFAULT_EXPIRATION);
                        }

                        // TODO: logging?
                        return [];
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    detailsResponse = await JsonSerializerExt.DeserializeAsync<CollapseDetailsResponse>(json);

                    cache.Set(ckSearch, detailsResponse, DEFAULT_EXPIRATION);
                }
            }
            if (detailsResponse.Id == 0) {
                // TODO: logging?
                return [];
            }

            string iframeUri = null;
            if (season.HasValue && episode.HasValue) {
                iframeUri = detailsResponse?.Seasons?.FirstOrDefault(x => x.Season == season)?.Episodes?.FirstOrDefault(x => x.Episode == episode)?.Link;
            } else {
                iframeUri = detailsResponse?.Link;
            }
            if (string.IsNullOrWhiteSpace(iframeUri)) { return []; }

            return await GetStreams(new Uri(iframeUri), season, episode);
        }

        async Task<MediaStream[]> GetStreams(Uri iframeUri, int? season = null, int? episode = null) {
            var ckStreams = CACHE_KEY_STREAMS
                .Replace("[uri]", iframeUri.ToString())
                .Replace("[season]", season?.ToString())
                .Replace("[episode]", episode?.ToString());
            var mediaStreams = cache.Get<MediaStream[]>(ckStreams);
            if (mediaStreams != null) {  return mediaStreams; }

            var ckDetailsJson = CACHE_KEY_API_DETAILS_JSON
                .Replace("[uri]", string.IsNullOrEmpty(iframeUri.Query) ? "TEMP_STRING" : iframeUri.ToString().Replace(iframeUri.Query, ""));

            var json = cache.Get<string>(ckDetailsJson);
            if (string.IsNullOrWhiteSpace(json)) {
                using (var client = GetHttpClient()) {
                    var html = await client.GetStringAsync(iframeUri);
                    var jsString = Regex.Match(html, @"makePlayer\((?<json>{.*?})\);", RegexOptions.Singleline).Groups["json"].Value;
                    json = JsStringToJsonString(jsString);
                    cache.Set(ckDetailsJson, json);
                }
            }

            var format = MediaFormatType.Undefined;
            Uri playlistUri = null;
            var audio = new string[0];
            string name = null;
            object detailsItem = null;

            if (season.HasValue && episode.HasValue) {
                var playerConfig = await JsonSerializerExt.DeserializeAsync<CollapseTvConfigResponse>(json);
                var episodeItem = playerConfig
                    .Playlist.Seasons
                    .FirstOrDefault(x => x.Season == season)
                    ?.Episodes
                    ?.FirstOrDefault(x => x.Episode == episode.ToString());
                if (episodeItem == null) {
                    // TODO: logging?
                    return [];
                }

                if (episodeItem.Dash != null) {
                    playlistUri = episodeItem.Dash;
                    format = MediaFormatType.DASH;
                } else {
                    playlistUri = episodeItem.Hls;
                    format = MediaFormatType.HLS;
                }

                var episodeNumber = int.Parse(episodeItem.Episode);

                audio = episodeItem.Audio.Names;
                name = $"Episode {episode?.ToString("000")}";
                detailsItem = episodeItem;
            } else {
                var movieResponse = await JsonSerializerExt.DeserializeAsync<CollapseMovieResponse>(json);
                if (movieResponse == null) {
                    // TODO: logging?
                    return [];
                }

                if (movieResponse?.Source?.DashUri != null) {
                    playlistUri = movieResponse?.Source?.DashUri;
                    format = MediaFormatType.DASH;
                } else {
                    playlistUri = movieResponse?.Source?.HlsUri;
                    format = MediaFormatType.HLS;
                }

                audio = movieResponse.Source.Audio.Names;
                name = movieResponse.Title;
                detailsItem = movieResponse;
            }

            mediaStreams = await GetPlaylistStreams(playlistUri, format);

            foreach (var mediaStream in mediaStreams) {
                mediaStream.Title = $"{name}" + "\n" + string.Join(" / ", audio);

                var ckDetails = CACHE_KEY_API_DETAILS
                    .Replace("[uri]", mediaStream.GetOriginalUrl().ToString());
                cache.Set(ckDetails, detailsItem, DEFAULT_EXPIRATION);
            }

            cache.Set(ckStreams, mediaStreams, DEFAULT_EXPIRATION);

            return mediaStreams;
        }

        public async Task<IMediaSource> GetMedia(MediaLink link, RangeHeaderValue range = null) {
            var mimeType = MediaMimeType.GetMimeType(link.FormatType);

            var ckPlaylist = CACHE_KEY_PLAYLIST
                .Replace("[uri]", link.GetUri().ToString())
                .Replace("[quality]", link.Quality);
            var playlistBytes = cache.Get<byte[]>(ckPlaylist);
            if (playlistBytes != null) {
                return new PlaylistMediaSource(mimeType, playlistBytes);
            }

            string[] audioTracks = null;
            var ckDetails = CACHE_KEY_API_DETAILS
                .Replace("[uri]", link.GetUri().ToString());
            var item = cache.Get<object>(ckDetails);
            if (item is CollapseMovieResponse movieItem) {
                audioTracks = movieItem.Source?.Audio?.Names;
            } else if (item is CollapseTvEpisodeReponse episodeItem) {
                audioTracks = episodeItem.Audio?.Names;
            }

            var playlist = await GetPlaylist(link.GetUri(), link.FormatType, link.Quality, audioTracks);
            playlistBytes = Encoding.UTF8.GetBytes(playlist);
            cache.Set(ckPlaylist, playlistBytes, DEFAULT_EXPIRATION);

            return new PlaylistMediaSource(mimeType, playlistBytes);
        }

        string JsStringToJsonString(string jsString) {
            var json = jsString;

            var matches = Regex.Matches(json, @"\""{0,1}([a-zA-Z0-9]{2,}\:)");
            foreach (Match match in matches) {
                if (match.Value.StartsWith("\"")) { continue; }

                json = json.Replace(match.Value, $"\"{match.Value.TrimEnd(':')}\":");
            }

            var numMatch = Regex.Match(json, "[0-9 ]+\\*[0-9 ]+");
            // value doesn't really matter atm
            json = json.Replace(numMatch.Value, "60000");

            return json;
        }
    }
}