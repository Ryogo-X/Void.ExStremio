using Microsoft.AspNetCore.Mvc;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Providers;

namespace Void.EXStremio.Web.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class MetaController : Controller {
        // GET /stream/movie/tt0032138
        [HttpGet("{type}/{id}")]
        public async Task<JsonResult> Get(string type, string id) {
            return new JsonResult(new { meta = new Meta() {
                Name = "TEST",
                ImdbId = id.Replace(".json", ""),
                Id = "111",
                Type = type,
                Links = new [] {
                    new Link() {
                        Name = "TEST",
                        Category = type,
                        Url = "http://localhost:5000"
                    }
                }
            }
            });
            id = id.Replace("kp", "").Replace(".json", "");
            var provider = new KinopoiskProvider();
            var item = await provider.Get(long.Parse(id));

            return new JsonResult(new { meta = item });
        }
    }
}
