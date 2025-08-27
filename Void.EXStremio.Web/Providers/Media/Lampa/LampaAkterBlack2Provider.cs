using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaAkterBlack2Provider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Akter-Black)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://lam2.akter-black.com/"); }
        }

        protected override string[] AllowedCdn {
            get { return ["cdnvideohub", "hdvb", "kinobase", "kinotochka", "lumex", "plvideo", "remux", "vibix", "veoveo", "zetflix", "aniliberty", "animevost", "animebest"]; }
        }

        public LampaAkterBlack2Provider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
