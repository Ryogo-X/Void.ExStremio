using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Models.Kinopoisk.Response {
    public class PosterKpResponse {
        [JsonPropertyName("avatarsUrl")]
        public string Url { get; set; }
        public string FallbackUrl { get; set; }
    }
}
