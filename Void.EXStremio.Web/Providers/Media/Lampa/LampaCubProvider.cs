using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Controllers;

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
            get { 
                return [
                    //"kinotochka" - proxy :(
                    //"plvideo" - proxy :(
                    //"remux" - proxy :(
                    //"collaps - proxy :(
                    //"collapse-dash" - proxy :(
                    //"animevost" - proxy :(
                    "veoveo", "redheadsound", "hdvb", "eneyida", "kodik", "animedia", "moonanime", "aniliberty", "animebest"
                    ]; 
            }
        }

        public LampaCubProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<StreamController> logger) : base(httpClientFactory, cache, logger) { }
    }
}
