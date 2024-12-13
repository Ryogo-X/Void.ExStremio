
using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaVautiouhProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Vautiouh)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://vautiouh.deploy.cx/"); }
        }

        public LampaVautiouhProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
