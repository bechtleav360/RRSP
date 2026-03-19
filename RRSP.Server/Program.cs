using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Signum.API;
using Signum.API.Filters;
using Signum.Authorization;
using Signum.Basics;
using Signum.ConcurrentUser;
using Signum.Mailing;
using Signum.Processes;
using Signum.Scheduler;
using Signum.Utilities;
using System.Globalization;
using System.IO;
using Schema = Signum.Engine.Maps.Schema;

namespace RRSP.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddUserSecrets<Program>();
        builder.Services.AddResponseCompression();
        builder.Services
            .AddMvc(options => options.AddSignumGlobalFilters())
            .AddApplicationPart(typeof(SignumServer).Assembly)
            .AddApplicationPart(typeof(AuthServer).Assembly)
            .AddJsonOptions(options => options.AddSignumJsonConverters());
        builder.Services.AddSignalR(a =>
        {
            a.AddFilter<LogHubExceptionFilter>();
        }).AddJsonProtocol(options => options.PayloadSerializerOptions.AddSignumJsonConverters());

        const int maxRequestLimit = 200 * 1024 * 1024;
        builder.Services.Configure<KestrelServerOptions>(options => { options.Limits.MaxRequestBodySize = maxRequestLimit; });
        builder.Services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = maxRequestLimit);
        builder.Services.AddSignumValidation();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("HealthCheck", builder =>
            {
                builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyHeader();
            });
        });

        //SwaggerConfig.ConfigureSwaggerService(builder); 

        var app = builder.Build();

        app.UseDeveloperExceptionPage();

        var pathBase = app.Configuration.GetValue<string>("PathBase");

        if (!string.IsNullOrEmpty(pathBase))
        {
            app.UsePathBase(pathBase);
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.Use(async (ctx, next) =>
            {
                if (!ctx.Request.PathBase.Equals(new PathString(pathBase)))
                {
                    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                    await ctx.Response.WriteAsync("Not Found");
                    return;
                }

                await next();
            });

        }

        app.UseStaticFiles();

        //HeavyProfiler.Enabled = true;
        using (HeavyProfiler.Log("Startup"))
        using (var log = HeavyProfiler.Log("Initial"))
        {
            Starter.NavbarColor = app.Configuration.GetValue<string>("NavbarColor");
            Starter.OverrideAutoExternal = app.Configuration.GetValue<string>("OverrideAutoExternal");
            Starter.Start(
                app.Configuration.GetConnectionString("ConnectionString")!,
                app.Configuration.GetValue<bool>("IsPostgres"),
                app.Configuration.GetConnectionString("AzureStorageConnectionString"),
                app.Configuration.GetValue<string>("BroadcastSecret"),
                app.Configuration.GetValue<string>("BroadcastUrls"),
                wsb: new WebServerBuilder
                {
                    WebApplication = app,
                    AuthTokenEncryptionKey = app.Configuration.GetValue<string>("AuthTokenEncryptionKey")!,
                    MachineName = app.Configuration.GetValue<string?>("ServerName"),
                    DefaultCulture = CultureInfo.GetCultureInfo("en")
                },
                filesRoot: app.Configuration.GetValue<string>("FilesRoot")
                );

            Statics.SessionFactory = new ScopeSessionFactory(new VoidSessionFactory());


            log.Switch("UseEndpoints");

            app.UseWhen(req => req.Request.Path.StartsWithSegments("/api/reflection/types"), builder =>
            {
                builder.UseResponseCompression();
            });

            //WhatsNew.Start(app);

            app.UseRouting();
            app.UseCors("HealthCheck");
            app.UseAuthorization();

            app.MapControllers();
            app.MapWhen(ctx =>
                !ctx.Request.Path.StartsWithSegments("/api") &&
                !ctx.Request.Path.StartsWithSegments("/dist") &&
                !Path.HasExtension(ctx.Request.Path.Value),
                appInner =>
                {
                    appInner.UseRouting();
                    appInner.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllerRoute(
                            name: "spa-fallback",
                            pattern: "{*url}",
                            defaults: new { controller = "Home", action = "Index" });
                    });
                });
        }

        SignumInitializeFilterAttribute.InitializeDatabase = () =>
        {
            using (HeavyProfiler.Log("Startup"))
            using (var log = HeavyProfiler.Log("Initial"))
            {
                log.Switch("Initialize");
                using (AuthLogic.Disable())
                    Schema.Current.Initialize();

                if (app.Configuration.GetValue<bool>("StartBackgroundProcesses"))
                {
                    log.Switch("StartRunningProcesses");
                    ProcessRunner.StartRunningProcessesAfter(5 * 1000);

                    log.Switch("StartScheduledTasks");
                    ScheduleTaskRunner.StartScheduledTaskAfter(5 * 1000);

                    log.Switch("StartRunningEmailSenderAsync");
                    AsyncEmailSender.StartAsyncEmailSenderAfter(5 * 1000);
                }

                SystemEventServer.LogStartStop(app.Lifetime);
            }
        };
        app.Run();
    }

    class NoAPIContraint : IRouteConstraint
    {
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            var url = (string?)values[routeKey];

            if (url != null && url.StartsWith("api/"))
                return false;

            return true;
        }
    }

}
