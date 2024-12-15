
using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaVautiouhProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Vautiouh)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://vautiouh.deploy.cx/"); }
        }

        protected override string[] AllowedCdn {
            //"filmix"
            //get { return ["ashdi", "hdvb", "kinotochka", "redheadsound", "zetflix", "remux", "anilibria", "animebesst", "animelib", "animevost"]; }
            get { return ["ashdi", "zetflix", "animevost"]; }
        }

        public LampaVautiouhProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
