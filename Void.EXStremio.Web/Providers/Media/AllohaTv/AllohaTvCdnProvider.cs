using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Providers.Media.AllohaTv.Models;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Providers.Media.AllohaTv {
    public class AllohaTvConfig {
        public const string CONFIG_API_KEY = "ALLOHA_API_KEY";

        public string ApiKey { get; }

        public AllohaTvConfig(string apiKey) {
            ApiKey = apiKey;
        }
    }

    class AllohaTvCdnProvider : IKinopoiskIdProvider {
        const string PREFIX = "kp";

        readonly IHttpClientFactory httpClientFactory;
        readonly AllohaTvConfig config;

        const string baseSearchUri = "https://api.alloha.tv/?token=[token]&imdb=[id]";

        public AllohaTvCdnProvider(IHttpClientFactory httpClientFactory, AllohaTvConfig config) {
            this.httpClientFactory = httpClientFactory;
            this.config = config;
        }

        public async Task<string?> GetKinopoiskIdAsync(string imdbId) {
            using (var client = httpClientFactory.CreateClient()) {
                var uriString = baseSearchUri
                                    .Replace("[token]", config.ApiKey)
                                    .Replace("[id]", imdbId);

                var response = await client.GetAsync(uriString);
                if (!response.IsSuccessStatusCode) {
                    // TODO: logging?
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var searchResponse = await JsonSerializerExt.DeserializeAsync<AllohaTvSearchResponse>(json);

                var kpId = searchResponse?.Data?.KpId?.ToString();
                return string.IsNullOrWhiteSpace(kpId) ? null : PREFIX + kpId;
            }
        }
    }
}
