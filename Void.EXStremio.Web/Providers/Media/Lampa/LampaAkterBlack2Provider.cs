using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Controllers;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaAkterBlack2Provider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Akter-Black)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://lam2.akter-black.com/"); }
        }

        protected override string[] AllowedCdn {
            get { 
                return [
                    //"kinobase" - IP bind :(
                    //"kinotochka" - IP bind :(
                    //"plvideo" - IP bind :(
                    //"remux" - IP bind :(
                    //"animevost" - proxy :(
                    //"cdnvideohub" - proxy :(
                    "hdvb", "lumex", "vibix", "veoveo", "zetflix", "aniliberty", "animebest"
                    ];
            }
        }

        public LampaAkterBlack2Provider(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<StreamController> logger) : base(httpClientFactory, cache, logger) { }
    }
}
