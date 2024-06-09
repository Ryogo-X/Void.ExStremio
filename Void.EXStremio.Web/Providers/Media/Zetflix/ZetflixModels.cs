using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Providers.Media.Zetflix {
    class ZetflixResponse {
        public string Method { get; set; }
        public Uri Url { get; set; }
        public string Title { get; set; }
        [JsonPropertyName("quality")]
        public Dictionary<string, string> Links { get; set; }
    }
}
