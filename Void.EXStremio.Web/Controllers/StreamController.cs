using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Void.EXStremio.Web.Providers.Metadata;
using Void.EXStremio.Web.Providers.Stream;

namespace Void.EXStremio.Web.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class StreamController : Controller {
        // GET /stream/movie/tt0032138
        [HttpGet("/stream/{type}/{id}")]
        public async Task<JsonResult> Get(string type, string id) {
            id = id.Replace(".json", "");
            var parts = id.Split(':', StringSplitOptions.RemoveEmptyEntries);
            id = parts[0];
            var season = parts.Length > 1 ? int.Parse(parts[1]) : (int?)null;
            var episode = parts.Length > 1 ? int.Parse(parts[2]) : (int?)null;

            var metaProvider = new ImdbMetadataProvider();
            var meta = await metaProvider.Get(id);
            meta.Type = type;

            var streamProvider = new HdRezkaStreamProvider();
            var streams = await streamProvider.Get(meta, season, episode);

            foreach(var stream in streams) {
                Debug.WriteLine($"{stream.Name}: {stream.Url}");
            }

            return new JsonResult(new {
                streams = streams
            });
        }
    }
}
