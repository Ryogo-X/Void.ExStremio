using Void.EXStremio.Web.Models;

namespace Void.EXStremio.Web.Providers.Metadata {
    public class CinemetaMetaProvider : IMetadataProvider {
        const string baseUri = "https://v3-cinemeta.strem.io/meta/type/id.json";
        readonly IHttpClientFactory httpClientFactory;

        public CinemetaMetaProvider(IHttpClientFactory httpClientFactory) {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<ExtendedMeta?> GetMetadataAsync(string type, string id) {
            var uriString = baseUri
                .Replace("id", id)
                .Replace("type", type);
            var uri = new Uri(uriString);

            using (var client = httpClientFactory.CreateClient()) {
                var response = await client.GetFromJsonAsync<MetaResponse<ExtendedMeta>>(uri);

                return response?.Meta;
            }
        }
    }
}