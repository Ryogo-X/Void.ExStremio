﻿using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AngleSharp.Io;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Providers.Metadata;
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
            var parts = sourceId.Replace(".json", "").Split(':');

            sourceId = parts[0];
            int? season = null;
            int? episode = null;
            if (parts.Length > 1) {
                season = int.Parse(parts[1]);
                episode = int.Parse(parts[2]);
            }
            var ids = new List<string> { sourceId };

            if (sourceId.StartsWith("tt")) {
                var imdbId = sourceId;
                ExtendedMeta meta = null;

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
                            var id = cache.Get<string>(ckMapping);
                            if (string.IsNullOrWhiteSpace(id)) {
                                var customId = await provider.GetCustomId(meta);
                                if (customId == null) { return; }

                                if (!customId.Expiration.HasValue) {
                                    cache.Set(ckMapping, customId.Id);
                                } else {
                                    cache.Set(ckMapping, customId.Id, customId.Expiration.Value);
                                }
                                lock (ids) {
                                    ids.Add(customId.Id);
                                }
                            } else {
                                lock (ids) {
                                    ids.Add(id);
                                }
                            }
                        });
                    }
                }
            }

            var streams = new List<MediaStream>();
            var streamProviders = HttpContext.RequestServices.GetServices<IMediaProvider>();
            await Parallel.ForEachAsync(streamProviders, async (streamProvider, token) => {
                foreach(var id in ids) {
                    if (!streamProvider.CanGetStreams(id)) { continue; }

                    try {
                        var newStreams = await streamProvider.GetStreams(id, season, episode);
                        if (!newStreams.Any()) { continue; }

                        lock (streams) {
                            streams.AddRange(newStreams);
                        }

                        break;
                    } catch (Exception ex) {
                        logger.LogError($"{streamProvider.GetType().Name} error retrieving streams for id: {id}.\n{ex}");
                        // TODO: logging?
                    }
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
