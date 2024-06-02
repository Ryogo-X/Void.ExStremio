using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Models.TMDB {
    public class AlternativeTitleTmdbResponse {
        [JsonPropertyName("iso_3166_1")]
        public string CountryCode { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
    }
}
