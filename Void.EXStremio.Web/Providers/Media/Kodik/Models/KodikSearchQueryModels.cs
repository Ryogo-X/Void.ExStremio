using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Providers.Media.Kodik.Models {
    class KodikSearchResponse {
        public string Time { get; set; }
        public int Total { get; set; }
        public KodikSearchItemResponse[] Results { get; set; }
    }

    class KodikSearchItemResponse {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Link { get; set; }
        public string Title { get; set; }
        [JsonPropertyName("title_orig")]
        public string TitleOriginal { get; set; }
        [JsonPropertyName("other_title")]
        public string TitleOther { get; set; }
        public KodikTranslationResponse Translation { get; set; }
        public int Year { get; set; }
        [JsonPropertyName("kinopoisk_id")]
        public string KpId { get; set; }
        [JsonPropertyName("imdb_id")]
        public string ImdbId { get; set; }
        public string Quality { get; set; }
        [JsonPropertyName("camrip")]
        public bool IsCamrip { get; set; }
        public Dictionary<string, KodikSeasonResponse> Seasons { get; set; }
    }

    class KodikTranslationResponse {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
    }

    class KodikSeasonResponse {
        public string Link { get; set; }
        public Dictionary<string, string> Episodes { get; set; }
    }
}
