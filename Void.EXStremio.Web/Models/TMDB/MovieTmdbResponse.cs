using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Models.TMDB {
    public class MovieTmdbResponse {
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("original_title")]
        public string OriginalTitle { get; set; }

        [JsonPropertyName("release_date")]
        public DateTime ReleaseDate { get; set; }

        [JsonPropertyName("external_ids")]
        public ExternalIdsTmdbResponse ExternalIds { get; set; }
        [JsonPropertyName("alternative_titles")]
        public MovieAlternativeTitlesTmdbResponse AlternativeTitles { get; set; }
        public MovieTranslationsTmdbResponse Translations { get; set; }
    }
}
