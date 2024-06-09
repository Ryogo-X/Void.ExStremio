using Void.EXStremio.Web.Models;
using AngleSharp;
using AngleSharp.Dom;

namespace Void.EXStremio.Web.Providers.Metadata {
    public class ImdbMetaProvider : IAdditionalMetadataProvider {
        public const string HTTP_CLIENT_KEY = "imdb";
        const string PREFIX = "tt";

        readonly IHttpClientFactory httpClientFactory;
        const string baseUri = "https://www.imdb.com/title/";

        public ImdbMetaProvider(IHttpClientFactory httpClientFactory) {
            this.httpClientFactory = httpClientFactory;
        }

        public bool CanGetAdditionalMetadata(string id) {
            return id.StartsWith(PREFIX);
        }

        public async Task<ExtendedMeta?> GetAdditionalMetadataAsync(string type, string id) {
            using (var client = httpClientFactory.CreateClient(HTTP_CLIENT_KEY)) {

                var url = baseUri + id;
                var html = await client.GetStringAsync(url);

                var config = Configuration.Default.WithDefaultLoader();
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(response => {
                    response.Address(url);
                    response.Content(html);
                });

                var years = GetYear(document);
                return new ExtendedMeta() {
                    Id = id,
                    ImdbId = id,
                    Name = GetTitle(document),
                    // TODO: FIX?
                    //OriginalName = GetOriginalTitle(document),
                    Year = years.startYear?.ToString(),
                    StartYear = years.startYear,
                    EndYear = years.endYear,
                    Type = IsSeries(document) ? "series" : "movie"
                };
            }
        }
        
        string GetTitle(IDocument document) {
            return document.QuerySelector("h1[data-testid=hero__pageTitle]").TextContent.Trim();
        }

        string GetOriginalTitle(IDocument document) {
            var text = document.QuerySelector("h1[data-testid=hero__pageTitle]").NextSibling.TextContent.Trim();
            return text.StartsWith("Original title: ") ? text.Replace("Original title: ", "") : null;
        }

        (int? startYear, int? endYear) GetYear(IDocument document) {
            var yearString = document.QuerySelector("a[href$=tt_ov_rdat]").TextContent.Trim();
            var parts = yearString.Split('–');

            if (parts.Length == 2) {
                int.TryParse(parts[0], out var startYear);
                int.TryParse(parts[1], out var endYear);

                return (startYear > 0 ? startYear: null, endYear > 0 ? endYear : null);
            } else {
                int.TryParse(parts[0], out var startYear);

                return (startYear > 0 ? startYear : null, null);
            }
        }

        bool IsSeries(IDocument document) {
            return document.QuerySelectorAll(".ipc-inline-list__item").Any(x => x.Text().Trim() == "TV Series");
        }
    }
}
