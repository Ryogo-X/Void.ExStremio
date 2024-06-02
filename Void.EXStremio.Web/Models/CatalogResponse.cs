namespace Void.EXStremio.Web.Models {
    public class CatalogResponse<T> where T : Meta {
        public string Query { get; set; }
        public double Rank { get; set; }
        public int CacheMaxAge { get; set; }
        public T[] Metas { get; set; }
    }
}
