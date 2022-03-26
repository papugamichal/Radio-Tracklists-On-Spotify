using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioTracklistsOnSpotify.Services.DataSourceService;
using RadioTracklistsOnSpotify.Services.DataSourceService.Abstraction;
using RadioTracklistsOnSpotify.Services.DataSourceService.Configuration;
using RadioTracklistsOnSpotify.Services.PlaylistManager;
using RadioTracklistsOnSpotify.Services.PlaylistManager.Configuration;
using RadioTracklistsOnSpotify.Services.SpotifyClientService.Abstraction;
using RadioTracklistsOnSpotify.Services.TrackCache;

namespace RadioTracklistsOnSpotify.Services
{
    public static class Extensions
    {
        private const string RadioNowySwiatDataSourceSection = "RadioNowySwiatDataSource";
        private const string Radio357DataSourceSection = "Radio357DataSource";

        private const string RadioNowySwiatPlaylistManagerOptionsSection = "RadioNowySwiatPlaylistManagerOptions";
        private const string Radio357PlaylistManagerOptionsSection = "Radio357PlaylistManagerOptions";

        public static IServiceCollection AddRadioNowySwiatDataSource(this IServiceCollection services)
        {
            services.AddScoped<IDataSourceService, RadioNowySwiatDataSourceService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<RadioNowySwiatDataSourceService>>();
                var configuration = provider.GetRequiredService<IConfiguration>();

                var options = new DataSourceOptions();
                configuration.GetSection(RadioNowySwiatDataSourceSection).Bind(options);
                var iOptions = Options.Create(options);

                return new RadioNowySwiatDataSourceService(logger, iOptions);
            });
            return services;
        }

        public static IServiceCollection AddRadio357DataSource(this IServiceCollection services)
        {
            services.AddScoped<IDataSourceService, Radio357DataSourceService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<Radio357DataSourceService>>();
                var configuration = provider.GetRequiredService<IConfiguration>();
                var options = new DataSourceOptions();
                configuration.GetSection(Radio357DataSourceSection).Bind(options);
                var iOptions = Options.Create(options);

                return new Radio357DataSourceService(logger, iOptions);
            });
            return services;
        }

        public static IServiceCollection AddRadioNowySwiatPlaylistManager(this IServiceCollection services)
        {
            services.AddScoped<IPlaylistManager, RadioNowySwiatPlaylistManager>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<RadioNowySwiatPlaylistManager>>();
                    var dataSource = provider.GetServices<IDataSourceService>().First(o => o.GetType() == typeof(RadioNowySwiatDataSourceService));
                    var spotifyCient = provider.GetRequiredService<ISpotifyClientService>();
                    var foundTracksCache = provider.GetRequiredService<FoundInSpotifyCache>();
                    var notFoundTracksCache = provider.GetRequiredService<NotFoundInSpotifyCache>();

                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var options = new PlaylistManagerOptions();
                    configuration.GetSection(RadioNowySwiatPlaylistManagerOptionsSection).Bind(options);
                    var iOptions = Options.Create(options);

                    return new RadioNowySwiatPlaylistManager(logger, iOptions, dataSource, spotifyCient, foundTracksCache, notFoundTracksCache);
                });
            return services;
        }

        public static IServiceCollection AddRadio357PlaylistManager(this IServiceCollection services)
        {
            services.AddScoped<IPlaylistManager, Radio357PlaylistManager>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<Radio357PlaylistManager>>();
                var dataSource = provider.GetServices<IDataSourceService>().First(o => o.GetType() == typeof(Radio357DataSourceService));
                var spotifyCient = provider.GetRequiredService<ISpotifyClientService>();
                var foundTracksCache = provider.GetRequiredService<FoundInSpotifyCache>();
                var notFoundTracksCache = provider.GetRequiredService<NotFoundInSpotifyCache>();

                var configuration = provider.GetRequiredService<IConfiguration>();
                var options = new PlaylistManagerOptions();
                configuration.GetSection(Radio357PlaylistManagerOptionsSection).Bind(options);
                var iOptions = Options.Create(options);

                return new Radio357PlaylistManager(logger, iOptions, dataSource, spotifyCient, foundTracksCache, notFoundTracksCache);
            });
            return services;
        }
    }
}
