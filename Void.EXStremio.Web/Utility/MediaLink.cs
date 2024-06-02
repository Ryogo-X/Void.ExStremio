using System.Text;
using Void.EXStremio.Web.Models;

namespace Void.EXStremio.Web.Utility {
    public class MediaLink {
        public Uri SourceUri { get; }
        public string SourceType { get; }
        public MediaFormatType FormatType { get; }
        public string Quality { get; }
        public MediaProxyType ProxyType { get; }

        public MediaLink(Uri sourceUri, string sourceType, MediaFormatType formatType, string quality, MediaProxyType proxyType = MediaProxyType.Direct) {
            SourceUri = sourceUri;
            SourceType = sourceType.ToLowerInvariant();
            FormatType = formatType;
            Quality = quality;
            ProxyType = proxyType;
        }

        public Uri GetUri() {
            if (ProxyType == MediaProxyType.Direct) {
                return SourceUri;
            } else {
                var uriBytes = Encoding.UTF8.GetBytes(SourceUri.ToString());
                var encodedUri = Convert.ToBase64String(uriBytes);
                return new Uri($"/stream/play/{encodedUri}?source={SourceType}&format={FormatType}&quality={Quality}", UriKind.Relative);
            }
        }

        public bool IsPlaylist() {
            return FormatType == MediaFormatType.DASH || FormatType == MediaFormatType.HLS;
        }

        public override string ToString() {
            return GetUri().ToString();
        }
    }
}
