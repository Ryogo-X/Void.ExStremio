
using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaLandProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Land)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("http://online.lampa.land/"); }
        }

        protected override string[] AllowedCdn {
            get { return ["hdvb", "remux", "redheadsound", "animebesst", "animelib"]; }
        }

        public LampaLandProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
