using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Models.Kinopoisk.Response {
    public class MovieKpResponse {
        [JsonPropertyName("__typename")]
        public string Type { get; set; }
        public long Id { get; set; }
        public TitleKpResponse Title { get; set; }
        [JsonPropertyName("url")]
        public Uri Uri { get; set; }
        [JsonPropertyName("kpSynopsis")]
        public string Description { get; set; }
        [JsonInclude]
        [JsonPropertyName("kpYear")]
        int? MovieYear { get; set; }
        [JsonInclude]
        [JsonPropertyName("fallbackYear")]
        int? TvYear { get; set; }
        public int Year {
            get { return MovieYear ?? TvYear ?? 0; }
        }  
    }
}
