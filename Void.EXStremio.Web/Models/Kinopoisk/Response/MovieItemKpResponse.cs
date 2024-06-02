using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Models.Kinopoisk.Response {
    public class MovieItemKpResponse {
        [JsonPropertyName("__typename")]
        public string Type { get; set; }
        public long Id { get; set; }
        public string Url { get; set; }
        [JsonPropertyName("productionYear")]
        public int? Year { get; set; }
        public int? FallbackYear { get; set; }
        [JsonPropertyName("releaseYears")]
        public YearRangeKpResponse[] Years { get; set; }
        public GalleryKpResponse Gallery { get; set; }
        public TitleKpResponse Title { get; set; }
        public GenreKpResponse[] Genres { get; set; }
        public CountryKpResponse[] Countries { get; set; }
        public RatingKpResponse Rating { get; set; }
        public SeasonsCountKpResponse SeasonsCount { get; set; }
    }
}
