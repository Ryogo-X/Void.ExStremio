using System.Text.Json;
using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Providers.Media.VideoCdn.Models {
    class VideoCdnSearchResponse {
        public bool Result { get; set; }
        public VideoCdnSearchItemResponse[] Data { get; set; }
    }

    class VideoCdnSearchItemResponse {
        public long Id { get; set; }
        [JsonPropertyName("kp_id")]
        public long KpId { get; set; }
        [JsonPropertyName("imdb_id")]
        public string ImdbId { get; set; }
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }
        public string Title { get; set; }
        [JsonPropertyName("orig_title")]
        public string TitleOriginal { get; set; }

        [JsonPropertyName("iframe_src")]
        public string Link { get; set; }

        [JsonInclude]
        [JsonPropertyName("translations")]
        JsonElement TranslationsElelement { get; set; }
        [JsonIgnore]
        public string[] Translations {
            get {
                if (TranslationsElelement.ValueKind == JsonValueKind.Array) {
                    return TranslationsElelement.EnumerateArray().Select(x => x.GetString()).ToArray();
                } else if (TranslationsElelement.ValueKind == JsonValueKind.Object) {
                    return TranslationsElelement.EnumerateObject().Select(x => x.Value.GetString()).ToArray();
                }

                return [];
            }
        }
    }
}
