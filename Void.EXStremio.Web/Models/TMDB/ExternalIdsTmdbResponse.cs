using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Models.TMDB {
    public class ExternalIdsTmdbResponse {
        [JsonPropertyName("imdb_id")]
        public string ImdbId { get; set; }
    }
}
