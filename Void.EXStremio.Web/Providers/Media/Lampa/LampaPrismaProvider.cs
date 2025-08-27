﻿using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Controllers;

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
            get { 
                return [
                    //"collaps" - proxy :(
                    //"collaps-dash" - proxy :(
                    //"kinotochka" - proxy :(
                    //"rezka" - proxy :(
                    "aniliberty", "ashdi", "eneyida", "hdvb", "lumex"
                    ];
            }
        }

        public LampaPrismaProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<StreamController> logger) : base(httpClientFactory, cache, logger) { }
    }
}
