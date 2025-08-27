using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaAkterBlackProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Akter-Black)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://lam2.akter-black.com/"); }
        }

        protected override string[] AllowedCdn {
            //get { return ["anilibria", "animebesst", "animelib", "ashdi", "filmix", "moonanime", "zetflix", "lumex", "hdvb", "kinoukr", "fancdn", "redheadsound", "vibix", "remux"]; }
            //get { return ["zetflix", "lumex", "kinoukr", "fancdn", "vibix"]; }
            get { return ["animevost", "animedia", "animebest", "aniliberty", "cdnvideohub", "kinotochka", "lumex", "veoveo", "vibix", "zetflix", "hdvb", "kinobase", "plvideo", "remux", "eneyida"]; }
        }

        public LampaAkterBlackProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
