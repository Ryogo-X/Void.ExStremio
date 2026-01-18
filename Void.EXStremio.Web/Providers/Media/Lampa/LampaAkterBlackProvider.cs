using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Controllers;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaAkterBlackProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Akter-Black)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://lam2.akter-black.com/"); }
        }

        protected override string[] AllowedCdn {
            get { 
                return [
                    "paladin", "mirage", "kinobase", "rezka", "veoveo", "remux", "kinotochka", "videodb",
                    "aniliberty", "animevost", "animebesst", "kodik"
                    ]; 
            }
        }

        public LampaAkterBlackProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<StreamController> logger) : base(httpClientFactory, cache, logger) { }
    }
}
