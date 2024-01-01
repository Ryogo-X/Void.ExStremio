using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using static Void.EXStremio.Web.Providers.Stream.HdRezkaApi;

namespace Void.EXStremio.Web.Providers.Stream {
    public class HdRezkaStreamProvider {
        static readonly CookieContainer cookies = new CookieContainer();

        string[] types = new[] { "сериал", "мультфильм", "аниме" };

        public async Task<Models.Stream[]> Get(Models.Meta meta, int? season = null, int? episode = null) {
            var apiClient = new HdRezkaApi(new Uri("https://hdrezka.me/"), cookies);
            var searchResults = await apiClient.Search(meta);
            if (!searchResults.Any()) { return null; }

            var searchResult = Match(searchResults, meta);
            var hdRezkaMetaItems = await apiClient.GetMetadata(searchResult.Url);

            return hdRezkaMetaItems.SelectMany(hdRezkaMetaItem => {
                var streams = Array.Empty<IMediaStream>();
                if (searchResult.Type == HdRezkaMediaType.Movie) {
                    streams = apiClient.GetMovieStreams(searchResult.Url, hdRezkaMetaItem.Id, hdRezkaMetaItem.TranslatorId, hdRezkaMetaItem.IsCamrip.Value, hdRezkaMetaItem.HasAds.Value, hdRezkaMetaItem.IsDirectorCut.Value).Result;
                } else if (searchResult.Type == HdRezkaMediaType.Series) {
                    streams = apiClient.GetSeriesEpisodeStreams(searchResult.Url, hdRezkaMetaItem.Id, hdRezkaMetaItem.TranslatorId, season.Value, episode.Value).Result;
                } else {
                    throw new NotImplementedException($"Unknown media type {searchResult.Type} for {searchResult.Url}");
                }

                return streams.Select(stream => {
                    return new Models.Stream() {
                        Name = $"{hdRezkaMetaItem.Title} {stream.Quality}",
                        Title = meta.Name,
                        Url = stream.Url,
                    };
                });
            }).ToArray();  
        }

        HdRezkaSearchResult Match(HdRezkaSearchResult[] searchResults, Models.Meta meta) {
            var rankedResults = searchResults.Select(searchResult => {
                var rank = searchResult.Titles.Select(title => {
                    return LevenshteinDistance(meta.Name + " " + meta.Year, title + " " + searchResult.StartYear);
                }).OrderBy(pts => pts).First();
                if (searchResult.Type.ToString().ToLowerInvariant() != meta.Type.ToLowerInvariant()) { rank = 10; }

                return new {
                    Rank = Math.Max(10 - rank, 0),
                    SearchResult = searchResult
                };
            }).OrderByDescending(rankedResult => rankedResult.Rank);
            var rankedResult = rankedResults.First();
            if (rankedResult.Rank < 7) {
                throw new ArgumentException($"Could not match any result for {meta.Name} [{meta.StartYear}]");
            }
            if (rankedResults.Count(r => r.Rank == rankedResult.Rank) > 1) {
                throw new ArgumentException($"Matched more than one result for {meta.Name} [{meta.StartYear}]");
            }

            return rankedResult.SearchResult;
        }

        async Task<Models.Stream[]> GetStreams(string title, string url, int? season = null, int? episode = null) {
            using(var client = GetClient()) {
                var html = await client.GetStringAsync(url);
                var document = await GetDocument(url, html);
                var translatorItems = document.QuerySelectorAll(".b-translator__item");
                var translatorParamsItems = translatorItems.Select(item => {
                    var title = item.ChildNodes.First(x => x.NodeType == NodeType.Text).Text();
                    var translatorId = int.Parse(item.GetAttribute("data-translator_id").Trim());

                    var idString = item.GetAttribute("data-id")?.Trim();
                    if (idString == null) { return null; }
                    var id = int.Parse(idString);

                    var isCamrip = Convert.ToBoolean(int.Parse(item.GetAttribute("data-camrip").Trim()));
                    var hasAds = Convert.ToBoolean(int.Parse(item.GetAttribute("data-ads").Trim()));
                    var isDirectorCut = Convert.ToBoolean(int.Parse(item.GetAttribute("data-director").Trim()));

                    return new TranslatorParams(title, id, translatorId, isCamrip, hasAds, isDirectorCut);
                }).Where(x => x != null);

                var apiClient = new HdRezkaApi(new Uri("https://hdrezka.me/"), cookies);
                return translatorParamsItems.SelectMany(translatorParamsItem => {
                    var streams = apiClient.GetMovieStreams(new Uri(url), translatorParamsItem.Id, translatorParamsItem.TranslatorId, translatorParamsItem.IsCamrip, translatorParamsItem.HasAds, translatorParamsItem.IsDirectorCut).Result;
                    return streams.Select(stream => {
                        return new Models.Stream() {
                            Name = $"{translatorParamsItem.Title} {stream.Quality}",
                            Title = title,
                            Url = stream.Url
                        };
                    });
                }).ToArray();
            }
        }

