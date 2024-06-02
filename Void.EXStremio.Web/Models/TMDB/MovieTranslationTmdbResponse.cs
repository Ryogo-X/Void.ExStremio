using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Models.TMDB {
    public class MovieTranslationsTmdbResponse {
        public MovieTranslationTmdbResponse[] Translations { get; set; }
    }

    public class MovieTranslationTmdbResponse {
        [JsonPropertyName("iso_3166_1")]
        public string CountryCode { get; set; }
        public MovieTranslationDataTmdbResponse Data { get; set; }
    }

    public class MovieTranslationDataTmdbResponse {
        public string Title { get; set; }
    }
}
