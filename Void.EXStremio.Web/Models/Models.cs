namespace Void.EXStremio.Web.Models {
    class CustomIdResult {
        public string Id { get; }
        public TimeSpan? Expiration { get; }

        public CustomIdResult(string id, TimeSpan? expiration = null) {
            Id = id;
            Expiration = expiration;
        }
    }
}
