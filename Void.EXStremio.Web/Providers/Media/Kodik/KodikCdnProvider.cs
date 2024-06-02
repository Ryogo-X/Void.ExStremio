﻿using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Providers.Media.Kodik.Models;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Providers.Media.Kodik {
    public class KodikConfig {
        public const string CONFIG_API_KEY = "KODIK_API_KEY";

        public string ApiKey { get; }

        public KodikConfig(string apiKey) {
            ApiKey = apiKey;
        }
    }

    class KodikCdnProvider : MediaProviderBase, IMediaProvider, IKinopoiskIdProvider {
        const string PREFIX = "kp";

        readonly KodikConfig config;

        const string baseSearchUri = "https://kodikapi.com/search?token=[token]&kinopoisk_id=[id]&with_episodes=true";

        readonly TimeSpan DEFAULT_EXPIRATION = TimeSpan.FromMinutes(8 * 60);
        readonly string CACHE_KEY_API_SEARCH_KP;
        readonly string CACHE_KEY_API_SEARCH_IMDB;
        readonly string CACHE_KEY_STREAMS;

        public override string ServiceName {
            get { return "Kodik"; }
        }

        public KodikCdnProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, KodikConfig config) : base(httpClientFactory, cache) {
            this.config = config;

            CACHE_KEY_API_SEARCH_KP = $"{ServiceName}:API:SEARCH:KP:[id]";
            CACHE_KEY_API_SEARCH_IMDB = $"{ServiceName}:API:SEARCH:IMDB:[id]";
            CACHE_KEY_STREAMS = $"{ServiceName}:STREAMS:[uri]:[season]:[episode]";
        }

        public bool CanHandle(string id) {
            return id.StartsWith(PREFIX);
        }

        public bool CanHandle(MediaLink link) {
            if (link.SourceType.ToUpperInvariant() != ServiceName.ToUpperInvariant()) { return false; }

            return true;
        }

        public async Task<string?> GetKinopoiskIdAsync(string imdbId) {
            string kpId = null;

            var ckSearchImdb = CACHE_KEY_API_SEARCH_IMDB
                .Replace("[id]", imdbId);
            var searchResponse = cache.Get<KodikSearchResponse>(ckSearchImdb);
            if (searchResponse == null) {
                using (var client = GetHttpClient()) {
                    var uriString = baseSearchUri
                                        .Replace("[token]", config.ApiKey)
                                        .Replace("[id]", imdbId);

                    var response = await client.GetAsync(uriString);
                    if (!response.IsSuccessStatusCode) {
                        // TODO: logging?
                        return null;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    searchResponse = await JsonSerializerExt.DeserializeAsync<KodikSearchResponse>(json);
                    cache.Set(ckSearchImdb, searchResponse, DEFAULT_EXPIRATION);

                    kpId = searchResponse?.Results.FirstOrDefault()?.KpId?.ToString();
                    if (string.IsNullOrWhiteSpace(kpId)) { return null; }

                    var ckSearchKp = CACHE_KEY_API_SEARCH_KP
                        .Replace("[id]", kpId);
                    cache.Set(ckSearchKp, searchResponse, DEFAULT_EXPIRATION);

                    return PREFIX + kpId;
                }
            }

            kpId = searchResponse?.Results.FirstOrDefault()?.KpId?.ToString();
            if (string.IsNullOrWhiteSpace(kpId)) { return null; }

            return PREFIX + kpId;
        }

        public async Task<MediaStream[]> GetStreams(string id, int? season = null, int? episode = null) {
            if (id.StartsWith(PREFIX)) { id = id.Replace(PREFIX, ""); }

            var ckSearchKp = CACHE_KEY_API_SEARCH_KP
                .Replace("[id]", id);
            var searchResponse = cache.Get<KodikSearchResponse>(ckSearchKp);
            if (searchResponse == null) {
                using (var client = GetHttpClient()) {
                    var uriString = baseSearchUri
                                        .Replace("[token]", config.ApiKey)
                                        .Replace("[id]", id);

                    var response = await client.GetAsync(uriString);
                    if (!response.IsSuccessStatusCode) {
                        // TODO: logging?
                        return [];
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    searchResponse = await JsonSerializerExt.DeserializeAsync<KodikSearchResponse>(json);
                    cache.Set(ckSearchKp, searchResponse, DEFAULT_EXPIRATION);
                }
            }

            MediaStream[] mediaStreams = [];
            foreach (var responseItem in searchResponse.Results) {
                var iframeUri = "https:" + responseItem.Link;
                if (season.HasValue && episode.HasValue) {
                    var episodeLink = responseItem.Seasons
                        .FirstOrDefault(x => x.Key == season.ToString()).Value?
                        .Episodes.FirstOrDefault(x => x.Key == episode.ToString()).Value;
                    if (string.IsNullOrWhiteSpace(episodeLink)) { continue; }

                    iframeUri = $"http:{episodeLink}?season={season}&episode={episode}";
                }

                var ckStreams = CACHE_KEY_STREAMS
                    .Replace("[uri]", iframeUri.ToString())
                    .Replace("[season]", season?.ToString())
                    .Replace("[episode]", season?.ToString());
                mediaStreams = cache.Get<MediaStream[]>(ckStreams);
                if (mediaStreams == null) {
                    mediaStreams = await GetStreams(iframeUri, season, episode);
                    foreach (var mediaStream in mediaStreams) {
                        if (season.HasValue && episode.HasValue) {
                            mediaStream.Title = $"Episode {episode?.ToString("000")}\n{responseItem.Translation.Title}";
                        } else {
                            mediaStream.Title = string.IsNullOrWhiteSpace(responseItem.TitleOriginal) ? responseItem.Title : $"{responseItem.Title} / {responseItem.TitleOriginal}\n{responseItem.Translation.Title}";
                        }
                    }

                    cache.Set(ckStreams, mediaStreams, DEFAULT_EXPIRATION);
                }
            }

            return mediaStreams;
        }

        async Task<MediaStream[]> GetStreams(Uri iframeUri, int? season = null, int? episode = null) {
            var mediaStreams = new List<MediaStream>();

            using (var client = GetHttpClient()) {
                var html = await client.GetStringAsync(iframeUri);

                var requestParams = new Dictionary<string, string>();
                {
                    var domain = Regex.Match(html, "var domain = \"(?<domain>.*?)\";").Groups["domain"].Value;
                    var d_sign = Regex.Match(html, "var d_sign = \"(?<d_sign>.*?)\";").Groups["d_sign"].Value;
                    var pd = Regex.Match(html, "var pd = \"(?<pd>.*?)\";").Groups["pd"].Value;
                    var pd_sign = Regex.Match(html, "var pd_sign = \"(?<pd_sign>.*?)\";").Groups["pd_sign"].Value;
                    var @ref = Regex.Match(html, "var ref = \"(?<ref>.*?)\";").Groups["ref"].Value;
                    var ref_sign = Regex.Match(html, "var ref_sign = \"(?<ref_sign>.*?)\";").Groups["ref_sign"].Value;
                    var type = Regex.Match(html, "videoInfo.type = '(?<type>.*?)';").Groups["type"].Value;
                    var hash = Regex.Match(html, "videoInfo.hash = '(?<hash>.*?)';").Groups["hash"].Value;
                    var vid = Regex.Match(html, "videoInfo.id = '(?<id>.*?)';").Groups["id"].Value;

                    requestParams["domain"] = domain;
                    requestParams["d_sign"] = d_sign;
                    requestParams["pd"] = pd;
                    requestParams["pd_sign"] = pd_sign;
                    requestParams["ref"] = @ref;
                    requestParams["ref_sign"] = ref_sign;
                    requestParams["bad_user"] = "true";
                    requestParams["cdn_is_working"] = "true";
                    requestParams["type"] = type;
                    requestParams["hash"] = hash;
                    requestParams["id"] = vid;
                    requestParams["info"] = "{}";
                }
                var apiRespinse = await client.PostAsync("https://kodik.info/ftor", new FormUrlEncodedContent(requestParams));
                var videoResponse = await apiRespinse.Content.ReadFromJsonAsync<KodikVideoSourceResponse>();
                foreach (var linkPair in videoResponse.Links) {
                    var quality = linkPair.Key;
                    foreach (var videoLink in linkPair.Value) {
                        var mediaStream = new MediaStream() {
                            Name = $"[{ServiceName.ToUpperInvariant()}]\n[{quality}p]",
                            Url = new MediaLink(videoLink.Link, ServiceName, MediaFormatType.HLS, quality, MediaProxyType.Direct).ToString(),
                        };

                        mediaStreams.Add(mediaStream);
                    }
                }
            }

            return mediaStreams.ToArray();
        }

        public Task<IMediaSource> GetMedia(MediaLink link) {
            throw new NotImplementedException();
        }
    }
}