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
                    //"kinobase" - proxy :(
                    //"kinotochka" - proxy :(
                    //"plvideo" - proxy :(
                    //"remux" - proxy :(
                    //"animevost" - proxy :(
                    //"cdnvideohub" - proxy :(
                    "animedia", "animebest", "aniliberty", "cdnvideohub", "lumex", "veoveo", "vibix", "zetflix", "hdvb", "eneyida"
                    ]; 
            }
        }

        public LampaAkterBlackProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<StreamController> logger) : base(httpClientFactory, cache, logger) { }
    }
}
