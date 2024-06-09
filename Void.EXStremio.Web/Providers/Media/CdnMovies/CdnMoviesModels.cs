using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Void.EXStremio.Web.Providers.Media.CdnMovies {
    class CdnMoviesSearchResponse {
        public CdnMoviesSearchItemResponse[] Data { get; set; }
    }

    class CdnMoviesSearchItemResponse {
        public string Id { get; set; }
        [JsonPropertyName("iframe_src")]
        public Uri Uri { get; set; }
        [JsonPropertyName("content_type")]
        public string Type { get; set; }
        [JsonPropertyName("ru_title")]
        public string Title { get; set; }
        [JsonPropertyName("orig_title")]
        public string OriginalTitle { get; set; }
        public int Year { get; set; }
        [JsonPropertyName("kinopoisk_id")]
        public int? KpId { get; set; }
        [JsonPropertyName("imdb_id")]
        public string ImdbId { get; set; }
        [JsonPropertyName("tmdb_id")]
        public int? TmdbId { get; set; }
        public string Description { get; set; }
    }

    class CdnMoviesTvSeasonResponse {
        public string Title { get; set; }
        [JsonPropertyName("folder")]
        public CdnMoviesTvEpisodeResponse[] Episodes { get; set; }

        public int GetNumber() {
            var value = Regex.Match(Title, "[0-9]*").Value;

            return int.Parse(value);
        }
    }

    class CdnMoviesTvEpisodeResponse {
        public int Episode { get; set; }
        public string Title { get; set; }
        [JsonPropertyName("folder")]
        public CdnMoviesPlaylistItemResponse[] Links { get; set; }

        public int GetNumber() {
            var value = Regex.Match(Title, "[0-9]*").Value;

            return int.Parse(value);
        }
    }

    class CdnMoviesPlaylistItemResponse {
        public string Title { get; set; }
        [JsonInclude]
        string File { get; set; }

        public IEnumerable<(string Quality, string Url)> GetLinks() {
            var lines = File.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines) {
                var quality = Regex.Match(line, @"\[(?<quality>[0-9]+)p\]").Groups["quality"].Value;
                var url = Regex.Match(line, @"(http|https)[^ ,]+").Value.Replace(":hls:manifest.m3u8", "");

                yield return (quality, url); 
            }          
        }
    }
}