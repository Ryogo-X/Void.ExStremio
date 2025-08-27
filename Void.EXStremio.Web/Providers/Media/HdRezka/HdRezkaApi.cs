using AngleSharp.Dom;
using AngleSharp;
using System.Text.RegularExpressions;
using System.Text;
using Void.EXStremio.Web.Utility;
using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Models;

namespace Void.EXStremio.Web.Providers.Media.HdRezka {
    class HdRezkaApi {
        const string EPISODE_NOT_FOUND_ERROR = "Время сессии истекло. Пожалуйста, обновите страницу и повторите попытку";

        readonly Uri host;
        readonly Func<HttpClient> getHttpClient;
        readonly IMemoryCache cache;
        readonly string user;
        readonly string password;
        readonly string cacheAuthKey = $"{nameof(HdRezkaApi):AUTH:COOKIES}";

        public HdRezkaApi(Uri host, string user, string password, Func<HttpClient> getHttpClient, IMemoryCache cache) {
            this.host = host;
            this.user = user;
            this.password = password;
            this.getHttpClient = getHttpClient;
            this.cache = cache;
        }

        public async Task<HdRezkaSearchResult[]> Search(string query) {
            using (var client = getHttpClient()) {
                await HandleAuth(client);

                var vars = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("q", query)
                };
                var formContent = new FormUrlEncodedContent(vars);
                var url = new Uri(host, "/engine/ajax/search.php");
                var response = await SimpleRetry.Retry(async () => await client.PostAsync(url, formContent), r => r.IsSuccessStatusCode, 3, 5, false);
                var html = await response.Content.ReadAsStringAsync();
                var document = await GetHtmlDocument(html);

                var items = document.QuerySelectorAll(".b-search__section_list > li");
                return items.Select(item => {
                    var url = new Uri(item.QuerySelector("a").GetAttribute("href"));
                    var title = item.QuerySelector(".enty").Text().Trim();
                    var description = item.QuerySelector(".enty").NextSibling.Text().Trim(new[] { '(', ' ', ')' });

                    var type = HdRezkaMediaType.Movie;
                    var yearString = "";
                    var splitIndex = description.LastIndexOf(',');
                    if (splitIndex == -1) {
                        yearString = description.Trim(new[] { '(', ' ', ')' });
                        description = "";
                    } else {
                        yearString = description.Substring(splitIndex + 1).Trim(new[] { '(', ' ', ')' });
                        description = description.Substring(0, description.Length - yearString.Length).Trim(new[] { ' ', ',' });
                    }
                    if (description.EndsWith("сериал")) {
                        type = HdRezkaMediaType.Series;
                        description = description.Substring(0, description.Length - "сериал".Length).Trim(new[] { ' ', ',' });
                    } else if (description.EndsWith("мультфильм")) {
                        if (yearString.Contains("-")) { type = HdRezkaMediaType.Series; }
                        description = description.Substring(0, description.Length - "мультфильм".Length).Trim(new[] { ' ', ',' });
                    } else if (description.EndsWith("аниме")) {
                        if (yearString.Contains("-")) { type = HdRezkaMediaType.Series; }
                        description = description.Substring(0, description.Length - "аниме".Length).Trim(new[] { ' ', ',' });
                    }
                    if (item.QuerySelector("#simple-episodes-tabs") != null) {
                        type = HdRezkaMediaType.Series;
                    }
                    var originalTitles = !string.IsNullOrWhiteSpace(description) ? description.Split(" / ", StringSplitOptions.TrimEntries) : [];

                    var yearStrings = yearString.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    int.TryParse(yearStrings[0], out var startYear);
                    int.TryParse(yearStrings.Length > 1 ? yearStrings[1] : "", out var endYear);

                    var titles = title.Split('/').Select(x => x.Trim()).ToArray();

                    return new HdRezkaSearchResult(url, titles, originalTitles, startYear, endYear, type);
                }).ToArray();
            }
        }

