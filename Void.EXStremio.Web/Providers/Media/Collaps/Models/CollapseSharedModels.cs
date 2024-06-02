namespace Void.EXStremio.Web.Providers.Media.Collaps.Models {
    class CollapseAudioResponse {
        public int[] Order { get; set; }
        public string[] Names { get; set; }
    }

    class CollapseSubtitleResponse {
        public Uri Url { get; set; }
        public string Name { get; set; }
    }
}
