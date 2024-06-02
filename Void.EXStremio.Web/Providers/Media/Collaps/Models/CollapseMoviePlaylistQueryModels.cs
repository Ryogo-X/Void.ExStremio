using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Providers.Media.Collaps.Models {
    class CollapseMovieResponse {
        public long Id { get; set; }
        public string Title { get; set; }
        [JsonPropertyName("poster")]
        public string PosterUrl { get; set; }
        public CollapseSourceResponse Source { get; set; }
    }

    class CollapseSourceResponse {
        [JsonPropertyName("dash")]
        public Uri DashUri { get; set; }
        [JsonPropertyName("hls")]
        public Uri HlsUri { get; set; }
        public CollapseAudioResponse Audio { get; set; }
        [JsonPropertyName("cc")]
        public CollapseSubtitleResponse[] Subtitles { get; set; }
    }
}