        public async Task<HdRezkaMetadata> GetMetadata(Uri url) {
            using (var client = getHttpClient()) {
                await HandleAuth(client);

                var response = await SimpleRetry.Retry(async () => await client.GetAsync(url), r => r.IsSuccessStatusCode, 3, 5, false);
                var html = await response.Content.ReadAsStringAsync();
                if (html.Contains("Ожидаем фильм в хорошем качестве...")) {
                    throw new InvalidOperationException($"[HdRezka] item is not released yet...");
                }

                var document = await GetHtmlDocument(html, url);

                string imdbId = null;
                string kpId = null;
                // get external ids
                {
                    var encodedImdbLink = document.QuerySelector(".imdb > a")
                        ?.GetAttribute("href")
                        ?.Split('/', StringSplitOptions.RemoveEmptyEntries)
                        ?.LastOrDefault();
                    var imdbLink = Uri.UnescapeDataString(Encoding.UTF8.GetString(Convert.FromBase64String(encodedImdbLink)));
                    imdbId = imdbLink?.Split('/', StringSplitOptions.RemoveEmptyEntries)?.LastOrDefault();

                    var encodedKpLink = document.QuerySelector(".kp > a")
                        ?.GetAttribute("href")
                        ?.Split('/', StringSplitOptions.RemoveEmptyEntries)
                        ?.LastOrDefault();
                    var kpLink = Uri.UnescapeDataString(Encoding.UTF8.GetString(Convert.FromBase64String(encodedKpLink)));
                    kpId = kpLink?.Split('/', StringSplitOptions.RemoveEmptyEntries)?.LastOrDefault();

                    if (string.IsNullOrWhiteSpace(imdbId) || string.IsNullOrWhiteSpace(kpId)) {
                        throw new InvalidOperationException($"Not able to parse metadata for {url}");
                    }
                }

                string title = null;
                string originalTitle = null;
                {
                    title = document.QuerySelector(".b-post__title")?.TextContent?.Trim();
                    originalTitle = document.QuerySelector(".b-post__origtitle")?.TextContent?.Trim();

                    if (string.IsNullOrWhiteSpace(title)) {
                        throw new InvalidOperationException($"Not able to parse metadata for {url}");
                    }
                }

                var typeString = document.QuerySelector("meta[property='og:type']")?.GetAttribute("content");
                HdRezkaMediaType type = HdRezkaMediaType.Undefined;
                if (typeString == "video.movie") {
                    type = HdRezkaMediaType.Movie;
                } else if (typeString == "video.tv_series") {
                    type = HdRezkaMediaType.Series;
                } else {
                    throw new InvalidOperationException($"Not able to parse metadata for {url}");
                }

                HdRezkaDetails[] details = [];
                var translatorItems = document.QuerySelectorAll(".b-translator__item:not(.b-prem_translator)");
                if (translatorItems.Any()) {
                    details = translatorItems.Select(item => {
                        var title = item.ChildNodes.First(x => x.NodeType == NodeType.Text).Text();
                        var translatorId = int.Parse(item.GetAttribute("data-translator_id").Trim());

                        var idString = item.GetAttribute("data-id")?.Trim();
                        if (idString == null) {
                            var episodeItem = document.QuerySelector("#simple-episodes-tabs .b-simple_episode__item");
                            idString = episodeItem.GetAttribute("data-id").Trim();
                        }
                        var id = int.Parse(idString);

                        bool? isCamrip = null;
                        var isCamripString = item.GetAttribute("data-camrip")?.Trim();
                        if (!string.IsNullOrWhiteSpace(isCamripString)) {
                            isCamrip = Convert.ToBoolean(int.Parse(isCamripString));
                        }

                        bool? hasAds = null;
                        var hasAdsString = item.GetAttribute("data-ads")?.Trim();
                        if (!string.IsNullOrWhiteSpace(hasAdsString)) {
                            hasAds = Convert.ToBoolean(int.Parse(hasAdsString));
                        }

                        bool? isDirectorCut = null;
                        var isDirectorCutString = item.GetAttribute("data-director")?.Trim();
                        if (!string.IsNullOrWhiteSpace(isDirectorCutString)) {
                            isDirectorCut = Convert.ToBoolean(int.Parse(isDirectorCutString));
                        }

                        return new HdRezkaDetails(title, id, translatorId, isCamrip, hasAds, isDirectorCut);
                    }).ToArray();
                } else if (type == HdRezkaMediaType.Movie) {
                    var scripts = document.QuerySelectorAll("script");
                    var movieScript = scripts.FirstOrDefault(x => x.InnerHtml.Contains("sof.tv.initCDNMoviesEvents"));
                    if (movieScript != null) {
                        var match = Regex.Match(movieScript.InnerHtml, @"sof.tv.initCDNMoviesEvents\((?<id>[0-9]*)[ ,]*(?<translatorId>[0-9]*)");

                        //var title = "";
                        var idString = match.Groups["id"].Value.Trim();
                        var id = int.Parse(idString);
                        var translatorIdString = match.Groups["translatorId"].Value.Trim();
                        var translatorId = int.Parse(translatorIdString);

                        details = [new HdRezkaDetails(title, id, translatorId)];
                    }
                } else if (type == HdRezkaMediaType.Series) {
                    var scripts = document.QuerySelectorAll("script");
                    var seriesScript = scripts.FirstOrDefault(x => x.InnerHtml.Contains("sof.tv.initCDNSeriesEvents"));
                    if (seriesScript != null) {
                        var match = Regex.Match(seriesScript.InnerHtml, @"sof.tv.initCDNSeriesEvents\((?<id>[0-9]*)[ ,]*(?<translatorId>[0-9]*)");

                        //var title = "";
                        var idString = match.Groups["id"].Value.Trim();
                        var id = int.Parse(idString);
                        var translatorIdString = match.Groups["translatorId"].Value.Trim();
                        var translatorId = int.Parse(translatorIdString);

                        details = [new HdRezkaDetails(title, id, translatorId)];
                    }
                }

                if (!details.Any()) {
                    throw new InvalidOperationException($"Not able to parse metadata for {url}");
                }

                return new HdRezkaMetadata(imdbId, kpId, title, originalTitle, type, details);
            }
        }

