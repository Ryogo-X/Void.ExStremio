using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Void.EXStremio.Web.Providers.Media.VideoCdn.Models {
    class VideoCdnPlaylistSeasonResponse {
        public int Id { get; set; }
        public string Comment { get; set; }
        [JsonPropertyName("folder")]
        public VideoCdnPlaylistEpisodeResponse[] Episodes { get; set; }
    }

    class VideoCdnPlaylistEpisodeResponse {
        public string Id { get; set; }
        public string Comment { get; set; }
        public string File { get; set; }
        public string Poster { get; set; }
        public Dictionary<string, string> Downloads { get; set; }
    }
}
