
using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaBwaProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (BWA)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://rc.bwa.to/"); }
        }

        protected override string[] AllowedCdn {
            get { return []; }
        }

        public LampaBwaProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