        public async Task<IReadOnlyDictionary<int, int[]>> GetEpisodes(Uri refererUrl, int id, int translatorId) {
            using (var client = getHttpClient()) {
                await HandleAuth(client);

                var vars = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("id", id.ToString()),
                        new KeyValuePair<string, string>("translator_id", translatorId.ToString()),
                        new KeyValuePair<string, string>("action", "get_episodes")
                    };
                var formContent = new FormUrlEncodedContent(vars);

                client.DefaultRequestHeaders.Add("Referer", refererUrl.ToString());
                var url = new Uri(host, $"/ajax/get_cdn_series/?t={DateTimeOffset.Now.ToUnixTimeSeconds()}");
                var response = await client.PostAsync(url, formContent);
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = System.Text.Json.JsonSerializer.Deserialize<HdRezkaApiResponse>(json);
                var document = await GetHtmlDocument(apiResponse.EpisodesHtml, refererUrl);
                var items = document.QuerySelectorAll(".b-simple_episode__item");

                return items
                    .Select(item => {
                        var season = int.Parse(item.GetAttribute("data-season_id").Trim());
                        var episode = int.Parse(item.GetAttribute("data-episode_id").Trim());

                        return (season, episode);
                    })
                    .GroupBy(item => item.season)
                    .ToDictionary(x => x.Key, x => x.Select(y => y.episode).ToArray());
            }
        }

        public async Task<IMediaStream[]> GetMovieStreams(Uri refererUrl, int id, int translatorId, bool? isCamrip, bool? hasAds, bool? isDirectorsCut) {
            var vars = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("id", id.ToString()),
                    new KeyValuePair<string, string>("translator_id", translatorId.ToString()),
                    new KeyValuePair<string, string>("action", "get_movie")
                };
            if (isCamrip.HasValue) {
                vars.Add(new KeyValuePair<string, string>("is_camrip", isCamrip.Value ? "1" : "0"));
            }
            if (hasAds.HasValue) {
                vars.Add(new KeyValuePair<string, string>("is_ads", hasAds.Value ? "1" : "0"));
            }
            if (isDirectorsCut.HasValue) {
                vars.Add(new KeyValuePair<string, string>("is_director", isDirectorsCut.Value ? "1" : "0"));
            }
            var formBody = new FormUrlEncodedContent(vars);

            return await GetStreams(refererUrl, formBody);
        }

        public async Task<IMediaStream[]> GetSeriesEpisodeStreams(Uri refererUrl, int id, int translatorId, int season, int episode) {
            var vars = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("id", id.ToString()),
                    new KeyValuePair<string, string>("translator_id", translatorId.ToString()),
                    new KeyValuePair<string, string>("season", season.ToString()),
                    new KeyValuePair<string, string>("episode", episode.ToString()),
                    new KeyValuePair<string, string>("action", "get_stream")
                };
            var formBody = new FormUrlEncodedContent(vars);

            return await GetStreams(refererUrl, formBody);
        }

        async Task<IMediaStream[]> GetStreams(Uri refererUrl, FormUrlEncodedContent formBody) {
            using (var client = getHttpClient()) {
                await HandleAuth(client);

                client.DefaultRequestHeaders.Add("Accept", "*/*");
                client.DefaultRequestHeaders.Add("Referer", refererUrl.ToString());
                var data = await SimpleRetry.Retry(
                    async () => {
                        var url = new Uri(host, $"/ajax/get_cdn_series/?t={DateTimeOffset.Now.ToUnixTimeSeconds()}");
                        var response = await client.PostAsync(url, formBody);
                        if (!response.IsSuccessStatusCode) { return new HdRezkaApiResponse(); }

                        var json = await response.Content.ReadAsStringAsync();
                        var data = System.Text.Json.JsonSerializer.Deserialize<HdRezkaApiResponse>(json);
                        if (!data.Success && data.Message == EPISODE_NOT_FOUND_ERROR) {
                            data.Success = true;
                        }

                        return data;
                    }, r => r.Success, 3, 5, false);
                if (data.Success && data.Message == EPISODE_NOT_FOUND_ERROR) { return []; }

                var decoded = HdRezkaPayloadDecoder.Decode(data.Payload);
                var parts = decoded.Split(",");
                return parts.Select(part => {
                    var qualityString = Regex.Match(part, @"\[.+\]").Value;
                    var quality = Regex.Match(qualityString, @"[0-9]+").Value;
                    if (qualityString.Contains("Ultra")) {
                        quality += "h";
                    }

                    var urls = Regex.Matches(part, @"(http|https)://[^ ,]+").Select(x => x.Value);

                    return (quality, urls);
                }).Select(item => {
                    var streamUrl = item.urls.FirstOrDefault(x => x.EndsWith(".mp4")) ?? item.urls.First().Replace(":hls:manifest.m3u8", "");
                    return new HdRezkaStream(streamUrl, item.quality);
                }).ToArray();
            }
        }

        async Task<IDocument> GetHtmlDocument(string html, Uri url = null) {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(response => {
                if (url != null) {
                    response.Address(url);
                }
                response.Content(html);
            });

            return document;
        }

        public async Task HandleAuth(HttpClient client) {
            if (!cache.TryGetValue(cacheAuthKey, out string[] cookies)) {
                var vars = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("login_name", user),
                    new KeyValuePair<string, string>("login_password", password),
                    new KeyValuePair<string, string>("login_not_save", "0")
                };
                var formContent = new FormUrlEncodedContent(vars);
                var url = new Uri(host, "/ajax/login/");
                var response = await SimpleRetry.Retry(async () => await client.PostAsync(url, formContent), r => r.IsSuccessStatusCode, 3, 5, false);
                var json = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonSerializer.Deserialize<HdRezkaApiResponseBase>(json);


                if (!data.Success || !response.Headers.TryGetValues("Set-Cookie", out var cookieStrings)) {
                    throw new InvalidOperationException("[HdRezka] Authorization failed. Please check login/password");
                }

                cookies = cookieStrings
                    .Select(x => x.Split(';').First())
                    .Where(x => x != "dle_user_id=deleted" && x != "dle_password=deleted")
                    .ToArray();
                cache.Set(cacheAuthKey, cookies, DateTime.Now.AddHours(8));
            }

            client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies));
        }

        public record HdRezkaSearchResult(Uri Url, string[] Titles, string[] AdditionalTitles, int StartYear, int? EndYear, HdRezkaMediaType Type) {
            public IEnumerable<string> GetSanitizedTitles() {
                foreach(var title in Titles) {
                    yield return title;

                    if (!IsStandaloneTitle(title)) {
                        yield return Regex.Replace(title, @"\[ТВ-[0-9]+\]", "");
                    }

                    if (title.Contains(':')) {
                        yield return title.Split(':').First();
                    }
                }
            }

            public bool IsStandaloneTitle(string title) {
                return !Regex.IsMatch(title, @"\[ТВ-[0-9]+\]");
            }
        }

        public record HdRezkaMetadata(string ImdbId, string KpId, string Title, string OriginalTitle, HdRezkaMediaType Type, HdRezkaDetails[] Details) {
            public bool IsStandaloneTitle() {
                return !Regex.IsMatch(Title, @"\[ТВ-[0-9]+\]");
            }

            public int GetTvSeason() {
                var valueString = Regex.Match(Title, @"\[ТВ-(?<value>[0-9]+)\]").Groups["value"].Value;

                return int.Parse(valueString);
            }
        }

        public record HdRezkaDetails(string Title, int Id, int TranslatorId, bool? IsCamrip = null, bool? HasAds = null, bool? IsDirectorCut = null);

        public interface IMediaStream {
            string Url { get; }
            string Quality { get; }
        }

        record HdRezkaStream(string Url, string Quality) : IMediaStream;

        public enum HdRezkaMediaType {
            Undefined = 0,
            Movie = 1,
            Series = 2
        }

        public class HdRezkaApiResponseBase {
            [System.Text.Json.Serialization.JsonPropertyName("success")]
            public bool Success { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("message")]
            public string Message { get; set; }
        }

        public partial class HdRezkaApiResponse : HdRezkaApiResponseBase {
            [System.Text.Json.Serialization.JsonPropertyName("quality")]
            public string Quality { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("seasons")]
            public string SeasonsHtml { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("episodes")]
            public string EpisodesHtml { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("url")]
            public string Payload { get; set; }

            //[System.Text.Json.Serialization.JsonPropertyName("subtitle")]
            //public bool Subtitle { get; set; }

            //[System.Text.Json.Serialization.JsonPropertyName("subtitle_lns")]
            //public bool SubtitleLns { get; set; }

            //[System.Text.Json.Serialization.JsonPropertyName("subtitle_def")]
            //public bool SubtitleDef { get; set; }

            //[System.Text.Json.Serialization.JsonPropertyName("thumbnails")]
            //public string Thumbnails { get; set; }
        }
    }

    public class HdRezkaPayloadDecoder {
        readonly static string regexExpression;

        static HdRezkaPayloadDecoder() {
            regexExpression = GenerateRegexExpression();
        }

        static string GenerateRegexExpression() {
            var expression = "[/_]{1,}(stems){1,}";
            var stremString = string.Empty;

            {
                var sequences = Enumerable.Range(0, 2).Select(_ => "!$@#^");
                var stems = CartesianProduct(sequences)
                    .Select(chars => string.Join("", chars))
                    .Select(stem => Convert.ToBase64String(Encoding.UTF8.GetBytes(stem)))
                    .ToArray();
                stremString = string.Join('|', stems);
            }

            {
                var sequences = Enumerable.Range(0, 3).Select(_ => "!$@#^");
                var stems = CartesianProduct(sequences)
                    .Select(chars => string.Join("", chars))
                    .Select(stem => Convert.ToBase64String(Encoding.UTF8.GetBytes(stem)))
                    .ToArray();
                stremString += "|" + string.Join('|', stems);
            }
            expression = expression.Replace("stems", stremString);

            return expression;
        }

        static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences) {
            // base case: 
            IEnumerable<IEnumerable<T>> result = new[] { Enumerable.Empty<T>() };
            foreach (var sequence in sequences) {
                var s = sequence; // don't close over the loop variable 
                                  // recursive case: use SelectMany to build the new product out of the old one 
                result =
                    from seq in result
                    from item in s
                    select seq.Concat(new[] { item });
            }
            return result;
        }

        public static string Decode(string payload) {
            var fixedPayload = payload;

            if (fixedPayload[0] == '#') {
                fixedPayload = fixedPayload.Substring(2);
            }

            while (true) {
                var match = Regex.Matches(fixedPayload, regexExpression).LastOrDefault();
                if (match == null) { break; }

                fixedPayload = fixedPayload.Replace(match.Value, string.Empty);
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(fixedPayload));
        }
    }
}
