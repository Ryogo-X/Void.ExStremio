using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Providers.Media.Collaps.Models {
    class CollapseDetailsResponse {
        public long Id { get; set; }
        public string Type { get; set; }
        [JsonPropertyName("name")]
        public string Title { get; set; }
        [JsonPropertyName("name_eng")]
        public string TitleOriginal { get; set; }
        public int Year { get; set; }
        public string Quality { get; set; }
        [JsonPropertyName("imdb_id")]
        public string ImdbId { get; set; }
        [JsonPropertyName("kinopoisk_id")]
        public string KpId { get; set; }
        [JsonPropertyName("iframe_url")]
        public string Link { get; set; }
        [JsonPropertyName("voiceActing")]
        public List<string> Audio { get; set; }
        [JsonPropertyName("subtitle")]
        public List<string> Subtitle { get; set; }
        public CollapseDetailsSeasonResponse[] Seasons { get; set; }
    }

    class CollapseDetailsSeasonResponse {
        public int Season { get; set; }
        public CollapseDetailsEpisode[] Episodes { get; set; }
        [JsonPropertyName("iframe_url")]
        public string Link { get; set; }
    }

    class CollapseDetailsEpisode {
        public int Episode { get; set; }
        public string Name { get; set; }
        [JsonPropertyName("voiceActing")]
        public List<string> Audio { get; set; }
        [JsonPropertyName("subtitle")]
        public List<string> Subtitle { get; set; }
        [JsonPropertyName("iframe_url")]
        public string Link { get; set; }
    }
}
