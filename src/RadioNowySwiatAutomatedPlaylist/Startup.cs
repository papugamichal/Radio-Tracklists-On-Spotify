using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RadioNowySwiatAutomatedPlaylist.HostedServices.KeepAlive;
using RadioNowySwiatAutomatedPlaylist.HostedServices.KeepAlive.Configuration;
using RadioNowySwiatAutomatedPlaylist.HostedServices.PlaylistUpdater;
using RadioNowySwiatAutomatedPlaylist.HostedServices.PlaylistUpdater.Configuration;
using RadioNowySwiatAutomatedPlaylist.HostedServices.PlaylistVisibilityLimiter;
using RadioNowySwiatAutomatedPlaylist.HostedServices.PlaylistVisibilityLimiter.Configuration;
using RadioNowySwiatAutomatedPlaylist.Services.DataSourceService;
using RadioNowySwiatAutomatedPlaylist.Services.DataSourceService.Abstraction;
using RadioNowySwiatAutomatedPlaylist.Services.DataSourceService.Configuration;
using RadioNowySwiatAutomatedPlaylist.Services.PlaylistManager;
using RadioNowySwiatAutomatedPlaylist.Services.PlaylistManager.Configuration;
using RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService;
using RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Abstraction;
using RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Configuration;
using RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Security;
using RadioNowySwiatAutomatedPlaylist.Services.TrackCache;

