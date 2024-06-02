using System.Reflection;

namespace Void.EXStremio.Web.Models.Kinopoisk.Query {
    public class SearchSuggestQuery {
        public const string OPERATION_NAME = "SearchSuggest";

        public string Keyword { get; set; }
        public string[] Types { get; set; } = ["MOVIE", "CINEMA"];
        public bool IsOnlyOnline { get; set; }
        public bool OnlySearchable { get; set; }
        public long Limit { get; set; } = 10;
        public long RegionId { get; set; } = 213;
        public object Latitude { get; set; }
        public object Longitude { get; set; }
        public bool IncludeMovieTops { get; set; }
        public bool IncludeMovieRating { get; set; } = true;
        public bool IncludeSeriesSeasonsCount { get; set; } = true;
        public bool IncludeFilmDuration { get; set; } = true;
        public bool IncludeMovieHorizontalCover { get; set; } = true;
        public bool IncludeMovieHorizontalLogo { get; set; } = true;
        public bool IncludeMovieRightholderForPoster { get; set; }
        public bool IncludeMovieUserVote { get; set; }
        public bool IncludeMovieUserPlannedToWatch { get; set; }
        public bool IncludeMovieUserFolders { get; set; }
        public bool IncludeMovieUserWatched { get; set; }
        public bool IncludeMovieUserNotInterested { get; set; }
        public bool IncludeCinemaUserData { get; set; }
        public bool IncludeMovieListMetaTotal { get; set; }
        public bool IncludePersonAgeAndDates { get; set; }
        public bool IncludeMovieContentFeatures { get; set; }
        public bool IncludeMovieOnlyClientSupportedContentFeatures { get; set; }
        public bool IncludeMovieViewOption { get; set; }
        public bool IncludeMovieTop250 { get; set; }

        public SearchSuggestQuery(string keyword) {
            Keyword = keyword;
        }

        public GraphqlQuery<SearchSuggestQuery> GetQuery() {
            var queryString = string.Empty;
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().First(x => x.Contains(nameof(SearchSuggestQuery)));
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                using(var reader = new StreamReader(stream)) {
                    queryString = reader.ReadToEnd()
                        .Replace("\r\n", "");
                }
            }

            var query = new GraphqlQuery<SearchSuggestQuery>();
            query.OperationName = OPERATION_NAME;
            query.Variables = this;
            query.Query = queryString;

            return query;
        }
    }
}
