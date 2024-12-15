
using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaByProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (ByLampa)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("http://185.87.48.42:2626/"); }
        }

        protected override string[] AllowedCdn {
            get { return ["anilibria", "animebesst", "animelib", "animevost", "filmix", "kinotochka", "moonanime", "hdvb", "kinoukr", "remux"]; }
        }

        public LampaByProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