namespace RadioNowySwiatPlaylistBot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            services
                .AddDataProtection().Services
                //.Configure<DataSourceOptions>(config =>
                //    this.Configuration.GetSection(DataSourceOptions.SectionName).Bind(config))
                .Configure<SpotifyClientOptions>(config =>
                    this.Configuration.GetSection(SpotifyClientOptions.SectionName).Bind(config))
                .Configure<SpotifyAuthorizationServiceOptions>(config =>
                    this.Configuration.GetSection(SpotifyAuthorizationServiceOptions.SectionName).Bind(config))
                .Configure<PlaylistUpdaterOptions>(config =>
                    this.Configuration.GetSection(PlaylistUpdaterOptions.SectionName).Bind(config))
                .Configure<KeepAliveServiceOptions>(config =>
                    this.Configuration.GetSection(KeepAliveServiceOptions.SectionName).Bind(config))
                .Configure<PlaylistManagerOptions>(config =>
                    this.Configuration.GetSection(PlaylistManagerOptions.SectionName).Bind(config))
                .Configure<PlaylistVisibilityLimiterOptions>(config =>
                    this.Configuration.GetSection(PlaylistVisibilityLimiterOptions.SectionName).Bind(config))
                .AddScoped<IDataSourceService, DataSourceService>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<DataSourceService>>();
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    
                    var _options = new DataSourceOptions();
                    configuration.Bind(_options);
                    var options = Options.Create(_options);
                    return new DataSourceService(logger, options);
                })
                .AddScoped<IPlaylistManager, PlaylistManager>()
                .AddSingleton<ISpotifyClientService, SpotifyClientService>()
                .AddSingleton<ISpotifyAuthorizationService, SpotifyAuthorizationService>()
                .AddSingleton<FoundInSpotifyCache>()
                .AddSingleton<NotFoundInSpotifyCache>()
                .AddHostedService<PlaylistUpdaterHostedService>()
                .AddHostedService<KeepAliveHostedService>()
                .AddHostedService<PlaylistVisibilityLimiterHostedService>()
                ;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet(ApiPaths.Root, async context =>
                {
                    var spotifyClient = context.RequestServices.GetRequiredService<ISpotifyClientService>();

                    var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
                    var isAuth = spotifyClient.IsAuthenticated();

                    await context.Response.WriteAsync($"Service is working!\n" +
                        $"User is: {(isAuth ? "Authenticated!" : "Not authenticated!")}\n" +
                        $"Uptime: {uptime.Days} days {uptime.Hours} hours {uptime.Minutes} minutes {uptime.Seconds} seconds");
                });

                endpoints.MapGet(ApiPaths.Endpoints, async context =>
                {
                    var endpoints = typeof(ApiPaths).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    var result = JsonConvert.SerializeObject(endpoints.Select(e => (string)e.GetRawConstantValue()).ToList(), new JsonSerializerSettings() { Formatting = Formatting.Indented });
                    await context.Response.WriteAsync(result);
                });

                endpoints.MapGet(ApiPaths.IsAuthenticated, async context =>
                {
                    var client = context.RequestServices.GetRequiredService<ISpotifyClientService>();

                    if (client.IsAuthenticated())
                    {
                        await context.Response.WriteAsync("Authenticated!");
                        return;
                    }

                    await context.Response.WriteAsync("Not authenticated!");
                });

                endpoints.MapGet(ApiPaths.Auth, async context =>
                {
                    var client = context.RequestServices.GetRequiredService<ISpotifyClientService>();

                    if (client.IsAuthenticated())
                    {
                        await context.Response.WriteAsync("Already authenticated!");
                        return;
                    }
                        
                    context.Response.Redirect(client.GetAuthorizationUri().ToString(), permanent: false);
                    
                });

                endpoints.MapGet(ApiPaths.Callback, async context =>
                {
                    var client = context.RequestServices.GetRequiredService<ISpotifyClientService>();

                    if (client.IsAuthenticated())
                    {
                        await context.Response.WriteAsync("Already authenticated!");
                        return;
                    }

                    string code = context.Request.Query["code"];
                    if (string.IsNullOrEmpty(code) && context.Request.Query.ContainsKey("error"))
                    {
                        string error = context.Request.Query["error"];
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Not authenticated! Reason: " + error);
                        return;
                    }

                    else if (string.IsNullOrEmpty(code))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Not authenticated!");
                        return;
                    }

                    await client.SetupAccessToken(code);

                    context.Response.Redirect(ApiPaths.IsAuthenticated, permanent: true);
                });

                endpoints.MapGet(ApiPaths.Playlist, async context =>
                {
                    string dateFromQuery = context.Request.Query["date"];
                    string startDateFromQuery = context.Request.Query["startdate"];
                    string endDateFromQuery = context.Request.Query["enddate"];

                    if (string.IsNullOrEmpty(dateFromQuery) && string.IsNullOrEmpty(startDateFromQuery))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Not date provided");
                        return;
                    }

                    string result = string.Empty;
                    var dataSourceService = context.RequestServices.GetRequiredService<IDataSourceService>();

                    if (!string.IsNullOrEmpty(dateFromQuery))
                    {
                        var playlist = dataSourceService.GetPlaylistFor(DateTime.Parse(dateFromQuery));
                        context.Response.ContentType = "application/json";
                        result = JsonConvert.SerializeObject(playlist, new JsonSerializerSettings() { Formatting = Formatting.Indented });
                    }
                    else if (!string.IsNullOrEmpty(startDateFromQuery))
                    {
                        DateTime endDate;
                        if (!string.IsNullOrEmpty(endDateFromQuery))
                        {
                            endDate = DateTime.Parse(endDateFromQuery);
                        }
                        else
                        {
                            endDate = DateTime.Today;
                        }

                        var playlist = dataSourceService.GetPlaylistForRange(DateTime.Parse(startDateFromQuery), endDate);
                        context.Response.ContentType = "application/json";
                        result = JsonConvert.SerializeObject(playlist, new JsonSerializerSettings() { Formatting = Formatting.Indented });
                    }

                    await context.Response.WriteAsync(result);
                });

                endpoints.MapGet(ApiPaths.UserId, async context =>
                {
                    var spotifyClient = context.RequestServices.GetRequiredService<ISpotifyClientService>();
                    if (!spotifyClient.IsAuthenticated())
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Not authenticated!");
                        return;
                    }
                    var result = await spotifyClient.RequestForUserId();
                    await context.Response.WriteAsync(result);
                });

                endpoints.MapGet(ApiPaths.UserPlaylists, async context =>
                {
                    var spotifyClient = context.RequestServices.GetRequiredService<ISpotifyClientService>();
                    if (!spotifyClient.IsAuthenticated())
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Not authenticated!");
                        return;
                    }

                    var playlists = await spotifyClient.RequestForUserPlaylists();
                    context.Response.ContentType = "application/json";
                    var result = JsonConvert.SerializeObject(playlists, new JsonSerializerSettings() { Formatting = Formatting.Indented });
                    await context.Response.WriteAsync(result);
                });

                endpoints.MapGet(ApiPaths.CreatePlaylist, async context =>
                {
                    string name = context.Request.Query["name"];
                    if (string.IsNullOrEmpty(name))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Not name provided");
                        return;
                    }

                    var spotifyClient = context.RequestServices.GetRequiredService<ISpotifyClientService>();
                    if (!spotifyClient.IsAuthenticated())
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Not authenticated!");
                        return;
                    }

                    //await spotifyClient.CreatePlaylist(name);
                    await context.Response.WriteAsync("Done");
                });
            });
        }
    }

    public static class ApiPaths
    {
        public const string Root = "/";
        public const string Endpoints = "/api";
        public const string Auth = "/auth";
        public const string IsAuthenticated = "/isAuthenticated";
        public const string Callback = "/callback";
        public const string UserPlaylists = "/userplaylists";
        public const string UserId = "/userid";
        public const string Playlist = "/playlist/";
        public const string CreatePlaylist = "/createplaylist";
    }
}

