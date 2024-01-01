using System.Text.Json;
using System.Text.Json.Serialization;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Providers {
    public class KinopoiskProvider {
        readonly static JsonSerializerOptions serializerOptions = new JsonSerializerOptions() {
            PropertyNameCaseInsensitive = true
        };

        public async Task<Meta[]> Search(string query) {
            using (var client = GetClient()) {
                var json = await client.GetStringAsync($"https://kinopoiskapiunofficial.tech/api/v2.1/films/search-by-keyword?keyword={query}&page=1");
                var result = JsonSerializer.Deserialize<KpSearchResults>(json, serializerOptions);

                return result.Items.Select(item => {
                    var meta = new Meta();
                    meta.Id = "kp" + item.FilmId;
                    meta.Name = item.NameRu;
                    if (!string.IsNullOrWhiteSpace(item.NameEn)) {
                        meta.Name += " / " + item.NameEn;
                    }
                    meta.Type = "any";
                    meta.Poster = item.PosterUrlPreview;

                    return meta;
                }).ToArray();
            }
        }

        public async Task<Meta> Get(long id) {
            using (var client = GetClient()) {
                var json = await client.GetStringAsync($"https://kinopoiskapiunofficial.tech/api/v2.2/films/{id}");
                var item = JsonSerializer.Deserialize<KpItem>(json, serializerOptions);

                var meta = new Meta();
                meta.Id = "kp" + id;
                meta.Name = item.NameRu;
                if (!string.IsNullOrWhiteSpace(item.NameEn)) {
                    meta.Name += " / " + item.NameEn;
                } else if (!string.IsNullOrWhiteSpace(item.NameOriginal)) {
                    meta.Name += " / " + item.NameOriginal;
                }
                meta.Description = item.Description;
                meta.Year = item.Year;
                meta.Genres = item.Genres.Select(x => x.Value).ToArray();
                meta.Runtime = item.FilmLength?.ToString() + "м";

                if (!string.IsNullOrWhiteSpace(item.ImdbId)) {
                    meta.ImdbId = item.ImdbId;
                    meta.Poster = MetaImage.GetPoster(meta.ImdbId);
                    meta.Background = MetaImage.GetBackground(meta.ImdbId);
                    meta.Logo = MetaImage.GetLogo(meta.ImdbId);
                    meta.ImdbRating = item.RatingImdb?.ToString("##.##");
                    if (item.Type == "FILM") {
                        meta.Type = "movie";
                    } else if (item.Type == "TV_SERIES") {
                        meta.Type = "series";
                    } else {
                        meta.Type = "other";
                    }
                } else {
                    meta.Poster = item.PosterUrl;
                    meta.Type = "any";
                }

                return meta;
            }
        }

        HttpClient GetClient() {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", "5dae0049-fe63-4925-a990-eead8c7152d9");

            return client;
        }

        public partial class KpSearchResults {
            [JsonPropertyName("keyword")]
            public string Keyword { get; set; }

            [JsonPropertyName("pagesCount")]
            public long PagesCount { get; set; }

            [JsonPropertyName("films")]
            public KpSearchItem[] Items { get; set; }
        }

        public partial class KpSearchItem {
            [JsonPropertyName("filmId")]
            public long FilmId { get; set; }

            [JsonPropertyName("nameRu")]
            public string NameRu { get; set; }

            [JsonPropertyName("nameEn")]
            public string NameEn { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("year")]
            public string Year { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("filmLength")]
            public string FilmLength { get; set; }

            [JsonPropertyName("countries")]
            public Country[] Countries { get; set; }

            [JsonPropertyName("genres")]
            public Genre[] Genres { get; set; }

            [JsonPropertyName("rating")]
            public string Rating { get; set; }

            [JsonPropertyName("ratingVoteCount")]
            public long RatingVoteCount { get; set; }

            [JsonPropertyName("posterUrl")]
            public Uri PosterUrl { get; set; }

            [JsonPropertyName("posterUrlPreview")]
            public Uri PosterUrlPreview { get; set; }
        }

        public partial class Country {
            [JsonPropertyName("country")]
            public string CountryCountry { get; set; }
        }

        public partial class Genre {
            [JsonPropertyName("genre")]
            public string Value { get; set; }
        }

        public partial class KpItem {
            [JsonPropertyName("kinopoiskId")]
            public long KinopoiskId { get; set; }

            [JsonPropertyName("imdbId")]
            public string ImdbId { get; set; }

            [JsonPropertyName("nameRu")]
            public string NameRu { get; set; }

            [JsonPropertyName("nameEn")]
            public string NameEn { get; set; }

            [JsonPropertyName("nameOriginal")]
            public string NameOriginal { get; set; }

            [JsonPropertyName("posterUrl")]
            public Uri PosterUrl { get; set; }

            [JsonPropertyName("posterUrlPreview")]
            public Uri PosterUrlPreview { get; set; }

            [JsonPropertyName("coverUrl")]
            public object CoverUrl { get; set; }

            [JsonPropertyName("logoUrl")]
            public object LogoUrl { get; set; }

            [JsonPropertyName("reviewsCount")]
            public long ReviewsCount { get; set; }

            [JsonPropertyName("ratingGoodReview")]
            public object RatingGoodReview { get; set; }

            [JsonPropertyName("ratingGoodReviewVoteCount")]
            public long RatingGoodReviewVoteCount { get; set; }

            [JsonPropertyName("ratingKinopoisk")]
            public double? RatingKinopoisk { get; set; }

            [JsonPropertyName("ratingKinopoiskVoteCount")]
            public long RatingKinopoiskVoteCount { get; set; }

            [JsonPropertyName("ratingImdb")]
            public double? RatingImdb { get; set; }

            [JsonPropertyName("ratingImdbVoteCount")]
            public long RatingImdbVoteCount { get; set; }

            [JsonPropertyName("ratingFilmCritics")]
            public object RatingFilmCritics { get; set; }

            [JsonPropertyName("ratingFilmCriticsVoteCount")]
            public long RatingFilmCriticsVoteCount { get; set; }

            [JsonPropertyName("ratingAwait")]
            public object RatingAwait { get; set; }

            [JsonPropertyName("ratingAwaitCount")]
            public long RatingAwaitCount { get; set; }

            [JsonPropertyName("ratingRfCritics")]
            public object RatingRfCritics { get; set; }

            [JsonPropertyName("ratingRfCriticsVoteCount")]
            public long RatingRfCriticsVoteCount { get; set; }

            [JsonPropertyName("webUrl")]
            public Uri WebUrl { get; set; }

            [JsonPropertyName("year")]
            public int? Year { get; set; }

            [JsonPropertyName("filmLength")]
            public long? FilmLength { get; set; }

            [JsonPropertyName("slogan")]
            public string Slogan { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("shortDescription")]
            public string ShortDescription { get; set; }

            [JsonPropertyName("editorAnnotation")]
            public string EditorAnnotation { get; set; }

            [JsonPropertyName("isTicketsAvailable")]
            public bool IsTicketsAvailable { get; set; }

            [JsonPropertyName("productionStatus")]
            public object ProductionStatus { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("ratingMpaa")]
            public object RatingMpaa { get; set; }

            [JsonPropertyName("ratingAgeLimits")]
            public string RatingAgeLimits { get; set; }

            [JsonPropertyName("countries")]
            public Country[] Countries { get; set; }

            [JsonPropertyName("genres")]
            public Genre[] Genres { get; set; }

            [JsonPropertyName("startYear")]
            public long? StartYear { get; set; }

            [JsonPropertyName("endYear")]
            public long? EndYear { get; set; }

            [JsonPropertyName("serial")]
            public bool Serial { get; set; }

            [JsonPropertyName("shortFilm")]
            public bool ShortFilm { get; set; }

            [JsonPropertyName("completed")]
            public bool Completed { get; set; }

            [JsonPropertyName("hasImax")]
            public bool HasImax { get; set; }

            [JsonPropertyName("has3D")]
            public bool Has3D { get; set; }

            [JsonPropertyName("lastSync")]
            public DateTimeOffset LastSync { get; set; }
        }
    }
}