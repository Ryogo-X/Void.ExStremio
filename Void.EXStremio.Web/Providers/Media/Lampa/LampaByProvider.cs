
using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaByProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (ByLampa)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://bylampa.online/"); }
        }

        protected override string[] AllowedCdn {
            get {
                throw new NotImplementedException();
            }
        }

        public LampaByProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
