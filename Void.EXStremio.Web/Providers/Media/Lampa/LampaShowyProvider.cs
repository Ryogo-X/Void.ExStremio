
using Microsoft.Extensions.Caching.Memory;

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
            //get { return ["hdvb", "remux", "redheadsound", "anilibria", "animebesst", "animelib"]; }
            //get { return ["hdvb", "remux", "redheadsound", "anilibria"]; }
            get { return []; }
        }

        public LampaShowyProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache) : base(httpClientFactory, cache) { }
    }
}