        async Task<IDocument> GetDocument(string url, string html) {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(response => {
                response.Address(url);
                response.Content(html);
            });

            return document;
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

        record TranslatorParams(string Title, int Id, int TranslatorId, bool IsCamrip, bool HasAds, bool IsDirectorCut);

        public static int LevenshteinDistance(string source, string target) {
            if (source == target) { return 0; }
            if (source.Length == 0) { return target.Length; }
            if (target.Length == 0) { return source.Length; }

            // create two work vectors of integer distances
            var v0 = new int[target.Length + 1];
            var v1 = new int[target.Length + 1];

            // initialize v0 (the previous row of distances)
            // this row is A[0][i]: edit distance for an empty s
            // the distance is just the number of characters to delete from t
            for (var i = 0; i < v0.Length; i++) {
                v0[i] = i;
            }

            for (int i = 0; i < source.Length; i++) {
                // calculate v1 (current row distances) from the previous row v0

                // first element of v1 is A[i+1][0]
                //   edit distance is delete (i+1) chars from s to match empty t
                v1[0] = i + 1;

                // use formula to fill in the rest of the row
                for (var j = 0; j < target.Length; j++) {
                    var cost = (source[i] == target[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min(v1[j] + 1, Math.Min(v0[j + 1] + 1, v0[j] + cost));
                }

                // copy v1 (current row) to v0 (previous row) for next iteration
                for (var j = 0; j < v0.Length; j++) {
                    v0[j] = v1[j];
                }
            }

            return v1[target.Length];
        }
    }

    class HdRezkaApi {
        readonly Uri host;
        readonly CookieContainer cookies;

        public HdRezkaApi(Uri host, CookieContainer cookies) {
            this.host = host;
            this.cookies = cookies;
        }

        public async Task<HdRezkaSearchResult[]> Search(Models.Meta meta) {
            using (var client = GetClient()) {
                client.DefaultRequestHeaders.Add("Referer", host.ToString());
                var vars = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("q", meta.Name + " " + meta.Year)
                };
                var formContent = new FormUrlEncodedContent(vars);
                var url = new Uri(host, "/engine/ajax/search.php");
                var response = await client.PostAsync(url, formContent);
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
                    var originalTitles = !string.IsNullOrWhiteSpace(description) ? description.Split(" / ", StringSplitOptions.TrimEntries) : new string[0];

                    var yearStrings = yearString.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    int.TryParse(yearStrings[0], out var startYear);
                    int.TryParse(yearStrings.Length > 1 ? yearStrings[1] : "", out var endYear);

                    var titles = title.Split('/').Select(x => x.Trim()).ToArray();

                    return new HdRezkaSearchResult(url, titles, originalTitles, startYear, endYear, type);
                }).ToArray();
            }
        }

