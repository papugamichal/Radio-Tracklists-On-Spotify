using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RadioTracklistsOnSpotify.HostedServices.KeepAlive;
using RadioTracklistsOnSpotify.HostedServices.KeepAlive.Configuration;
using RadioTracklistsOnSpotify.HostedServices.PlaylistUpdater;
using RadioTracklistsOnSpotify.HostedServices.PlaylistVisibilityLimiter;
using RadioTracklistsOnSpotify.HostedServices.PlaylistVisibilityLimiter.Configuration;

namespace RadioTracklistsOnSpotify.HostedServices
{
    public static class Extensions
    {
        public static IServiceCollection AddRadioNowySwiatPlaylistUpdaterHostedService(this IServiceCollection services)
        {
            services.AddHostedService<RadioNowySwiatPlaylistUpdaterHostedService>();
            return services;
        }

        public static IServiceCollection AddRadio357PlaylistUpdaterHostedService(this IServiceCollection services)
        {
            services.AddHostedService<Radio357PlaylistUpdaterHostedService>();
            return services;
        }

        public static IServiceCollection AddKeepAliveHostedService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<KeepAliveServiceOptions>(options => configuration.GetSection(KeepAliveServiceOptions.SectionName).Bind(options));
            services.AddHostedService<KeepAliveHostedService>();
            return services;
        }

        public static IServiceCollection AddPlaylistVisibilityLimiterHostedService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<PlaylistVisibilityLimiterOptions>(options => configuration.GetSection(PlaylistVisibilityLimiterOptions.SectionName).Bind(options));
            services.AddHostedService<PlaylistVisibilityLimiterHostedService>();
            return services;
        }
    }
}
