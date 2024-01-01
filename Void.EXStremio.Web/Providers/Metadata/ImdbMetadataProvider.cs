using Void.EXStremio.Web.Models;
using AngleSharp;
using AngleSharp.Dom;

namespace Void.EXStremio.Web.Providers.Metadata {
    public class ImdbMetadataProvider {
        public async Task<Meta> Get(string id) {
            using (var client = new HttpClient()) {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.5");
                var url = "https://www.imdb.com/title/" + id;
                var html = await client.GetStringAsync(url);

                var config = Configuration.Default.WithDefaultLoader();
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(response => {
                    response.Address(url);
                    response.Content(html);
                });

                var years = GetYear(document);
                return new Meta() {
                    Id = id,
                    ImdbId = id,
                    Name = GetTitle(document),
                    OriginalName = GetOriginalTitle(document),
                    Year = years.startYear,
                    StartYear = years.startYear,
                    EndYear = years.endYear
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
    }
}
