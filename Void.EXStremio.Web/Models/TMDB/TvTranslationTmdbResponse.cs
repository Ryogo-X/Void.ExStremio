using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Models.TMDB {
    public class TvTranslationsTmdbResponse {
        public TvTranslationTmdbResponse[] Translations { get; set; }
    }

    public class TvTranslationTmdbResponse {
        [JsonPropertyName("iso_3166_1")]
        public string CountryCode { get; set; }
        public TvTranslationDataTmdbResponse Data { get; set; }
    }

    public class TvTranslationDataTmdbResponse {
        public string Name { get; set; }
    }
}
