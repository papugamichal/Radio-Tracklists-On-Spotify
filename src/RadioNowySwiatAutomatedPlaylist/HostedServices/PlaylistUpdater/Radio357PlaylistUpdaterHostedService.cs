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
    public sealed class Radio357PlaylistUpdaterHostedService : PlaylistUpdaterHostedService
    {
        private const string Radio357SectionName = "Radio357PlaylistUpdaterHostedService";

        public Radio357PlaylistUpdaterHostedService(ILogger<Radio357PlaylistUpdaterHostedService> logger, IServiceScopeFactory serviceScopeFactory)
            : base(logger, serviceScopeFactory)
        {
        }

        internal override string RadioName => "Radio 357";

        internal override IOptions<PlaylistUpdaterOptions> OptionsProvider(IServiceProvider provider)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();

            var options = new PlaylistUpdaterOptions();
            configuration.GetSection(Radio357SectionName).Bind(options);
            return Options.Create(options);
        }

        internal override IPlaylistManager PlaylistMangerProvider(IServiceProvider provider) =>
            provider.GetServices<IPlaylistManager>().First(o => o.GetType() == typeof(Radio357PlaylistManager));
    }
}