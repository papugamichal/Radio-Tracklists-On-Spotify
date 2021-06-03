using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioNowySwiatPlaylistBot.Services.PlaylistManager;
using RadioNowySwiatPlaylistBot.Services.PlaylistVisibilityLimiterHostedService.Configuration;

namespace RadioNowySwiatPlaylistBot.Services.PlaylistVisibilityLimiterHostedService
{
    public class PlaylistVisibilityLimiterHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<PlaylistVisibilityLimiterHostedService> logger;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private IOptions<PlaylistVisibilityLimiterOptions> options;
        private Timer timer;

        public PlaylistVisibilityLimiterHostedService(
            ILogger<PlaylistVisibilityLimiterHostedService> logger,
            IServiceScopeFactory serviceScopeFactory
            )
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Public playlist limiter hosted service is starting.");

            using var scope = serviceScopeFactory.CreateScope();
            options = scope.ServiceProvider.GetService<IOptions<PlaylistVisibilityLimiterOptions>>();

            if (!options.Value.Enabled)
            {
                logger.LogInformation("Service is disabled.");
                return Task.CompletedTask;
            }

            var delay = CalcualteDelayToMidnight();
            timer = new Timer(DoWork, null, delay, options.Value.RefreshInterval);

            DoWork(null);

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

                var limit = TimeSpan.FromDays(this.options.Value.Limit);
                manager.LimitAccessToPlaylistOlderThan(limit).ConfigureAwait(false).GetAwaiter().GetResult(); 
            }
            catch (Exception e)
            {
                logger.LogError(e, "Something went wrong during update visibility of public playlist!");
            }

            logger.LogInformation($"Update visibility of public playlist completed. Handled in {sw.Elapsed.TotalSeconds} seconds");
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Public playlist limiter hosted service is stopping.");

            if (!options.Value.Enabled)
            {
                return Task.CompletedTask;
            }

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