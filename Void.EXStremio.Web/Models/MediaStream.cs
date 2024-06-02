using static Void.EXStremio.Web.Providers.Stream.HdRezkaApi;
using System.Text;
using System.Text.RegularExpressions;

namespace Void.EXStremio.Web.Models {
    public enum Platform {
        Android,
        iOS
    }

    public enum Availability {
        NotAvailable = 0,
        BearlyAvailable = 1,
        Ok = 2,
        HighlyAvailable = 3
    }

    public class Subtitles {
        public string Url { get; set; }
        public string Lang { get; set; }
    }

    public class ExternalUri {
        public string Platform { get; set; }
        public string Uri { get; set; }
        public string AppUri { get; set; }

    }

    public class MediaStream {
        public string Url { get; set; }
        public string YtId { get; set; }
        public string InfoHash { get; set; }
        public uint? FileIdx { get; set; }
        public uint? MapIdx { get; set; }
        public string ExternalUrl { get; set; }
        public ExternalUri[] ExternalUris { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public Availability? Availability { get; set; }
        public string Tag { get; set; }
        public bool? IsFree { get; set; }
        public bool? IsSubscription { get; set; }
        public bool? IsPeered { get; set; }
        public Subtitles[] Subtitles { get; set; }
        public bool? SubtitlesExclusive { get; set; }
        public bool? Live { get; set; }
        public bool? Repeat { get; set; }
        public string[] Geos { get; set; }
        public object Meta { get; set; }

        public string GetOriginalUrl() {
            var uri = Regex.Match(Url ?? "", "/stream/play/(?<uri>[^?]+)\\?").Groups["uri"].Value;
            if (!string.IsNullOrEmpty(uri)) {
                return Encoding.UTF8.GetString(Convert.FromBase64String(uri));
            }

            return Url;
        }
    }
}
