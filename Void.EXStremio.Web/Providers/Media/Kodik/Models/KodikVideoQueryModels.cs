using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Void.EXStremio.Web.Providers.Media.Kodik.Models {
    class KodikVideoSourceResponse {
        public string Domain { get; set; }
        public Dictionary<string, KodikVideoLinkResponse[]> Links { get; set; }
    }

    class KodikVideoLinkResponse {
        [JsonInclude]
        [JsonPropertyName("src")]
        string EncodedLink { get; set; }
        public string Type { get; set; }
        public Uri Link {
            get { 
                var link = DecodeLink(EncodedLink);
                return new Uri(link);
            }
        }

        string DecodeLink(string encodedLink) {
            var decodeChar = (char e) => {
                return (char)((e <= 'Z' ? 90 : 122) >= (e = (char)((int)e + 13)) ? e : e - 26);
            };

            encodedLink = Regex.Replace(encodedLink, "[a-zA-Z]", x => decodeChar(x.Value[0]).ToString());
            var missingPadding = 4 - encodedLink.Length % 4;
            if (missingPadding > 0 && missingPadding <= 2) {
                encodedLink += string.Join("", Enumerable.Range(0, missingPadding).Select(x => '='));
            }
            var bytes = Convert.FromBase64String(encodedLink);

            var link = Encoding.UTF8.GetString(bytes);
            if (!link.StartsWith("http")) {
                link = "https:" + link;
            }

            return link;
        }
    }
}