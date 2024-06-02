using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Providers.Media.Collaps.Models {
    class CollapseTvConfigResponse {
        public CollapseTvPlaylistResponse Playlist { get; set; }
    }

    class CollapseTvPlaylistResponse {
        public long Id { get; set; }
        public CollapseTvSeasonReponse[] Seasons { get; set; }
    }

    class CollapseTvSeasonReponse {
        public long Season { get; set; }
        public CollapseTvEpisodeReponse[] Episodes { get; set; }

    }

    class CollapseTvEpisodeReponse {
        public long Id { get; set; }
        public long VideoKey { get; set; }
        public string Episode { get; set; }
        public Uri Dash { get; set; }
        public Uri Hls { get; set; }
        public CollapseAudioResponse Audio { get; set; }
        [JsonPropertyName("cc")]
        public CollapseSubtitleResponse[] Subtitles { get; set; }
        public int Duration { get; set; }
        public string Title { get; set; }
        public Uri Poster { get; set; }
    }
}
