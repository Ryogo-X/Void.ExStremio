namespace Void.EXStremio.Web.Models {
    public class GraphqlQuery<T> {
        public string OperationName { get; set; }
        public T Variables { get; set; }
        public string Query { get; set; }
    }
}
