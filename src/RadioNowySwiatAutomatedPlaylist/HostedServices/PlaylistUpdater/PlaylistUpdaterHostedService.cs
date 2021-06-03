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
    public class PlaylistUpdaterHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<PlaylistUpdaterHostedService> logger;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private IOptions<PlaylistUpdaterOptions> options;
        private Timer timer;

        public PlaylistUpdaterHostedService(
            ILogger<PlaylistUpdaterHostedService> logger,
            IServiceScopeFactory serviceScopeFactory
            )
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("DailyPlaylist hosted service is starting.");

            using var scope = serviceScopeFactory.CreateScope();
            options = scope.ServiceProvider.GetService<IOptions<PlaylistUpdaterOptions>>();
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
                var manager = scope.ServiceProvider.GetService<IPlaylistManager>();

                /* Version 1 */
                //manager.PopulateSpotifyDailylist().ConfigureAwait(false).GetAwaiter().GetResult();

                /* Version 2 */
                manager.PopulateTodayAndHandlePreviousPlaylists().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Something went wrong during processing daily playlist!");
            }

            logger.LogInformation($"Update playlist completed. Handled in {sw.Elapsed.TotalSeconds} seconds");
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("DailyPlaylist hosted service is stopping.");
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}