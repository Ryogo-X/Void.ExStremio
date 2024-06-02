using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Providers.Media.AllohaTv.Models {
    class AllohaTvSearchResponse {
        public string Status { get; set; }
        [JsonPropertyName("error_info")]
        public string Error { get; set; }
        public AllohaTvSearchItemResponse Data { get; set; }
    }

    class AllohaTvSearchItemResponse {
        [JsonPropertyName("id_kp")]
        public long KpId { get; set; }
        [JsonPropertyName("id_imdb")]
        public string ImdbId { get; set; }
        [JsonPropertyName("id_tmdb")]
        public long TmdbId { get; set; }
        [JsonPropertyName("name")]
        public string Title { get; set; }
        [JsonPropertyName("original_name")]
        public string TitleOriginal { get; set; }
        [JsonPropertyName("alternative_name")]
        public string TitleOther { get; set; }
        public int Year { get; set; }
        [JsonPropertyName("token_movie")]
        public string Token { get; set; }
        [JsonPropertyName("iframe")]
        public string Link { get; set; }
    }
}
