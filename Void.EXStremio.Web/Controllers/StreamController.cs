using System.Buffers.Text;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AngleSharp.Io;
using Microsoft.AspNetCore.Mvc;
using Void.EXStremio.Web.Providers.Metadata;
using Void.EXStremio.Web.Providers.Stream;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class StreamController : Controller {
        readonly IHttpClientFactory httpClientFactory;
        public StreamController(IHttpClientFactory httpClientFactory) {
            this.httpClientFactory = httpClientFactory;
        }

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
                var encodedUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(stream.Url));
                stream.Url = UrlBuilder.AbsoluteUrl(Request, "/stream/play/" + encodedUrl).ToString();
            }

            return new JsonResult(new {
                streams = streams
            });
        }

        [HttpGet("/stream/play/{encodedUrl}")]
        [HttpHead("/stream/play/{encodedUrl}")]
        public async Task<FileStreamResult> Play(string encodedUrl) {
            var isRangeRequest = Request.Headers.ContainsKey("Range");

            var url = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUrl));
            var client = httpClientFactory.CreateClient();

            var message = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
            if (isRangeRequest) {
                message.Headers.Range = new RangeHeaderValue();
                foreach (var rangeString in Request.Headers.Range) {
                    var range = rangeString.Replace("bytes=", "").Split('-', StringSplitOptions.RemoveEmptyEntries);
                    message.Headers.Range.Ranges.Add(new RangeItemHeaderValue(long.Parse(range[0]), range.Length > 1 ? long.Parse(range[1]) : null));
                }
            }

            var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
            var stream = await response.Content.ReadAsStreamAsync();

            var result = File(stream, response.Content.Headers.ContentType?.ToString(), true);
            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.Headers.Append("Content-Length", response.Content.Headers.ContentLength?.ToString());
            if (isRangeRequest) {
                Response.Headers.Append("Content-Range", response.Content.Headers.ContentRange?.ToString());
                Response.StatusCode = (int)HttpStatusCode.PartialContent;
            }

            return result;
        }
    }
}
