using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioNowySwiatAutomatedPlaylist.HostedServices.PlaylistUpdater.Configuration;
using RadioNowySwiatAutomatedPlaylist.Services.PlaylistManager;

namespace RadioNowySwiatAutomatedPlaylist.HostedServices.PlaylistUpdater
{
    public sealed class RadioNowySwiatPlaylistUpdaterHostedService : PlaylistUpdaterHostedService
    {
        private const string RadioNowySwiatSectionName = "RadioNowySwiatPlaylistUpdaterHostedService";

        public RadioNowySwiatPlaylistUpdaterHostedService(ILogger<RadioNowySwiatPlaylistUpdaterHostedService> logger, IServiceScopeFactory serviceScopeFactory) 
            : base(logger, serviceScopeFactory)
        {
        }

        internal override string RadioName => "Radio Nowo Swiat";

        internal override IOptions<PlaylistUpdaterOptions> OptionsProvider(IServiceProvider provider)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();

            var options = new PlaylistUpdaterOptions();
            configuration.GetSection(RadioNowySwiatSectionName).Bind(options);
            return Options.Create(options);
        }

        internal override IPlaylistManager PlaylistMangerProvider(IServiceProvider provider) =>
            provider.GetServices<IPlaylistManager>().First(o => o.GetType() == typeof(RadioNowySwiatPlaylistManager));
    }
}