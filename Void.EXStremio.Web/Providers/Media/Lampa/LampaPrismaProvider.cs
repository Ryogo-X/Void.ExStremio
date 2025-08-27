using Microsoft.Extensions.Caching.Memory;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaPrismaProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Prisma"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("https://clock.manhan.one/"); }
        }

        protected override string InitUriPath { get; } = "/lite/events";
        protected override string CustomArgs {
            get { return "&account_email=rsmail@ukr.net"; }
        }

        protected override string[] AllowedCdn {
            get { return ["aniliberty", "ashdi", "collaps", "collaps-dash", "eneyida", "hdvb", "kinotochka", "lumex", "rezka"]; }
        }

        public LampaPrismaProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
