using Microsoft.AspNetCore.Mvc;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Providers;

namespace Void.EXStremio.Web.Controllers {
    // search results
    [ApiController]
    [Route("[controller]")]
    public class CatalogController : Controller {
        // GET /catalog
        [HttpGet("{type}/{id?}/{searchArg?}")]
        public async Task<JsonResult> Get(string type, string? id = null, string? searchArg = null) {
            return new JsonResult(new { metas = new[] { 
                new Meta() {
                    Name = "TEST-TEST-TEST",
                    Type = "movie",
                    ImdbId = id
                }
            } });

            //if (type == "any" && id == "kinopoisk") {
            //    var keyword = searchArg.Split('=').Last().Replace(".json", "");
            //    var provider = new KinopoiskProvider();

            //    var items = await provider.Search(keyword);
            //    items = items.Take(10).AsParallel().Select(item => {
            //        var kpId = item.Id.Replace("kp", "");
            //        item = provider.Get(long.Parse(kpId)).Result;
            //        if (!string.IsNullOrEmpty(item.ImdbId)) {
            //            item.Id = item.ImdbId;
            //        }

            //        return item;
            //    }).ToArray();

            //    return new JsonResult(new { metas = items });
            //}

            return null;
        }
    }
}
