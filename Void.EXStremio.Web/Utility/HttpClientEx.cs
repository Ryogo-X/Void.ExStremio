using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Utility {
    public class HttpClientEx : IDisposable {
        private readonly HttpClient client;
        private readonly IMemoryCache cache;

        public TimeSpan Timeout {
            get { return client.Timeout; }
            set { client.Timeout = value; }
        }

        public HttpClientEx(HttpClient client, IMemoryCache cache) {
            this.client = client;
            this.cache = cache;
        }

        public async Task<string> GetStringAsync(Uri uri, bool useCache = false, TimeSpan? expiration = null) {
            return await GetStringAsync(uri.ToString(), useCache, expiration);
        }

        public async Task<string> GetStringAsync(string uri, bool useCache = false, TimeSpan? expiration = null) {
            if (useCache && !expiration.HasValue) { throw new ArgumentNullException(nameof(expiration)); }
            if (useCache && cache.TryGetValue<string>(uri, out var value)) { return value; }

            value = await client.GetStringAsync(uri);
            if (useCache) { cache.Set(uri, value); }

            return value;
        }

        public void Dispose() {
            client?.Dispose();
        }
    }
}
