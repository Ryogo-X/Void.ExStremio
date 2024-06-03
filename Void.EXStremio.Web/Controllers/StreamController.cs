using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AngleSharp.Io;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class StreamController : Controller {
        readonly IHttpClientFactory httpClientFactory;
        readonly IMemoryCache cache;
        readonly ILogger<StreamController> logger;

        readonly string CACHE_KEY_MAPPING_IMDB_KP;

        public StreamController(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<StreamController> logger) {
            this.httpClientFactory = httpClientFactory;
            this.cache = cache;
            this.logger = logger;

            CACHE_KEY_MAPPING_IMDB_KP = $"MAPPINGS:IMDB:[imdb]";
        }

        // TODO: enrich meta with data from Cinemeta / TMDB / IMBD
        //       after each enrich try to match with KP
        // GET /stream/movie/tt0032138
        [HttpGet("/stream/{type}/{id}")]
        public async Task<JsonResult> GetAsync(string type, string id) {
            var parts = id.Replace(".json", "").Split(':');

            id = parts[0];
            int? season = null;
            int? episode = null;
            if (parts.Length > 1) {
                season = int.Parse(parts[1]);
                episode = int.Parse(parts[2]);
            }

            string kpId = null;
            if (id.StartsWith("tt")) {
                var ckMapping = CACHE_KEY_MAPPING_IMDB_KP
                    .Replace("[imdb]", id);

                kpId = cache.Get<string>(ckMapping);
                if (string.IsNullOrWhiteSpace(kpId)) {
                    var kpIdProviders = HttpContext.RequestServices.GetServices<IKinopoiskIdProvider>();
                    foreach (var kpIdProvider in kpIdProviders) {
                        try {
                            kpId = await kpIdProvider.GetKinopoiskIdAsync(id);
                        } catch {
                            // TODO: logging?
                        }

                        if (!string.IsNullOrWhiteSpace(kpId)) { break; }
                    }
                    if (string.IsNullOrWhiteSpace(kpId)) {
                        return new JsonResult(new {
                            error = $"[STREAM] Not able to retrieve KP id for (type: {type}, id: {id})"
                        });
                    }

                    cache.Set(ckMapping, kpId);
                }
            } else if (id.StartsWith("kp")) {
                kpId = id;
            } else {
                return new JsonResult(new { 
                    error = $"Identifier '{id}' not supported."
                });
            }

            var streams = new List<MediaStream>();
            var streamProviders = HttpContext.RequestServices.GetServices<IMediaProvider>();
            await Parallel.ForEachAsync(streamProviders, async (streamProvider, token) => {
                if (!streamProvider.CanHandle(kpId)) { return; }

                try {
                    var newStreams = await streamProvider.GetStreams(kpId, season, episode);
                    if (!newStreams.Any()) {
                        if (!streamProvider.CanHandle(id)) { return; }
                        newStreams = await streamProvider.GetStreams(id, season, episode);
                    }

                    lock (streams) {
                        streams.AddRange(newStreams);
                    }
                } catch {
                    // TODO: logging?
                }
            });

            // transform relative urls to absolute urls
            foreach (var stream in streams) {
                if (stream.Url.StartsWith("http")) { continue; }

                stream.Url = UrlBuilder.AbsoluteUrl(Request, stream.Url).ToString();
            }

            return new JsonResult(new {
                streams = streams
            });

            //var catalogProvider = new KinopoiskCatalogProvider(httpClientFactory);
            //var catalogResponse = await catalogProvider.GetAsync(type, meta.OriginalName ?? meta.Name);

            //var kpMeta = catalogResponse
            //    .Metas
            //    .FirstOrDefault(x => {
            //        return x.IsMatch(meta);
            //    });

            //if (kpMeta == null && meta.TmdbId > 0) {
            //    var tmdbMetaProvider = new TmdbMetaProvider(httpClientFactory);
            //    var tmdbMeta = await tmdbMetaProvider.GetAsync(type, "tmdb" + meta.TmdbId.ToString());

            //    kpMeta = catalogResponse
            //        .Metas
            //        .FirstOrDefault(x => {
            //            return x.IsMatch(tmdbMeta);
            //        });
            //}

            //if (kpMeta == null) {
            //    var imdbProvider = new ImdbMetaProvider(httpClientFactory);
            //    var imdbMeta = await imdbProvider.GetAsync(type, id);

            //    kpMeta = catalogResponse
            //        .Metas
            //        .FirstOrDefault(x => {
            //            return x.IsMatch(imdbMeta);
            //        });
            //}

            //if (kpMeta == null) {
            //    throw new InvalidOperationException($"Cannot find Kinopoisk ID for '{id}'");
            //}

            //var fbMediaProvider = new FlicksbarMediaProvider();
            //var streams = await fbMediaProvider.GetStreams(kpMeta.KpId, season, episode);
            //streams.ToString();

            //foreach (var stream in streams) {
            //    if (stream.Url.StartsWith("http")) { continue; }

            //    stream.Url = UrlBuilder.AbsoluteUrl(Request, stream.Url).ToString();
            //}

            //return new JsonResult(new {
            //    streams = streams
            //});

            //id = id.Replace(".json", "");
            //var parts = id.Split(':', StringSplitOptions.RemoveEmptyEntries);
            //id = parts[0];
            //var season = parts.Length > 1 ? int.Parse(parts[1]) : (int?)null;
            //var episode = parts.Length > 1 ? int.Parse(parts[2]) : (int?)null;

            //var metaProvider = new ImdbMetadataProvider();
            //var meta = await metaProvider.Get(id);
            //meta.Type = type;

            //var streamProvider = new HdRezkaStreamProvider();
            //var streams = await streamProvider.Get(meta, season, episode);

            //foreach(var stream in streams) {
            //    var encodedUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(stream.Url));
            //    stream.Url = UrlBuilder.AbsoluteUrl(Request, "/stream/play/" + encodedUrl).ToString();
            //}
        }

        [HttpGet("/stream/play/{encodedUrl}")]
        [HttpHead("/stream/play/{encodedUrl}")]
        public async Task<FileResult> Play(string encodedUrl, [FromQuery]string source, [FromQuery]MediaFormatType format, [FromQuery]string quality) {
            var uriString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUrl));
            var mediaLink = new MediaLink(new Uri(uriString), source, format, quality);

            var mediaProviders = HttpContext.RequestServices.GetServices<IMediaProvider>();
            var mediaProvider = mediaProviders.FirstOrDefault(x => x.CanHandle(mediaLink));
            if (mediaProvider == null) {
                logger?.LogWarning($"No provider found to handle the request: (source: {source}, format: {format}, quality: {quality})");
                throw new InvalidDataException("No provider found to handle the request.");
            }

            RangeHeaderValue rangeHeader = null;
            if (Request.Headers.ContainsKey("Range")) {
                rangeHeader = new RangeHeaderValue();
                foreach (var rangeString in Request.Headers.Range) {
                    var range = rangeString.Replace("bytes=", "").Split('-', StringSplitOptions.RemoveEmptyEntries);
                    rangeHeader.Ranges.Add(new RangeItemHeaderValue(long.Parse(range[0]), range.Length > 1 ? long.Parse(range[1]) : null));
                }
            }

            var media = await mediaProvider.GetMedia(mediaLink, rangeHeader);

            FileResult file = null;
            if (media is StreamMediaSource streamMedia) {
                file = File(streamMedia.Stream, streamMedia.ContentType, true);

                if (!string.IsNullOrWhiteSpace(streamMedia.AcceptRanges)) {
                    Response.Headers.Append("Accept-Ranges", streamMedia.AcceptRanges);
                }
                if (!string.IsNullOrWhiteSpace(streamMedia.ContentRange)) {
                    Response.Headers.Append("Content-Range", streamMedia.ContentRange);
                    Response.StatusCode = (int)HttpStatusCode.PartialContent;
                }
                Response.Headers.Append("Content-Length", streamMedia.ContentLength.ToString());
            } else if (media is PlaylistMediaSource playlistMedia) {
                file = File(playlistMedia.Content, playlistMedia.ContentType);
            } else {
                throw new InvalidDataException("Uknown media type.");
            }

            return file;

            //var isRangeRequest = Request.Headers.ContainsKey("Range");

            //if (cdn == MediaCdnType.Collapse) {
            //    if (format == MediaFormatType.DASH) {
            //        var provider = (CollapsCdnProvider)HttpContext.RequestServices.GetServices<IMediaProvider>().FirstOrDefault(x => x.GetType() == typeof(CollapsCdnProvider));
            //        if (provider == null) { throw new InvalidOperationException("Collaps provider not initialized"); }

            //        var xml = await provider.GetDashXml(new Uri(url), quality);

            //        var bytes = Encoding.UTF8.GetBytes(xml);
            //        return File(bytes, "application/dash+xml");
            //    }
            //}
            //var client = httpClientFactory.CreateClient();

            //var message = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
            //if (isRangeRequest) {
            //    message.Headers.Range = new RangeHeaderValue();
            //    foreach (var rangeString in Request.Headers.Range) {
            //        var range = rangeString.Replace("bytes=", "").Split('-', StringSplitOptions.RemoveEmptyEntries);
            //        message.Headers.Range.Ranges.Add(new RangeItemHeaderValue(long.Parse(range[0]), range.Length > 1 ? long.Parse(range[1]) : null));
            //    }
            //}

            //var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
            //var stream = await response.Content.ReadAsStreamAsync();

            //var result = File(stream, response.Content.Headers.ContentType?.ToString(), true);
            //Response.Headers.Append("Accept-Ranges", "bytes");
            //Response.Headers.Append("Content-Length", response.Content.Headers.ContentLength?.ToString());
            //if (isRangeRequest) {
            //    Response.Headers.Append("Content-Range", response.Content.Headers.ContentRange?.ToString());
            //    Response.StatusCode = (int)HttpStatusCode.PartialContent;
            //}

            //return result;
        }
    }
}
