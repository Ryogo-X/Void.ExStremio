using System.Net;

namespace Void.EXStremio.Web.Providers.Media {
    public class FlicksbarMediaProvider {
        const string baseUri = "https://flicksbar.mom/kinobox/index.php?kinopoisk="; //"https://flicksbar.mom/kinobox/index.php?imdb=";
        static readonly CookieContainer cookies = new CookieContainer();

        public async Task<Models.MediaStream[]> GetStreams(string kpId, int? season = null, int? episode = null) {
            return null;

            //using (var client = GetClient()) {
            //    client.DefaultRequestHeaders.Add("Referer", "http://flicksbar.mom/");

            //    var apiResponse = await client.GetFromJsonAsync<FlicksbarApiResponse>(baseUri + kpId);
            //    if (!apiResponse.Success) {
            //        throw new InvalidOperationException(apiResponse.Error?.Message ?? "Unknown error.");
            //    }

            //    var streams = new List<Models.Stream>();
            //    foreach (var dataItem in apiResponse.Data) {
            //        if (string.IsNullOrWhiteSpace(dataItem.IframeUrl)) { continue; }


            //        var uri = new Uri(dataItem.IframeUrl);
            //        if (dataItem.Type == "HDVB") {
            //            var provider = new HdvbCdnProvider();

            //            var newStreams = await provider.GetStreams(uri, season, episode);
            //            streams.AddRange(newStreams);
            //        } else if (dataItem.Type == "COLLAPS") {
            //            var provider = new CollapsCdnProvider();

            //            var newStreams = await provider.GetStreams(uri, season, episode);
            //            streams.AddRange(newStreams);
            //        }
            //    }

            //    return streams.ToArray();
            //}
        }

        HttpClient GetClient() {
            var handler = new HttpClientHandler();
            handler.CookieContainer = cookies;
            handler.AutomaticDecompression = DecompressionMethods.All;

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/113.0");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.5");

            return client;
        }

        class FlicksbarApiResponse {
            public bool Success { get; set; }
            public FlicksbarDataItemApiResponse[] Data { get; set; }
            public FlicksbarErrorApiResponse Error { get; set; }
        }

        class FlicksbarErrorApiResponse {
            public int Code { get; set; }
            public string Message { get; set; }
        }

        class FlicksbarDataItemApiResponse {
            public string Type { get; set; }
            public string Translation { get; set; }
            public string Quality { get; set; }
            public string IframeUrl { get; set; }
        }
    }
}
