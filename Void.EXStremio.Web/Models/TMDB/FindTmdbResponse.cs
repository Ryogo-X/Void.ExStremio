using System.Text.Json.Serialization;

namespace Void.EXStremio.Web.Models.TMDB {
    public class FindTmdbResponse {
        [JsonPropertyName("movie_results")]
        public FindMovieTmdbResponse[] MovieResults { get; set; }
        [JsonPropertyName("tv_results")]
        public FindTvTmdbResponse[] TvResults { get; set; }
    }

    public class FindMovieTmdbResponse {
        public int Id { get; set; }
        public string Title { get; set; }
        [JsonPropertyName("original_title")]
        public string OriginalTitle { get; set; }
        [JsonPropertyName("original_language")]
        public string OriginalLanguage { get; set; }
    }

    public class FindTvTmdbResponse { 
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonPropertyName("original_name")]
        public string OriginalName { get; set; }
        [JsonPropertyName("original_language")]
        public string OriginalLanguage { get; set; }
    }
}
