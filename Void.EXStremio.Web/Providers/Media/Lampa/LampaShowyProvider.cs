
using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaShowyProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Showy)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://showy.online/"); }
        }

        protected override string[] AllowedCdn {
            get { return ["hdvb", "remux", "redheadsound", "anilibria", "animebesst", "animelib"]; }
        }

        public LampaShowyProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
