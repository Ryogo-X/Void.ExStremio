using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaCubProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (CUB.RIP)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("http://185.87.48.42:2627/"); }
        }

        protected override string CustomArgs {
            get { return "&uid=tyusdt"; }
        }

        protected override string[] AllowedCdn {
            get { return ["plvideo", "veoveo", "remux", "redheadsound", "hdvb", "kinotochka", "eneyida", "collaps", "collapse-dash", "kodik", "animevost", "animedia", "moonanime", "aniliberty", "animebest"]; }
        }

        public LampaCubProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
