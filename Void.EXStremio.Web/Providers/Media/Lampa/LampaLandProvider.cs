
using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaLandProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Land)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("http://online.lampa.land/"); }
        }

        public LampaLandProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
