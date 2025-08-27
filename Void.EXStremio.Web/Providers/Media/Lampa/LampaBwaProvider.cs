using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Controllers;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaBwaProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (BWA)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://rc.bwa.to/"); }
        }
        protected override string[] AllowedCdn {
            get { return ["ashdi", "hdvb", "kinotochka", "remux", "veoveo"]; }
        }

        public LampaBwaProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<StreamController> logger) : base(httpClientFactory, cache, logger) { }
    }
}
