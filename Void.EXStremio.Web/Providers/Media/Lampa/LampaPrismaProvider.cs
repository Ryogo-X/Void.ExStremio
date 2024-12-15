using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaPrismaProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Prisma"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://api.manhan.one/"); }
        }

        protected override string InitUriPath { get; } = "/lite/events";

        protected override string[] AllowedCdn {
            //get { return ["megatv", "ashdi", "hdvb", "kinotochka", "animebesst", "animelib", "moonanime"]; }
            get { return ["kinotochka", "animebesst", "animelib", "moonanime"]; }
        }

        public LampaPrismaProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
