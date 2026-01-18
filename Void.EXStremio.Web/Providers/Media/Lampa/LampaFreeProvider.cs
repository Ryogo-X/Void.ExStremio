using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Controllers;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaFreeProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Prisma"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("http://212.34.151.5:8880/"); }
        }

        protected override string InitUriPath { get; } = "/lite/events";
        protected override string CustomArgs {
            get { return "&account_email=rsmail@ukr.net"; }
        }

        protected override string[] AllowedCdn {
            get { 
                return [
                    "vdbmovies", "lumex", "cdnvideohub"
                    ];
            }
        }

        public LampaFreeProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<StreamController> logger) : base(httpClientFactory, cache, logger) { }
    }
}
