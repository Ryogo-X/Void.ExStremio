namespace Void.EXStremio.Web.Utility {
    static class MetaImage {
        public static Uri GetPoster(string id) {
            return new Uri($"https://images.metahub.space/poster/small/{id}/img");
        }

        public static Uri GetLogo(string id) {
            return new Uri($"https://images.metahub.space/logo/medium/{id}/img");
        }

        public static Uri GetBackground(string id) {
            return new Uri($"https://images.metahub.space/background/medium/{id}/img");
        }
    }
}