        public async Task<HdRezkaMetadata[]> GetMetadata(Uri url) {
            using (var client = GetClient()) {
                var html = await client.GetStringAsync(url);
                var document = await GetHtmlDocument(html, url);
                var translatorItems = document.QuerySelectorAll(".b-translator__item");
                if (translatorItems.Any()) {
                    return translatorItems.Select(item => {
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

                        return new HdRezkaMetadata(title, id, translatorId, isCamrip, hasAds, isDirectorCut);
                    }).ToArray();
                }

                var scripts = document.QuerySelectorAll("script");
                var movieScript = scripts.FirstOrDefault(x => x.InnerHtml.Contains("sof.tv.initCDNMoviesEvents"));
                if (movieScript != null) {
                    var match = Regex.Match(movieScript.InnerHtml, @"sof.tv.initCDNMoviesEvents\((?<id>[0-9]*)[ ,]*(?<translatorId>[0-9]*)");

                    var title = "";
                    var idString = match.Groups["id"].Value.Trim();
                    var id = int.Parse(idString);
                    var translatorIdString = match.Groups["translatorId"].Value.Trim();
                    var translatorId = int.Parse(translatorIdString);

                    return new[] { new HdRezkaMetadata(title, id, translatorId) };
                }

                var seriesScript = scripts.FirstOrDefault(x => x.InnerHtml.Contains("sof.tv.initCDNSeriesEvents"));
                if (seriesScript != null) {
                    var match = Regex.Match(seriesScript.InnerHtml, @"sof.tv.initCDNSeriesEvents\((?<id>[0-9]*)[ ,]*(?<translatorId>[0-9]*)");

                    var title = "";
                    var idString = match.Groups["id"].Value.Trim();
                    var id = int.Parse(idString);
                    var translatorIdString = match.Groups["translatorId"].Value.Trim();
                    var translatorId = int.Parse(translatorIdString);

                    return new[] { new HdRezkaMetadata(title, id, translatorId) };
                }

                throw new NotImplementedException($"Not able to parse metadata for {url}");
            }
        }

        public async Task<IReadOnlyDictionary<int, int[]>> GetEpisodes(Uri refererUrl, int id, int translatorId) {
            using (var client = GetClient()) {
                var vars = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("id", id.ToString()),
                    new KeyValuePair<string, string>("translator_id", translatorId.ToString()),
                    new KeyValuePair<string, string>("action", "get_episodes")
                };
                var formContent = new FormUrlEncodedContent(vars);

                client.DefaultRequestHeaders.Add("Referer", refererUrl.ToString());
                var url = new Uri(host, "/ajax/get_cdn_series/?t={DateTimeOffset.Now.ToUnixTimeSeconds()}");
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

        public async Task<IMediaStream[]> GetMovieStreams(Uri refererUrl, int id, int translatorId, bool isCamrip, bool hasAds, bool isDirectorsCut) {
            using (var client = GetClient()) {
                var vars = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("id", id.ToString()),
                    new KeyValuePair<string, string>("translator_id", translatorId.ToString()),
                    new KeyValuePair<string, string>("is_camrip", isCamrip ? "1" : "0"),
                    new KeyValuePair<string, string>("is_ads", hasAds ? "1" : "0"),
                    new KeyValuePair<string, string>("is_director", isDirectorsCut ? "1" : "0"),
                    new KeyValuePair<string, string>("action", "get_movie")
                };
                var formContent = new FormUrlEncodedContent(vars);

                client.DefaultRequestHeaders.Add("Referer", refererUrl.ToString());
                var url = new Uri(host, "/ajax/get_cdn_series/?t={DateTimeOffset.Now.ToUnixTimeSeconds()}");
                var response = await client.PostAsync(url, formContent);
                var json = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonSerializer.Deserialize<HdRezkaApiResponse>(json);
                var decoded = PayloadDecode(data.Payload);
                var parts = decoded.Split(",");
                return parts.Select(part => {
                    var quality = Regex.Match(part, @"(\[.*?\])").Value;
                    var urls = Regex.Matches(part, @"https://[^ ,]*").Select(x => x.Value);

                    return (quality, urls);
                }).Where(item => {
                    return item.quality == "[1080p Ultra]" || item.quality == "[720p]" || item.quality == "[480p]";
                }).Select(item => {
                    var streamUrl = item.urls.FirstOrDefault(x => x.EndsWith(".mp4")) ?? item.urls.First().Replace(":hls:manifest.m3u8", "");
                    return new MovieMediaStream(streamUrl, item.quality);
                }).ToArray();
            }
        }

