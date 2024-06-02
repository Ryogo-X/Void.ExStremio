namespace Void.EXStremio.Web.Models.Kinopoisk.Response {
    public class SearchSuggestKpResponse {
        public GlobalKpResponse<PaginatedKpResponse<MovieItemKpResponse>> Suggest { get; set; }
    }
}
