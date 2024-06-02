using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Providers.Media.Hdvb.Models {
    class HdvbPlaylistSeason {
        public string Id { get; set; }
        public string Title { get; set; }
        [JsonPropertyName("folder")]
        public HdvbPlaylistEpisode[] Episodes { get; set; }
    }

    class HdvbPlaylistEpisode {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Episode { get; set; }
        [JsonPropertyName("folder")]
        public HdvbPlaylistFile[] Files { get; set; }
    }

    class HdvbPlaylistFile {
        public string Id { get; set; }
        public string File { get; set; }
        public string Title { get; set; }
        public string Translator { get; set; }
    }
}
