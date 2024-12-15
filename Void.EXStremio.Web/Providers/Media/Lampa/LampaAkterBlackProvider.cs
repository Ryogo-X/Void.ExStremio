using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaAkterBlackProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Akter-Black)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://lam.akter-black.com/"); }
        }

        protected override string[] AllowedCdn {
            //get { return ["anilibria", "animebesst", "animelib", "ashdi", "filmix", "moonanime", "zetflix", "lumex", "hdvb", "kinoukr", "fancdn", "redheadsound", "vibix", "remux"]; }
            get { return ["filmix", "zetflix", "lumex", "kinoukr", "fancdn", "vibix"]; }
        }

        public LampaAkterBlackProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
