
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Net;
using System.Reflection;

namespace Void.EXStremio.Web {
    public class Program
    {
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("Switch.Microsoft.AspNetCore.Mvc.EnableRangeProcessing", true);

            var builder = WebApplication.CreateBuilder();

            builder.Services.AddHttpClient();
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

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            //app.UseHttpsRedirection();

            //app.UseAuthorization();

            app.MapControllers();


            app.MapGet("/routes", (IEnumerable<EndpointDataSource> endpointSources) => string.Join("\n", endpointSources.SelectMany(source => source.Endpoints)));

            app.UseCors(config => {
                config.AllowAnyOrigin();
                config.AllowAnyMethod();
            });

            app.Use((ctx, next) => next(ctx));

            app.Run();
        }
    }
}
