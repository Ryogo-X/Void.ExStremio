using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Providers.Media.Ashdi {
    class AshdiSearchResponse { 
        public string Id { get; set; }
        public string Title { get; set; }
        [JsonPropertyName("title_en")]
        public string TitleOriginal {  get; set; }
        [JsonPropertyName("kp_id")]
        public string KpId { get; set; }
        [JsonPropertyName("imdb_id")]
        public string ImdbId { get; set; }
        public Uri Url { get; set; }
        public string Year { get; set; }
    }
}
