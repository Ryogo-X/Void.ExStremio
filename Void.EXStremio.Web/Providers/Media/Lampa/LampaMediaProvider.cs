using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Utility;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    abstract class LampaMediaProvider : MediaProviderBase, IMediaProvider, IInitializableProvider {
        protected const string KP_PREFIX = "kp";
        protected const string IMDB_PREFIX = "tt";

        readonly TimeSpan DEFAULT_EXPIRATION = TimeSpan.FromMinutes(4 * 60);
        readonly string CACHE_KEY_MOVIE_STREAMS;
        readonly string CACHE_KEY_TV_STREAMS;

        // ?life=true
        protected const string initBase = "?life=true";
        protected const string initMovieUriArgs = "?id=603&imdb_id=tt0133093&kinopoisk_id=301&serial=0";
        protected const string initTvUriArgs = "?id=1396&imdb_id=tt0903747&kinopoisk_id=404900&serial=1";
        protected const string initAnimeUriArgs = "?id=9323&imdb_id=tt0113568&kinopoisk_id=8228&serial=1";

        LampaSrc[] videoSources = [];

        public abstract override string ServiceName { get; }

        protected abstract Uri BaseUri { get; }
        protected virtual string InitUriPath { get; } = "/lite/events";

        protected abstract string[] AllowedCdn { get; }

        protected LampaMediaProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) {
            CACHE_KEY_MOVIE_STREAMS = $"{ServiceName}:STREAMS:[uri]";
            CACHE_KEY_TV_STREAMS = $"{ServiceName}:STREAMS:TV:[uri]";
        }

        #region IInitializableProvider
        public bool IsInitialized { get; private set; }
        public bool IsReady { get; private set; }

        public virtual async Task Initialize() {
            var sources = new List<LampaSrc>();

            try {
                using (var client = GetHttpClientEx()) {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    // base
                    {
                        var json = await client.GetStringAsync(new Uri(BaseUri, InitUriPath + initBase));
                        var newSources = await JsonSerializerExt.DeserializeAsync<LampaSrc[]>(json);
                        sources.AddRange(newSources);
                    }

                    // movie
                    {
                        var json = await client.GetStringAsync(new Uri(BaseUri, InitUriPath + initMovieUriArgs));
                        var newSources = await JsonSerializerExt.DeserializeAsync<LampaSrc[]>(json);
                        sources.AddRange(newSources);
                    }

                    // tv
                    {
                        var json = await client.GetStringAsync(new Uri(BaseUri, InitUriPath + initTvUriArgs));
                        var newSources = await JsonSerializerExt.DeserializeAsync<LampaSrc[]>(json);
                        sources.AddRange(newSources);
                    }

                    // anime
                    {
                        var json = await client.GetStringAsync(new Uri(BaseUri, InitUriPath + initAnimeUriArgs));
                        var newSources = await JsonSerializerExt.DeserializeAsync<LampaSrc[]>(json);
                        sources.AddRange(newSources);
                    }

                    var providers = sources
                        .Select(x => new Uri(x.Url).GetLeftPart(UriPartial.Path))
                        .Where(x => !AllowedCdn.Any() || AllowedCdn.Any(p => x.Contains(p)))
                        .Distinct().OrderBy(x => x).ToArray();
                    videoSources = providers.Select(url => {
                        var src = sources.First(x => x.Url.Contains(url));

                        return new LampaSrc(src.Name.Split(' ').First().ToUpper(), url, src.Balanser);
                    }).ToArray();
                    //videoSources = sources.Distinct().OrderBy(x => x.Balanser).ToArray();
                    IsReady = true;
                }
            } catch (Exception) {
                throw;
            } finally {
                IsInitialized = true;
            }
        }
        #endregion

        #region IMediaProvider
        public bool CanGetStreams(string id) {
            return id.StartsWith(IMDB_PREFIX) || id.StartsWith(KP_PREFIX);
        }

        public bool CanGetMedia(MediaLink link) {
            return false;
        }

        public async Task<MediaStream[]> GetStreams(string id, int? season = null, int? episode = null, ExtendedMeta meta = null) {
            if (meta == null) { throw new InvalidOperationException($"[{nameof(LampaMediaProvider)}] -> meta is NULL"); }

            var mediaStreams = new List<MediaStream>();
            foreach (var videoSource in videoSources) {
                var uriString = videoSource.Url;
                if (uriString.Contains("?")) {
                    uriString += "&rjson=true";
                } else {
                    uriString += "?rjson=true";
                }

                if (meta.TmdbId != 0) {
                    uriString += $"&id={meta.TmdbId}&source=tmdb";
                }
                if (!string.IsNullOrWhiteSpace(meta.ImdbId)) {
                    uriString += $"&imdb_id={meta.ImdbId}";
                }
                if (!string.IsNullOrWhiteSpace(meta.KpId)) {
                    uriString += $"&kinopoisk_id={meta.KpId.Replace("kp", "")}";
                }
                uriString += $"&original_title={meta.Name}&year={meta.GetYear()}";
                if (meta.LocalizedTitles.Any(x => x.LangCode == "ru")) {
                    uriString += $"&title={meta.LocalizedTitles.First(x => x.LangCode == "ru").Title}";
                }
                if (!season.HasValue) {
                    uriString += "&serial=0";
                } else {
                    uriString += $"&serial=1";
                }

                var newStreams = await GetStreams(uriString, videoSource.Balanser, season, episode);
                mediaStreams.AddRange(newStreams);
            }

            return mediaStreams.ToArray();
        }

        async Task<MediaStream[]> GetStreams(string uri, string balancer, int? season = null, int? episode = null) {
            var streams = new List<MediaStream>();

            try {
                using (var client = GetHttpClientEx()) {
                    var json = await client.GetStringAsync(uri, true, DEFAULT_EXPIRATION);
                    if (string.IsNullOrWhiteSpace(json)) { return []; }

                    var apiResponse = await JsonSerializerExt.DeserializeAsync<LampaApiResponse>(json);
                    if (apiResponse.GetResponseType() == LampaApiResponseType.Movie) {
                        var movieResponse = await JsonSerializerExt.DeserializeAsync<LampaMovieApiResponse>(json);
                        var movieItems = movieResponse?.GetItems() ?? [];
                        foreach (var movieItem in movieItems) {
                            var newStreams = await GetStreams(movieItem, balancer);
                            streams.AddRange(newStreams);
                        }
                    } else if (apiResponse.GetResponseType() == LampaApiResponseType.Season) {
                        var seasonResponse = await JsonSerializerExt.DeserializeAsync<LampaSeasonApiResponse>(json);
                        var seasonItems = seasonResponse.GetItems();
                        var seasonItem = seasonItems.FirstOrDefault(x => x.Id == season);
                        if (seasonItem != null) { 
                            json = await client.GetStringAsync(seasonItem.Url + "&rjson=true", true, DEFAULT_EXPIRATION);
                            var episodeApiResponse = await JsonSerializerExt.DeserializeAsync<LampaEpisodeApiResponse>(json);
                            if (episodeApiResponse.Type != "similar") {
                                var translators = episodeApiResponse.Translators;
                                if (translators == null) {
                                    translators = [new LampaVoiceApiRespinse {
                                        Method = "episode",
                                        Url = seasonItem.Url,
                                        Active = true,
                                        Name = "Неизвестно"
                                    }];
                                } else {
                                    var defaultTranslator = translators.FirstOrDefault(x => x.Name == "По умолчанию");
                                    if (defaultTranslator != null) {
                                        defaultTranslator.Name = "Неизвестно";
                                    }
                                }
                                foreach (var translator in translators) {
                                    json = await client.GetStringAsync(translator.Url + "&rjson=true", true, DEFAULT_EXPIRATION);
                                    episodeApiResponse = await JsonSerializerExt.DeserializeAsync<LampaEpisodeApiResponse>(json);
                                    var episodeItems = episodeApiResponse.GetItems();
                                    var episodeItem = episodeItems.FirstOrDefault(x => x.Episode == episode);
                                    if (episodeItem != null) {
                                        var newStreams = await GetStreams(episodeItem, balancer);
                                        foreach(var newStream in newStreams) {
                                            if (newStream.Title?.Contains("translator.Name") == true) { continue; }

                                            newStream.Title += $"\n{translator.Name}";
                                        }

                                        streams.AddRange(newStreams);
                                    }
                                }
                            }
                        }
                    } else if (apiResponse.GetResponseType() == LampaApiResponseType.Episode) {
                        var episodeApiResponse = await JsonSerializerExt.DeserializeAsync<LampaEpisodeApiResponse>(json);
                        if (episodeApiResponse.Type != "similar") {
                            var episodeItems = episodeApiResponse.GetItems();
                            var episodeItem = episodeItems.FirstOrDefault(x => x.Episode == episode);
                            if (episodeItem != null) {
                                var newStreams = await GetStreams(episodeItem, balancer);
                                foreach (var newStream in newStreams) {
                                    newStream.Title += $"\nНеизвестно";
                                }

                                streams.AddRange(newStreams);
                            }
                        }
                    } else if (apiResponse.GetResponseType() == LampaApiResponseType.Call) {
                        throw new InvalidOperationException($"[{nameof(LampaMediaProvider)}] -> Invalid response type: {LampaApiResponseType.Call}");
                    } else {
                        //TODO: logging
                        //throw new InvalidOperationException($"[{nameof(LampaVideoCdnProvider)}] -> Unsupported api response type");
                        return [];
                    }
                }

            } catch (Exception) {
                //TODO: logging
            }

            return streams.ToArray();
        }

        async Task<MediaStream[]> GetStreams(LampaCallPlayApiResponse apiResponse, string balancer) {
            if (apiResponse.Title?.Contains("Заблокировано правообладателем") == true || apiResponse.Translate?.Contains("Заблокировано правообладателем") == true) { return []; }
            if (cache.TryGetValue<MediaStream[]>(apiResponse.GetId(), out var streamItems)) { return streamItems; }

            var streams = new List<MediaStream>();
            
            if (apiResponse.Method == "play") {
                if (apiResponse.Links != null) {
                    foreach (var link in apiResponse.Links) {
                        var name = $"[{balancer.ToUpper()}]"; //$"[{balancer?.ToUpper()} / {ServiceName.ToUpperInvariant()}]";

                        var quality = GetQuality(apiResponse.Details) ?? GetQuality(link.Key);
                        if (!string.IsNullOrWhiteSpace(quality)) {
                            name = $"{name}\n[{quality}]";
                        } else {
                            name = $"{name}\n[{link.Key}]";
                        }

                        streams.Add(new MediaStream {
                            ProviderName = ServiceName,
                            CdnName = balancer,
                            Name = name,
                            Title = apiResponse.Translate ?? apiResponse.Title,
                            Url = apiResponse.Url
                        });
                    }
                } else {
                    var name = $"[{balancer.ToUpper()}]"; //$"[{balancer?.ToUpper()} / {ServiceName.ToUpperInvariant()}]";
                    if (apiResponse.Url.Contains(".m3u")) {
                        var items = await GetHlsPlaylistStreams(new Uri(apiResponse.Url), false);
                        if (items.Any()) {
                            foreach (var item in items) {
                                var quality = GetQuality(item.Name);

                                streams.Add(new MediaStream {
                                    ProviderName = ServiceName,
                                    CdnName = balancer,
                                    Name = $"{name}\n[{quality}]",
                                    Title = apiResponse.Translate ?? apiResponse.Title,
                                    Url = item.Url
                                });
                            }
                        } else {
                            var quality = GetQuality(apiResponse.Details) ?? GetQuality(apiResponse.Url) ?? GetQuality(apiResponse.Title) ?? GetQuality(apiResponse.Translate);
                            if (!string.IsNullOrWhiteSpace(quality)) {
                                name = $"{name}\n[{quality}]";
                            }

                            streams.Add(new MediaStream {
                                ProviderName = ServiceName,
                                CdnName = balancer,
                                Name = name,
                                Title = apiResponse.Translate ?? apiResponse.Title,
                                Url = apiResponse.Url
                            });
                        }
                    } else {
                        var quality = GetQuality(apiResponse.Details) ?? GetQuality(apiResponse.Url) ?? GetQuality(apiResponse.Title) ?? GetQuality(apiResponse.Translate);
                        if (!string.IsNullOrWhiteSpace(quality)) {
                            name = $"{name}\n[{quality}]";
                        }

                        streams.Add(new MediaStream {
                            ProviderName = ServiceName,
                            CdnName = balancer,
                            Name = name,
                            Title = apiResponse.Translate ?? apiResponse.Title,
                            Url = apiResponse.Url
                        });
                    }
                }
            } else if (apiResponse.Method == "call") {
                using (var client = GetHttpClientEx()) {
                    var json = await client.GetStringAsync(apiResponse.Url, true, DEFAULT_EXPIRATION);
                    var nestedApiResponse = await JsonSerializerExt.DeserializeAsync<LampaCallPlayApiResponse>(json);

                    var newStreams = await GetStreams(nestedApiResponse, balancer);
                    streams.AddRange(newStreams);
                }
            }

            streamItems = streams.ToArray();
            cache.Set(apiResponse.GetId(), streamItems, DEFAULT_EXPIRATION);

            return streamItems;
        }

        public Task<IMediaSource> GetMedia(MediaLink link, RangeHeaderValue range = null) {
            throw new NotImplementedException();
        }
        #endregion

        protected override HttpClient GetHttpClient() {
            return httpClientFactory.CreateClient(nameof(LampaMediaProvider));
        }

        HttpClientEx GetHttpClientEx() {
            var client = GetHttpClient();
            return new HttpClientEx(client, cache);
        }

        string GetQuality(string uri) {
            uri ??= "";

            var match = Regex.Match(uri, "([0-9]{3,4})p");
            if (!match.Success) { return null; }

            return match.Value;
        }
    }

    record LampaSrc(string Name, string Url, string Balanser);

    enum LampaApiResponseType {
        Unknown = 0,
        Movie = 1,
        Season = 2,
        Episode = 3,
        Call = 4
    }

    interface ILampaApiResponse {
        string Type { get; set; }
    }

    interface ILampaNestedApiResponse {
        string Method { get; set; }
    }

    class LampaApiResponse : ILampaApiResponse {
        public string Type { get; set; }

        public JsonElement Data { get; set; }

        public LampaApiResponseType GetResponseType() {
            if (Type == "movie") {
                return LampaApiResponseType.Movie;
            } else if (Type == "season") {
                return LampaApiResponseType.Season;
            } else if (Type == "episode") {
                return LampaApiResponseType.Episode;
            } else if (Type == "call") {
                return LampaApiResponseType.Call;
            }

            return LampaApiResponseType.Unknown;
        }
    }

    class LampaMovieApiResponse : LampaApiResponse {
        public LampaCallPlayApiResponse[] GetItems() {
            return Data.Deserialize<LampaCallPlayApiResponse[]>() ?? [];
        }
    }

    class LampaSeasonApiResponse : LampaApiResponse {
        public LampaLinkApiResponse[] GetItems() {
            return Data.Deserialize<LampaLinkApiResponse[]>() ?? [];
        }
    }

    class LampaEpisodeApiResponse : LampaApiResponse {
        [JsonPropertyName("voice")]
        public LampaVoiceApiRespinse[] Translators { get; set; }

        public LampaCallPlayApiResponse[] GetItems() {
            return Data.Deserialize<LampaCallPlayApiResponse[]>() ?? [];
        }
    }

    class LampaCallPlayApiResponse : ILampaNestedApiResponse {
        [JsonPropertyName("method")]
        public string Method { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("details")]
        public string Details { get; set; }
        [JsonPropertyName("translate")]
        public string Translate { get; set; }
        [JsonPropertyName("maxquality")]
        public string MaxQuality { get; set; }
        [JsonPropertyName("quality")]
        public Dictionary<string, string> Links { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("s")]
        public int? Season { get; set; }
        [JsonPropertyName("e")]
        public int? Episode { get; set; }

        public string GetId() {
            return nameof(LampaCallPlayApiResponse) + (Url + string.Join("", Links ?? [])).GetHashCode();
        }
    }

    class LampaLinkApiResponse : ILampaNestedApiResponse {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("method")]
        public string Method { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    class LampaVoiceApiRespinse : ILampaNestedApiResponse {
        [JsonPropertyName("method")]
        public string Method { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("active")]
        public bool Active { get; set; }
    }
}