
using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaLandProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Land)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("http://online3.lampa.land/"); }
        }

        protected override string[] AllowedCdn {
            //get { return ["alloha", "cdnvideohub", "filmixtv", "hdvb", "kinopub", "kinotochka", "kinoukr", "lumex", "remux", "rhsprem", "vibix", "zetflix", "anilibria", "animebesst", "animelib", "moonanime"]; }
            get { return ["kinopub", "rhsprem"]; }
        }

        public LampaLandProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
