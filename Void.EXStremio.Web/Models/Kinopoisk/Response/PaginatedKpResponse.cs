namespace Void.EXStremio.Web.Models.Kinopoisk.Response {
    public class PaginatedKpResponse<T> {
        public int Offset { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public GlobalKpResponse<T>[] Items { get; set; }
    }
}