        public async Task<IMediaStream[]> GetSeriesEpisodeStreams(Uri refererUrl, int id, int translatorId, int season, int episode) {
            using (var client = GetClient()) {
                var vars = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("id", id.ToString()),
                    new KeyValuePair<string, string>("translator_id", translatorId.ToString()),
                    new KeyValuePair<string, string>("season", season.ToString()),
                    new KeyValuePair<string, string>("episode", episode.ToString()),
                    new KeyValuePair<string, string>("action", "get_stream")
                };
                var formContent = new FormUrlEncodedContent(vars);

                client.DefaultRequestHeaders.Add("Referer", refererUrl.ToString());
                var url = new Uri(host, "/ajax/get_cdn_series/?t={DateTimeOffset.Now.ToUnixTimeSeconds()}");
                var response = await client.PostAsync(url, formContent);
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = System.Text.Json.JsonSerializer.Deserialize<HdRezkaApiResponse>(json);

                var html = apiResponse.EpisodesHtml;

                var decoded = PayloadDecode(apiResponse.Payload);
                var parts = decoded.Split(",");
                return parts.Select(part => {
                    var quality = Regex.Match(part, @"(\[.*?\])").Value;
                    var urls = Regex.Matches(part, @"https://[^ ,]*").Select(x => x.Value);

                    return (quality, urls);
                }).Where(item => {
                    return item.quality == "[1080p Ultra]" || item.quality == "[720p]" || item.quality == "[480p]";
                }).Select(item => {
                    var streamUrl = item.urls.FirstOrDefault(x => x.EndsWith(".mp4")) ?? item.urls.First().Replace(":hls:manifest.m3u8", "");
                    return new MovieMediaStream(streamUrl, item.quality);
                }).ToArray();
            }
        }

        static string PayloadDecode(string encodedPayload) {
            encodedPayload = encodedPayload.Substring(2);
            //var decodedPayload = Regex.Replace(encodedPayload, "(//_)|(//[a-zA-Z0-9=]{1,16})", "");

            //return Encoding.UTF8.GetString(Convert.FromBase64String(decodedPayload));

            var decodedPayload = "";
            var prevIdx = 0;
            while (true) {
                if (prevIdx == encodedPayload.Length) { break; }

                var nextIdx = encodedPayload.IndexOf("//_//", prevIdx == 0 ? prevIdx : prevIdx + 1);
                if (nextIdx == -1) {
                    nextIdx = encodedPayload.Length; 
                }

                var tmpString = "";
                while (true) {
                    tmpString = encodedPayload.Substring(prevIdx, nextIdx - prevIdx);
                    if (tmpString.Length < 4) {
                        break;
                    }

                    if (tmpString.StartsWith("//_//")) {
                        prevIdx = prevIdx + 5;
                        continue;
                    } else if (tmpString.StartsWith("//")) {
                        prevIdx = prevIdx + 2;
                        continue;
                    }
                    var encodedChunk = tmpString.Substring(0, 4);
                    var decodedChunk = Encoding.UTF8.GetString(Convert.FromBase64String(encodedChunk));
                    if (Regex.IsMatch(decodedChunk, @"[!$@#^]{2,3}")) {
                        prevIdx = prevIdx + 4;
                        continue;
                    }

                    break;
                }

                decodedPayload += tmpString;
                prevIdx = nextIdx;
            }

            if (decodedPayload.Contains("/")) {
                decodedPayload.ToString();
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(decodedPayload));
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

        public record HdRezkaSearchResult(Uri Url, string[] Titles, string[] AdditionalTitles, int StartYear, int? EndYear, HdRezkaMediaType Type);

        public record HdRezkaMetadata(string Title, int Id, int TranslatorId, bool? IsCamrip = null, bool? HasAds = null, bool? IsDirectorCut = null);

        public interface IMediaStream {
            string Url { get; }
            string Quality { get; }
        }

        record MovieMediaStream(string Url, string Quality) : IMediaStream;
        record SeriesEpisodeMediaStream(string Url, string Quality, int Season, int Episode) : IMediaStream;

        public enum HdRezkaMediaType {
            Movie,
            Series
        }

        public partial class HdRezkaApiResponse {
            [System.Text.Json.Serialization.JsonPropertyName("success")]
            public bool Success { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("message")]
            public string Message { get; set; }


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
}
