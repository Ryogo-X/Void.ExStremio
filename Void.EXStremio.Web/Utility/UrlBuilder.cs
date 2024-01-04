namespace Void.EXStremio.Web.Utility {
    public static class UrlBuilder {
        public static Uri AbsoluteUrl(HttpRequest request, string relativeUrl) {
            var proto = request.Headers.ContainsKey("X-Forwarded-Proto") ? request.Headers["X-Forwarded-Proto"].First() : request.Scheme;
            var host = request.Headers.ContainsKey("X-Forwarded-Host") ? request.Headers["X-Forwarded-Host"].First() : request.Host.Value;
            var prefix = request.Headers.ContainsKey("X-Forwarded-Prefix") ? request.Headers["X-Forwarded-Prefix"].First()?.TrimEnd('/') : "";

            return new Uri(proto + "://" + host + prefix + relativeUrl);
        }
    }
}
