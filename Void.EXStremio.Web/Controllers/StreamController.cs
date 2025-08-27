using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
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

        readonly string CACHE_KEY_ID_MAPPING;
        readonly string CACHE_KEY_IMDB_META;
        readonly string CACHE_KEY_IMDB_META_EXT;

        public StreamController(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<StreamController> logger) {
            this.httpClientFactory = httpClientFactory;
            this.cache = cache;
            this.logger = logger;

            CACHE_KEY_ID_MAPPING = $"MAPPINGS:[type]:[id]";
            CACHE_KEY_IMDB_META = $"META:IMDB:[imdb]";
            CACHE_KEY_IMDB_META_EXT = $"META:IMDB:EXT:[imdb]";
        }

        // TODO: enrich meta with data from Cinemeta / TMDB / IMBD
        //       after each enrich try to match with KP
        // GET /stream/movie/tt0032138
        [HttpGet("/stream/{type}/{sourceId}")]
        public async Task<JsonResult> GetAsync(string type, string sourceId) {
            var mProviders = HttpContext.RequestServices
                .GetServices<IMediaProvider>()
                .Where(x => x is IInitializableProvider)
                .Select(x => (IInitializableProvider)x)
                .Where(x => !x.IsInitialized);
            await Parallel.ForEachAsync(mProviders, async (x, _) => {
                try {
                    await x.Initialize();
                } catch (Exception ex) {
                    logger?.LogError(ex, $"[{nameof(StreamController)}] -> Failed to initialize provider {x.GetType().Name}");
                }
            });

            var parts = sourceId.Replace(".json", "").Split(':');

            sourceId = parts[0];
            int? season = null;
            int? episode = null;
            if (parts.Length > 1) {
                season = int.Parse(parts[1]);
                episode = int.Parse(parts[2]);
            }
            var ids = new List<string> { sourceId };

            ExtendedMeta meta = null;
            if (sourceId.StartsWith("tt")) {
                var imdbId = sourceId;

                // get IMDB meta
                {
                    var ckMeta = CACHE_KEY_IMDB_META.Replace("[imdb]", imdbId);
                    meta = cache.Get<ExtendedMeta>(ckMeta);
                    if (meta == null) {
                        var metaProvider = HttpContext.RequestServices.GetService<IMetadataProvider>();
                        meta = await metaProvider.GetMetadataAsync(type, imdbId);

                        if (meta != null) {
                            cache.Set(ckMeta, meta);
                        } else {
                            logger.LogError($"Cannot retrieve metadata for (id: {imdbId}, type: {type})");
                        }  
                    }
                }

                // get KP id
                {
                    var ckMapping = CACHE_KEY_ID_MAPPING
                        .Replace("[type]", "imdb")
                        .Replace("[id]", imdbId);
                    var kpId = cache.Get<string>(ckMapping);
                    if (string.IsNullOrWhiteSpace(kpId)) {
                        var providers = HttpContext.RequestServices.GetServices<IKinopoiskIdProvider>();
                        foreach (var provider in providers) {
                            try {
                                kpId = await provider.GetKinopoiskIdAsync(imdbId);
                            } catch (Exception ex) {
                                logger.LogError($"{provider.GetType().Name} error retrieving KP id.\n{ex}");
                            }

                            if (!string.IsNullOrWhiteSpace(kpId)) { break; }
                        }

                        if (!string.IsNullOrWhiteSpace(kpId)) {
                            cache.Set(ckMapping, kpId);
                            ids.Add(kpId);
                        } else {
                            logger.LogWarning($"[STREAM] Not able to retrieve KP id for (type: {type}, id: {imdbId})");
                        }
                    } else {
                        ids.Add(kpId);
                    }
                }

                // get extended metadata
                {
                    var ckMetaExt = CACHE_KEY_IMDB_META_EXT.Replace("[imdb]", imdbId);
                    meta = cache.Get<ExtendedMeta>(ckMetaExt);
                    if (meta == null) {
                        var ckMeta = CACHE_KEY_IMDB_META.Replace("[imdb]", imdbId);
                        meta = cache.Get<ExtendedMeta>(ckMeta);
                        if (meta != null) {
                            var providers = HttpContext.RequestServices.GetServices<IAdditionalMetadataProvider>();
                            await Parallel.ForEachAsync(providers, async (provider, token) => {
                                foreach (var id in ids) {
                                    try {
                                        if (!provider.CanGetAdditionalMetadata(id)) { continue; }

                                        var extMeta = await provider.GetAdditionalMetadataAsync(type, id);
                                        lock (meta) {
                                            meta.Extend(extMeta);
                                        }
                                    } catch (Exception ex) {
                                        logger.LogError($"{provider.GetType().Name} error retrieving extended meta for id: {id}.\n{ex}");
                                    }
                                }
                            });

                            cache.Set(ckMetaExt, meta);
                        }
                    }
                }

                // get custom ids
                {
                    var ckMetaExt = CACHE_KEY_IMDB_META_EXT.Replace("[imdb]", imdbId);
                    meta = cache.Get<ExtendedMeta>(ckMetaExt);
                    if (meta != null) {
                        var providers = HttpContext.RequestServices.GetServices<ICustomIdProvider>();
                        await Parallel.ForEachAsync(providers, async (provider, token) => {
                            var ckMapping = CACHE_KEY_ID_MAPPING
                                .Replace("[type]", provider.GetType().Name)
                                .Replace("[id]", imdbId);
                            var customIds = cache.Get<CustomIdResult[]>(ckMapping);
                            if (customIds == null) {
                                try {
                                    customIds = await provider.GetCustomIds(meta);
                                } catch (Exception ex) {
                                    logger.LogError($"{provider.GetType().Name} error retrieving custom ids.\n{ex}");
                                }
                                customIds ??= [];

                                if (customIds?.FirstOrDefault()?.Expiration.HasValue != true) {
                                    cache.Set(ckMapping, customIds, DateTimeOffset.Now.AddHours(8));
                                } else {
                                    cache.Set(ckMapping, customIds, customIds.First().Expiration.Value);
                                }
                            }

                            lock (ids) {
                                ids.AddRange(customIds.Select(x => x.Id));
                            }
                        });
                    }
                }
            }
            if (meta != null) {
                meta.KpId = ids.FirstOrDefault(x => x.StartsWith("kp"));
            }

            var streams = new List<MediaStream>();
            var streamProviders = HttpContext.RequestServices.GetServices<IMediaProvider>();
            await Parallel.ForEachAsync(streamProviders, async (streamProvider, token) => {
                if (streamProvider is IInitializableProvider initializableProvider && !initializableProvider.IsReady) { return; }

                foreach(var id in ids) {
                    if (!streamProvider.CanGetStreams(id)) { continue; }

                    try {
                        var newStreams = await streamProvider.GetStreams(id, season, episode, meta);
                        if (!newStreams.Any()) { continue; }

                        lock (streams) {
                            foreach(var newStream in newStreams) {
                                var stream = streams.FirstOrDefault(x => x.CdnName == newStream.CdnName && x.Name == newStream.Name && x.Title == newStream.Title);
                                if (stream != null) { continue; }

                                streams.Add(newStream);
                            }
                        }

                        break;
                    } catch (Exception ex) {
                        logger.LogError(ex, $"{streamProvider.GetType().Name} error retrieving streams for id: {id}.\n{ex}");
                    }
                }
            });

            var hasHdStreams = streams.Any(stream => {
                var qualityString = Regex.Match(stream.Name, "(?<quality>[0-9]+)p").Groups["quality"].Value;
                if (!int.TryParse(qualityString, out var quality)) { return true; }

                return quality > 640;
            });
            if (hasHdStreams) {
                streams.RemoveAll(stream => {
                    var qualityString = Regex.Match(stream.Name, "(?<quality>[0-9]+)p").Groups["quality"].Value;
                    if (!int.TryParse(qualityString, out var quality)) { return false; }

                    return quality < 640;
                });
            }
            var duplicateStreams = new List<MediaStream>();
            var streamGroups = streams.GroupBy(x => x.CdnName).Where(x => !string.IsNullOrWhiteSpace(x.Key));
            foreach (var streamGroup in streamGroups) {
                var providerNames = streamGroup.Select(x => x.ProviderName).Distinct();
                foreach(var providerName in providerNames.Skip(1)) {
                    streams.RemoveAll(stream => stream.CdnName == streamGroup.Key && stream.ProviderName == providerName);
                }
            }
            
            // transform relative urls to absolute urls
            foreach (var stream in streams) {
                if (stream.Url.StartsWith("http")) { continue; }

                stream.Url = UrlBuilder.AbsoluteUrl(Request, stream.Url).ToString();
            }

            // add binge groups
            foreach (var stream in streams) {
                stream.BehaviorHints = new BehaviorHints() {
                    BingeGroup = $"exstremio | {stream.GetCdnSource()} | {stream.GetQuality()} | {stream.GetTranslation()}"
                };
            }
            streams.Sort((x, y) => { 
                if (x.CdnName != y.CdnName) {
                    return x.CdnName?.First() > y.CdnName?.First() ? 1 : -1;
                }

                var xQuality = x.GetQuality();
                var yQuality = y.GetQuality();
                if (xQuality == yQuality) { return 0; }

                return xQuality > yQuality ? -1 : 1;
            });

            return new JsonResult(new {
                streams = streams
            });
        }

        [HttpGet("/stream/play/{encodedUrl}")]
        [HttpHead("/stream/play/{encodedUrl}")]
        public async Task<FileResult> Play(string encodedUrl, [FromQuery]string source, [FromQuery]MediaFormatType format, [FromQuery]string quality) {
            var uriString = Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(encodedUrl)));
            var mediaLink = new MediaLink(new Uri(uriString), source, format, quality);

            var mediaProviders = HttpContext.RequestServices.GetServices<IMediaProvider>();
            var mediaProvider = mediaProviders.FirstOrDefault(x => x.CanGetMedia(mediaLink));
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
        }
    }
}
