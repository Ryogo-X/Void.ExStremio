
using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaLampacProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Lampac)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("http://80.85.247.249:9118/"); }
        }

        protected override string[] AllowedCdn {
            get {
                throw new NotImplementedException();
            }
        }
        public LampaLampacProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
