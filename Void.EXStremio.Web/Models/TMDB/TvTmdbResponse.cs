using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Models.TMDB {
    public class TvTmdbResponse {
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Title { get; set; }

        [JsonPropertyName("original_name")]
        public string OriginalTitle { get; set; }


        [JsonPropertyName("first_air_date")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("last_air_date")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("number_of_seasons")]
        public int? Seasons { get; set; }

        [JsonPropertyName("external_ids")]
        public ExternalIdsTmdbResponse ExternalIds { get; set; }
        [JsonPropertyName("alternative_titles")]
        public TvAlternativeTitlesTmdbResponse AlternativeTitles { get; set; }
        public TvTranslationsTmdbResponse Translations { get; set; }
    }
}
