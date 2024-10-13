using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Providers.Media.Ashdi {
    public class AshdiConfig {
        public const string CONFIG_API_KEY = "ASHDI_API_KEY";

        public string ApiKey { get; }

        public AshdiConfig(string apiKey) {
            ApiKey = apiKey;
        }
    }

    class AshdiCdnProvider : MediaProviderBase, IKinopoiskIdProvider, IMediaProvider {
        const string KP_PREFIX = "kp";
        const string IMDB_PREFIX = "tt";
        const string baseSearchImdbUri = "https://base.ashdi.vip/api/product/read_one.php?api_key=[token]&imdb=[id]";
        const string baseSearchKpUri = "https://base.ashdi.vip/api/product/read_one.php?api_key=[token]&kinopoisk=[id]";

        readonly AshdiConfig config;

        readonly TimeSpan DEFAULT_EXPIRATION = TimeSpan.FromMinutes(4 * 60);
        readonly string CACHE_KEY_API_SEARCH_KP;
        readonly string CACHE_KEY_API_SEARCH_IMDB;

        public override string ServiceName {
            get { return "Ashdi"; }
        }

        public AshdiCdnProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, AshdiConfig config) : base(httpClientFactory, cache) {
            this.config = config;

            CACHE_KEY_API_SEARCH_KP = $"{ServiceName}:API:SEARCH:KP:[id]";
            CACHE_KEY_API_SEARCH_IMDB = $"{ServiceName}:API:SEARCH:IMDB:[id]";
        }

        public async Task<string?> GetKinopoiskIdAsync(string imdbId) {
            string kpId = null;

            var ckSearchImdb = CACHE_KEY_API_SEARCH_IMDB
                .Replace("[id]", imdbId);
            var searchResponse = cache.Get<AshdiSearchResponse>(ckSearchImdb);
            if (searchResponse == null) {
                using (var client = GetHttpClient()) {
                    var uriString = baseSearchImdbUri
                                        .Replace("[token]", config.ApiKey)
                                        .Replace("[id]", imdbId);

                    var response = await client.GetAsync(uriString);
                    if (!response.IsSuccessStatusCode) {
                        cache.Set(ckSearchImdb, new AshdiSearchResponse(), DEFAULT_EXPIRATION);

                        // TODO: logging?
                        return null;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    searchResponse = await JsonSerializerExt.DeserializeAsync<AshdiSearchResponse>(json);
                    cache.Set(ckSearchImdb, searchResponse, DEFAULT_EXPIRATION);

                    kpId = searchResponse?.KpId?.ToString();
                    if (string.IsNullOrWhiteSpace(kpId)) { return null; }

                    var ckSearchKp = CACHE_KEY_API_SEARCH_KP
                        .Replace("[id]", kpId);
                    cache.Set(ckSearchKp, searchResponse, DEFAULT_EXPIRATION);

                    return KP_PREFIX + kpId;
                }
            }

            kpId = searchResponse?.KpId?.ToString();
            if (string.IsNullOrWhiteSpace(kpId)) { return null; }

            return KP_PREFIX + kpId;
        }

        #region IMediaProvider
        public bool CanGetStreams(string id) {
            return id.StartsWith(KP_PREFIX);
        }

        public async Task<MediaStream[]> GetStreams(string id, int? season = null, int? episode = null) {
            // TODO: FIX

            //id = id.Replace(KP_PREFIX, "");
            //var uriString = baseSearchKpUri
            //        .Replace("[token]", config.ApiKey)
            //        .Replace("[id]", id);

            //var ckSearchKp = CACHE_KEY_API_SEARCH_KP
            //    .Replace("[id]", id);
            //var searchResponse = cache.Get<AshdiSearchResponse>(ckSearchKp);
            //if (searchResponse == null) {
            //    using (var client = GetHttpClient()) {
            //        var response = await client.GetAsync(uriString);
            //        if (!response.IsSuccessStatusCode) {
            //            cache.Set(ckSearchKp, new AshdiSearchResponse(), DEFAULT_EXPIRATION);
            //            // TODO: logging?
            //            return [];
            //        }

            //        var json = await response.Content.ReadAsStringAsync();
            //        searchResponse = await JsonSerializerExt.DeserializeAsync<AshdiSearchResponse>(json);
            //        cache.Set(ckSearchKp, searchResponse, DEFAULT_EXPIRATION);
            //    }
            //}
            //if (string.IsNullOrEmpty(searchResponse.Id)) { return []; }

            //using (var client = GetHttpClient()) {
            //    client.DefaultRequestHeaders.Referrer = new Uri("https://filmos.net/");
            //    var response = await client.GetAsync(searchResponse.Url);
            //    var html = await response.Content.ReadAsStringAsync();
            //    html.ToString();
            //}

            return [];
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
