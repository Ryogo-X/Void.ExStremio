namespace Void.EXStremio.Web.Models {
    abstract class MediaMimeType {
        public const string DASH = "application/dash+xml";
        public const string HLS = "application/vnd.apple.mpegurl";

        public static string GetMimeType(MediaFormatType type) {
            if (type == MediaFormatType.HLS) {
                return HLS;
            } else if (type == MediaFormatType.DASH) { 
                return DASH;
            } else {
                throw new NotSupportedException($"No MIME-type for '{type}' format.");
            }
        }
    }
}
