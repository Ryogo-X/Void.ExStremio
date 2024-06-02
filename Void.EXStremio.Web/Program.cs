
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Reflection;
using Void.EXStremio.Web.Models;
using Void.EXStremio.Web.Providers.Catalog;
using Void.EXStremio.Web.Providers.Media.AllohaTv;
using Void.EXStremio.Web.Providers.Media.Collaps;
using Void.EXStremio.Web.Providers.Media.Hdvb;
using Void.EXStremio.Web.Providers.Media.Kodik;
using Void.EXStremio.Web.Providers.Media.VideoCdn;
using Void.EXStremio.Web.Providers.Metadata;
using Void.EXStremio.Web.Utility;

namespace Void.EXStremio.Web {
    public class Program {
        public static void Main(string[] args) {
            AppContext.SetSwitch("Switch.Microsoft.AspNetCore.Mvc.EnableRangeProcessing", true);

            var builder = WebApplication.CreateBuilder();

            builder.Services.AddHttpClient();

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // Add services to the container.r
            var mvcBuilder = builder.Services.AddControllers();
            mvcBuilder.AddJsonOptions(options => {
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });
            mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(Assembly.GetExecutingAssembly()));

            builder.Services.AddCors(options => {
                options.AddDefaultPolicy(
                    builder => {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });

            builder.WebHost.ConfigureKestrel((context, serverOptions) => {
                serverOptions.Listen(IPAddress.Any, port: 5000);
                //serverOptions.Listen(IPAddress.Loopback, port: 5001, opts => opts.UseHttps());
            });

            RegisterProviders(builder.Services);

            var app = builder.Build();

            app.UseExceptionHandler(exceptionHandlerApp => {
                exceptionHandlerApp.Run(async context => {
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();

                    var error = exceptionHandlerPathFeature?.Error;
                    if (error != null) {
                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")}: [ERROR]\n{error.Message}\n{error.StackTrace}");
                    }
                });
            });

            // Configure the HTTP request pipeline.

            //app.UseHttpsRedirection();

            //app.UseAuthorization();

            app.MapControllers();

            app.MapGet("/routes", (IEnumerable<EndpointDataSource> endpointSources) => string.Join("\n", endpointSources.SelectMany(source => source.Endpoints)));

            app.UseCors(config => {
                config.AllowAnyOrigin();
                config.AllowAnyMethod();
            });

            app.Run();
        }

        static void RegisterProviders(IServiceCollection serviceCollection) {
            var logger = serviceCollection.BuildServiceProvider().GetService<ILogger<Program>>();

            // ---
            serviceCollection.AddSingleton<IMemoryCache, MemoryCache>(x => new MemoryCache(new MemoryCacheOptions()));

            // IMetadataProvider
            serviceCollection.AddSingleton<IMetadataProvider, CinemetaMetaProvider>();

            // IAdditionalMetadataProvider
            var tmdbApiKey = Environment.GetEnvironmentVariable(TmdbConfig.CONFIG_API_KEY);
            if (!string.IsNullOrEmpty(tmdbApiKey)) {
                serviceCollection.AddSingleton(_ => new TmdbConfig(tmdbApiKey));
                serviceCollection.AddSingleton<IAdditionalMetadataProvider, TmdbMetaProvider>();
            } else {
                logger?.LogWarning("[INIT] TMDB provider not initialized - api key is missing.");
            }

            serviceCollection.AddSingleton<IAdditionalMetadataProvider, ImdbMetaProvider>();
            serviceCollection.AddHttpClient(ImdbMetaProvider.HTTP_CLIENT_KEY, httpClient => {
                httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.5");
            });

            // ICatalogProvider
            serviceCollection.AddSingleton<ICatalogProvider, KinopoiskCatalogProvider>();
            serviceCollection.AddHttpClient(KinopoiskCatalogProvider.HTTP_CLIENT_KEY, httpClient => {
                httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Kinopoisk/1.0.0");
                httpClient.DefaultRequestHeaders.Add("Service-Id", "76");
            });

            // IMediaProvider / IKinopoiskIdProvider
            var kodikApiKey = Environment.GetEnvironmentVariable(KodikConfig.CONFIG_API_KEY);
            if (!string.IsNullOrEmpty(kodikApiKey)) {
                serviceCollection.AddSingleton(_ => new KodikConfig(kodikApiKey));
                serviceCollection.AddSingleton<IKinopoiskIdProvider, KodikCdnProvider>();
                serviceCollection.AddSingleton<IMediaProvider, KodikCdnProvider>();
                serviceCollection.AddHttpClient(nameof(KodikCdnProvider), httpClient => {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/113.0");
                    httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.5");
                    httpClient.DefaultRequestHeaders.Referrer = new Uri("https://kinomix.web.app/");
                });
            } else {
                logger?.LogWarning("[INIT] KODIK provider not initialized - api key is missing.");
            }

            var videoCdnApiKey = Environment.GetEnvironmentVariable(VideoCdnConfig.CONFIG_API_KEY);
            if (!string.IsNullOrEmpty(videoCdnApiKey)) {
                serviceCollection.AddSingleton(_ => new VideoCdnConfig(videoCdnApiKey));
                serviceCollection.AddSingleton<IKinopoiskIdProvider, VideoCdnProvider>();
                serviceCollection.AddSingleton<IMediaProvider, VideoCdnProvider>();
            } else {
                logger?.LogWarning("[INIT] VideoCDN provider not initialized - api key is missing.");
            }

            var allohaTvApiKey = Environment.GetEnvironmentVariable(AllohaTvConfig.CONFIG_API_KEY);
            if (!string.IsNullOrEmpty(allohaTvApiKey)) {
                serviceCollection.AddSingleton(_ => new AllohaTvConfig(allohaTvApiKey));
                serviceCollection.AddSingleton<IKinopoiskIdProvider, AllohaTvCdnProvider>();
            } else {
                logger?.LogWarning("[INIT] AllohaTv provider not initialized - api key is missing.");
            }

            var collapsApiKey = Environment.GetEnvironmentVariable(CollapsConfig.CONFIG_API_KEY);
            if (!string.IsNullOrEmpty(collapsApiKey)) {
                serviceCollection.AddSingleton(_ => new CollapsConfig(collapsApiKey));
                serviceCollection.AddSingleton<IMediaProvider, CollapsCdnProvider>();
                serviceCollection.AddHttpClient(nameof(CollapsCdnProvider), httpClient => {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/113.0");
                    httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.5");
                });
            } else {
                logger?.LogWarning("[INIT] Collaps provider not initialized - api key is missing.");
            }

            var hdvbApiKey = Environment.GetEnvironmentVariable(HdvbConfig.CONFIG_API_KEY);
            if (!string.IsNullOrEmpty(collapsApiKey)) {
                serviceCollection.AddSingleton(_ => new HdvbConfig(hdvbApiKey));
                serviceCollection.AddSingleton<IMediaProvider, HdvbCdnProvider>();
                serviceCollection.AddHttpClient(nameof(HdvbCdnProvider), httpClient => {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/113.0");
                    httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.5");
                    httpClient.DefaultRequestHeaders.Referrer = new Uri("https://kinomix.web.app/");
                    // http://flicksbar.mom/
                });
            } else {
                logger?.LogWarning("[INIT] HDVB provider not initialized - api key is missing.");
            }
        }
    }
}
