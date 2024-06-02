using Microsoft.AspNetCore.Mvc;
using Void.EXStremio.Web.Models;

namespace Void.EXStremio.Web.Controllers {
    [ApiController]
    public class ManifestController : Controller {
        static readonly Manifest manifest = new Manifest {
            Id = "void.exstremio",
            Version = "0.0.1",
#if !DEBUG
            Name = "EXStremio",
#else
            Name = "EXStremio (DEBUG)",
#endif
            //Description = "Kinopoisk/TVMaze meta provider.",
            Resources = [
                //"catalog",
                //"meta",
                "stream"
            ],
            //IdPrefixes = new string[] { "tt", "anidub", "kp", "tvmz" },
            //IdPrefixes = new string[] { "kp", "tt" },
            IdPrefixes = ["tt"],
            Types = ["movie", "series"],
            //Types = new string[] { "any", "movie", "series" },
            //Types = new string[] { "any" },
            Catalogs = new Catalog[] {
                //new Catalog {
                //    Type = "any",
                //    Id = "kinopoisk",
                //    Name = "RU",
                //    Extra = new[] {
                //        //new ExtraParam() {
                //        //    Name = "skip", IsRequired = false
                //        //},
                //        new ExtraParam() {
                //            Name = "search", IsRequired = true
                //        }
                //    }
                //}
            }
        };

        [Route("manifest.json")]
        [HttpGet]
        public JsonResult Get() {
            return new JsonResult(manifest);
        }
    }
}
