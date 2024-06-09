using System.Reflection;

namespace Void.EXStremio.Web.Models.Kinopoisk.Query {
    public class GetDetailsQuery {
        public const string OPERATION_NAME = "MovieDetails";

        public int MovieId { get; set; }
        public int CountryId { get; set; }
        public int CityId { get; set; } = 1;
        public int SequelsAndPrequelsLimit { get; set; }
        public int RelatedMoviesLimit { get; set; }
        public int ActorsLimit { get; set; }
        public int TrailersLimit { get; set; }
        public int CreatorsPerGroupLimit { get; set; }
        public int ImagesPerGroupLimit { get; set; }
        public int FactsLimit { get; set; }
        public int BloopersLimit { get; set; }
        public int CriticReviewsLimit { get; set; }
        public int UserReviewsLimit { get; set; }
        public int UserRecommendationMoviesLimit { get; set; }
        public int PostsLimit { get; set; }
        public int PremieresLimit { get; set; }
        public bool IsAppendUserData { get; set; }
        public bool IsInternationalUserData { get; set; }
        public int MovieListsLimit { get; set; } = 1;
        public List<string> SequelsAndPrequelsRelationsOrder { get; set; } = new List<string>();
        public List<string> RelatedMoviesRelationsOrder { get; set; } = new List<string>();
        public int FriendsVotesLimit { get; set; }
        public bool IsTariffSubscriptionActive { get; set; }
        public string MediaBillingTarget { get; set; } = string.Empty;
        public bool CheckSilentInvoiceAvailability { get; set; }
        public bool IncludeMovieRating { get; set; }
        public bool IsInternational { get; set; }
        public bool SkipTrailers { get; set; }
        public bool IsOnlyOnlineSeriesInfo { get; set; }
        public bool IncludeMovieDirectors { get; set; }
        public bool IncludePlannedToWatchRating { get; set; }

        public GetDetailsQuery(int kpId) {
            MovieId = kpId;
        }

        public GraphqlQuery<GetDetailsQuery> GetQuery() {
            var queryString = string.Empty;
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().First(x => x.Contains(nameof(GetDetailsQuery)));
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                using (var reader = new StreamReader(stream)) {
                    queryString = reader.ReadToEnd()
                        .Replace("\r\n", "");
                }
            }

            var query = new GraphqlQuery<GetDetailsQuery>();
            query.OperationName = OPERATION_NAME;
            query.Variables = this;
            query.Query = queryString;

            return query;
        }
    }
}