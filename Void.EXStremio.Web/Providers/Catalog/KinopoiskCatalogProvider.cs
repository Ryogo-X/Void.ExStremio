using System.Net.Http.Headers;
using System.Text.Json;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Models.Kinopoisk;
using Void.EXStremio.Web.Models.Kinopoisk.Query;
using Void.EXStremio.Web.Models.Kinopoisk.Response;

namespace Void.EXStremio.Web.Providers.Catalog {
    public class KinopoiskCatalogProvider : ICatalogProvider {
        public const string HTTP_CLIENT_KEY = "kinopoisk";
        private readonly IHttpClientFactory httpClientFactory;

        const string baseUri = "https://graphql.kinopoisk.ru/graphql?operationName={0}";

        public KinopoiskCatalogProvider(IHttpClientFactory httpClientFactory) {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<CatalogResponse<KinopoiskMeta>> GetAsync(string type, string searchQuery) {
            using (var client = httpClientFactory.CreateClient(HTTP_CLIENT_KEY)) {
                var searchSuggestQuery = new SearchSuggestQuery(searchQuery);
                var query = searchSuggestQuery.GetQuery();
                var queryString = JsonSerializer.Serialize(query, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var uri = new Uri(string.Format(baseUri, SearchSuggestQuery.OPERATION_NAME));

                var content = new StringContent(queryString, MediaTypeHeaderValue.Parse("application/json"));
                var response = await client.PostAsync(uri, content);
                var searchSuggestResponse = await response.Content.ReadFromJsonAsync<GraphqlResponse<SearchSuggestKpResponse>>();

                //TODO: extract into converter class
                return new CatalogResponse<KinopoiskMeta>() {
                    Query = searchQuery,
                    Rank = 1,
                    CacheMaxAge = 21600,
                    Metas = searchSuggestResponse.Data.Suggest.Global.Items.Select(x => {
                        var converter = new KpItemToMetaConverter(x.Global);
                        return converter.ToMeta();
                    }).ToArray()
                };
            }
        }
    }

    class KpItemToMetaConverter {
        MovieItemKpResponse kpItem;

        public KpItemToMetaConverter(MovieItemKpResponse kpItem) {
            this.kpItem = kpItem;
        }

        public KinopoiskMeta ToMeta() {
            var meta = new KinopoiskMeta();
            meta.Name = GetTitle();
            meta.Year = GetYear().ToString();
            meta.ReleaseInfo = GetYears();
            meta.StartYear = GetStartYear();
            meta.EndYear = GetEndYear();
            meta.KpId = kpItem.Id.ToString();
            meta.KpRating = kpItem.Rating?.Kinopoisk?.Value?.ToString();
            meta.Poster = kpItem.Gallery.Posters.Vertical != null ? new Uri("https:" + kpItem.Gallery.Posters.Vertical.Url) : null;
            meta.Type = GetItemType();
            var localizedName = GetLocalizedTitle();
            if (!string.IsNullOrEmpty(localizedName)) {
                meta.LocalizedTitles.Add(new LocalizedTitle("ru", localizedName));
            }

            return meta;
        }

        string GetTitle() {
            if (!string.IsNullOrEmpty(kpItem.Title.Localized)) {
                return kpItem.Title.Original;
            }

            return null;
        }

        string GetLocalizedTitle() {
            if (!string.IsNullOrEmpty(kpItem.Title.Localized)) {
                return kpItem.Title.Localized;
            }

            return kpItem.Title.Original;
        }

        int? GetYear() {
            if (kpItem.Year.HasValue) {
                return kpItem.Year.Value;
            } else if (kpItem.FallbackYear.HasValue) {
                return kpItem.FallbackYear.Value;
            }

            return null;
        }

        string GetYears() {
            if (kpItem.Years?.Any() != true) { return null; }

            return $"{kpItem.Years[0].Start}-{kpItem.Years[0].End}";
        }

        int? GetStartYear() {
            if (kpItem.Years?.Any() != true) { return null; }

            return kpItem.Years[0].Start;
        }

        int? GetEndYear() {
            if (kpItem.Years?.Any() != true) { return null; }

            return kpItem.Years[0].End;
        }

        string GetItemType() {
            if (kpItem.Type == "TvSeries") {
                return "series";
            } else if (kpItem.Type == "Film") {
                return "movie";
            }

            return "any";
        }
    }
}
