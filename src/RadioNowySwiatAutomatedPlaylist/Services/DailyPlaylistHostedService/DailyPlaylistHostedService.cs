using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioNowySwiatPlaylistBot.Services.DailyPlaylistHostedService.Configuration;
using RadioNowySwiatPlaylistBot.Services.PlaylistManager;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.Abstraction;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadioNowySwiatPlaylistBot.Services.DailyPlaylistHostedService
{
    public class DailyPlaylistHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<DailyPlaylistHostedService> logger;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private IOptions<DailyPlaylistServiceOptions> options;
        private Timer timer;

        public DailyPlaylistHostedService(
            ILogger<DailyPlaylistHostedService> logger,
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
            options = scope.ServiceProvider.GetService<IOptions<DailyPlaylistServiceOptions>>();
            timer = new Timer(DoWork, null, TimeSpan.FromSeconds(30), options.Value.RefreshInterval);

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
                manager.PopulateSpotifyDailylist().ConfigureAwait(false).GetAwaiter().GetResult();

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