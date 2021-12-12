using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioNowySwiatAutomatedPlaylist.HostedServices.PlaylistUpdater.Configuration;
using RadioNowySwiatAutomatedPlaylist.Services.PlaylistManager;

namespace RadioNowySwiatAutomatedPlaylist.HostedServices.PlaylistUpdater
{
    public abstract class PlaylistUpdaterHostedService : IHostedService, IDisposable
    {
        private readonly ILogger logger;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private IOptions<PlaylistUpdaterOptions> options;
        private Timer timer;

        public PlaylistUpdaterHostedService(
            ILogger logger,
            IServiceScopeFactory serviceScopeFactory
            )
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        internal abstract string RadioName { get; }
        internal abstract IPlaylistManager PlaylistMangerProvider(IServiceProvider provider);
        internal abstract IOptions<PlaylistUpdaterOptions> OptionsProvider(IServiceProvider provider);

        public Task StartAsync(CancellationToken stoppingToken)
        {
            using var scope = serviceScopeFactory.CreateScope();
            options = OptionsProvider(scope.ServiceProvider);

            if (!options.Value.Enabled)
            {
                logger.LogInformation($"Playlist updater for '{RadioName}' hosted service is disabled.");
                return Task.CompletedTask;
            }

            logger.LogInformation($"Playlist updater for '{RadioName}' hosted service is starting.");
            timer = new Timer(DoWork, null, TimeSpan.FromSeconds(10), options.Value.RefreshInterval);
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var manager = PlaylistMangerProvider(scope.ServiceProvider);

                /* Version 1 */
                //manager.PopulateSpotifyDailylist().ConfigureAwait(false).GetAwaiter().GetResult();

                /* Version 2 */
                manager.PopulateTodayAndHandlePreviousPlaylists().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Something went wrong during processing '{RadioName}' playlist!");
            }

            logger.LogInformation($"Updating '{RadioName}' playlist completed. Handled in {sw.Elapsed.TotalSeconds} seconds");
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation($"Playlist updater for '{RadioName}' hosted service is stopping.");
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}