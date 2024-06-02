using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Providers.Media.Hdvb.Models {
    class HdvbSearchItemResponse {
        [JsonPropertyName("title_ru")]
        public string Title { get; set; }
        [JsonPropertyName("title_en")]
        public string TitleOriginal { get; set; }
        public int Year { get; set; }
        public int KpId { get; set; }
        public string Translator { get; set; }
        public string Token { get; set; }
        public string Type { get; set; }
        [JsonPropertyName("iframe_url")]
        public Uri Link { get; set; }
        public Uri Poster { get; set; }
        [JsonPropertyName("serial_episodes")]
        public HdvbSeasonResponse[] Seasons { get; set; }
    }

    class HdvbSeasonResponse {
        [JsonPropertyName("season_number")]
        public int Season { get; set; }
        public int[] Episodes { get; set; }
    }
}
