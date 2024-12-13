
using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaShowyProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Showy)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://showy.online/"); }
        }

        public LampaShowyProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
