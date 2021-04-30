using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioNowySwiatPlaylistBot.Services.AccessLimiterHostedService.Configuration;
using RadioNowySwiatPlaylistBot.Services.PlaylistManager;

namespace RadioNowySwiatPlaylistBot.Services.AccessLimiterHostedService
{
    public class AccessLimiterHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<AccessLimiterHostedService> logger;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private IOptions<AccessLimiterServiceOptions> options;
        private Timer timer;

        public AccessLimiterHostedService(
            ILogger<AccessLimiterHostedService> logger,
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
            options = scope.ServiceProvider.GetService<IOptions<AccessLimiterServiceOptions>>();

            var delay = CalcualteDelayToMidnight();
            timer = new Timer(DoWork, null, TimeSpan.Zero, options.Value.RefreshInterval);

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

                var limit = TimeSpan.FromDays(this.options.Value.PublicAccessPlaylistLimit);
                manager.LimitAccessToPlaylistOlderThan(limit).ConfigureAwait(false).GetAwaiter().GetResult(); 
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

        private TimeSpan CalcualteDelayToMidnight()
        {
            TimeSpan ScheduledTimespan = new TimeSpan(0, 10, 0);
            TimeSpan TimeOftheDay = TimeSpan.Parse(DateTime.Now.TimeOfDay.ToString("hh\\:mm"));

            return ScheduledTimespan >= TimeOftheDay
                ? ScheduledTimespan - TimeOftheDay    // When Scheduled Time for that day is not passed
                : new TimeSpan(24, 0, 0) - TimeOftheDay + ScheduledTimespan;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}