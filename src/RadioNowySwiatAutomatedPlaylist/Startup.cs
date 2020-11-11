using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RadioNowySwiatPlaylistBot.Services;
using RadioNowySwiatPlaylistBot.Services.DailyPlaylistHostedService;
using RadioNowySwiatPlaylistBot.Services.DailyPlaylistHostedService.Configuration;
using RadioNowySwiatPlaylistBot.Services.DataSourceService;
using RadioNowySwiatPlaylistBot.Services.DataSourceService.Abstraction;
using RadioNowySwiatPlaylistBot.Services.DataSourceService.Configuration;
using RadioNowySwiatPlaylistBot.Services.PlaylistManager;
using RadioNowySwiatPlaylistBot.Services.PlaylistManager.Configuration;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.Abstraction;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.Configuration;
using RadioNowySwiatPlaylistBot.Services.TrackCache;

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
                .Configure<DataSourceOptions>(config =>
                    this.Configuration.GetSection(DataSourceOptions.SectionName).Bind(config))
                .Configure<SpotifyClientOptions>(config =>
                    this.Configuration.GetSection(SpotifyClientOptions.SectionName).Bind(config))
                .Configure<DailyPlaylistServiceOptions>(config =>
                    this.Configuration.GetSection(DailyPlaylistServiceOptions.SectionName).Bind(config))
                .Configure<PlaylistManagerOptions>(config =>
                    this.Configuration.GetSection(PlaylistManagerOptions.SectionName).Bind(config))
                .AddScoped<IDataSourceService, DataSourceService>()
                .AddScoped<IPlaylistManager, PlaylistManager>()
                .AddSingleton<ISpotifyClientService, SpotifyClientService>()
                .AddSingleton<FoundInSpotifyCache>()
                .AddSingleton<NotFoundInSpotifyCache>()
                .AddHostedService<DailyPlaylistHostedService>();
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
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Service is working!");
                });

                endpoints.MapGet("/auth", async context =>
                {
                    var client = context.RequestServices.GetRequiredService<ISpotifyClientService>();

                    if (client.IsAuthenticated())
                    {
                        await context.Response.WriteAsync("Already authenticated!");
                        return;
                    }
                        
                    context.Response.Redirect(client.GetAuthorizationCodeUrl().ToString(), permanent: false);
                    
                });

                endpoints.MapGet("/callback", async context =>
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

                    await client.SetAuthorizationCode(code);
                    await client.SetupAccessToken();

                    context.Response.Redirect("/isAuthenticated", permanent: true);
                });

                endpoints.MapGet("/isAuthenticated", async context =>
                {
                    var client = context.RequestServices.GetRequiredService<ISpotifyClientService>();

                    if (client.IsAuthenticated())
                    {
                        await context.Response.WriteAsync("Authenticated!");
                        return;
                    }

                    await context.Response.WriteAsync("Not authenticated!");
                });

                endpoints.MapGet("/playlist/", async context =>
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

                endpoints.MapGet("/userid", async context =>
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

                endpoints.MapGet("/userplaylists", async context =>
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

                endpoints.MapGet("/createplaylist", async context =>
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
}
