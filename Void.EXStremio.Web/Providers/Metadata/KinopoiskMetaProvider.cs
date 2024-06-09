using System.Net.Http.Headers;
using System.Text.Json;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Models.Kinopoisk.Query;
using Void.EXStremio.Web.Models.Kinopoisk.Response;
using Void.EXStremio.Web.Providers.Catalog;

namespace Void.EXStremio.Web.Providers.Metadata {
    public class KinopoiskMetaProvider : IAdditionalMetadataProvider {
        const string PREFIX = "kp";
        const string baseUri = "https://graphql.kinopoisk.ru/graphql?operationName={0}";

        readonly IHttpClientFactory httpClientFactory;

        public KinopoiskMetaProvider(IHttpClientFactory httpClientFactory) {
            this.httpClientFactory = httpClientFactory;
        }

        public bool CanGetAdditionalMetadata(string id) {
            return id.StartsWith(PREFIX);
        }

        public async Task<ExtendedMeta?> GetAdditionalMetadataAsync(string type, string id) {
            if (!id.StartsWith(PREFIX)) { throw new InvalidOperationException($"{nameof(KinopoiskMetaProvider)} does not support id: {id}"); }
            id = id.Replace(PREFIX, "");

            using (var client = httpClientFactory.CreateClient(KinopoiskCatalogProvider.HTTP_CLIENT_KEY)) {
                var searchSuggestQuery = new GetDetailsQuery(int.Parse(id));
                var query = searchSuggestQuery.GetQuery();
                var queryString = JsonSerializer.Serialize(query, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var uri = new Uri(string.Format(baseUri, GetDetailsQuery.OPERATION_NAME));
                var content = new StringContent(queryString, MediaTypeHeaderValue.Parse("application/json"));
                var response = await client.PostAsync(uri, content);
                var json = await response.Content.ReadAsStringAsync();
                var movieDetailsResponse = await response.Content.ReadFromJsonAsync<GraphqlResponse<MovieDetailsKpResponse>>();

                var meta =  new ExtendedMeta() {
                    Year = movieDetailsResponse.Data.Movie.Year.ToString(),
                    Description = movieDetailsResponse.Data.Movie.Description,
                    KpId = id
                };

                if (!string.IsNullOrWhiteSpace(movieDetailsResponse.Data.Movie.Title.Localized)) {
                    meta.Name = movieDetailsResponse.Data.Movie.Title.Original;
                    meta.LocalizedTitles.Add(new LocalizedTitle("ru", movieDetailsResponse.Data.Movie.Title.Localized));
                } else {
                    meta.LocalizedTitles.Add(new LocalizedTitle("ru", movieDetailsResponse.Data.Movie.Title.Original));
                }

                return meta;
            }
        }
    }
}
