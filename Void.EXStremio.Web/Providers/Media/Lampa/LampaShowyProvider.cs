using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Controllers;

namespace Void.EXStremio.Web.Providers.Media.Lampa {
    class LampaShowyProvider : LampaMediaProvider {
        public override string ServiceName {
            get { return "Lampa (Showy)"; }
        }

        protected override Uri BaseUri {
            get { return new Uri("http://showwwy.com/"); }
        }

        protected override string InitUriPath { get; } = "/lite/events";
        protected override string CustomArgs { get; } = "&showy_token=9dc8771f-ba96-49e8-a33e-7c86acad54b5";

        protected override string[] AllowedCdn {
            get { return ["animevost", "hdvb", "remux", "veoveo", "zetflix"]; }
        }

        public LampaShowyProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<StreamController> logger) : base(httpClientFactory, cache, logger) { }
    }
}
