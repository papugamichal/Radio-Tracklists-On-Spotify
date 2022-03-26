using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioTracklistsOnSpotify.HostedServices.KeepAlive.Configuration;

namespace RadioTracklistsOnSpotify.HostedServices.KeepAlive
{
    public class KeepAliveHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<KeepAliveHostedService> logger;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private IOptions<KeepAliveServiceOptions> options;
        private Timer timer;

        public KeepAliveHostedService(
            ILogger<KeepAliveHostedService> logger,
            IServiceScopeFactory serviceScopeFactory
            )
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serviceScopeFactory = serviceScopeFactory;
            this.serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Keep alive service is starting.");

            using var scope = serviceScopeFactory.CreateScope();
            options = scope.ServiceProvider.GetService<IOptions<KeepAliveServiceOptions>>();

            if (options.Value.Enabled)
            {
                timer = new Timer(DoWork, null, TimeSpan.FromSeconds(30), options.Value.RefreshInterval);
            }
            else
            {
                logger.LogInformation("Service is disabled");
            }

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var client = new HttpClient();
            var request = client.GetAsync(options.Value.Url).ConfigureAwait(false).GetAwaiter().GetResult();

            if (request.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogError($"Something went wrong! Request end with code: {request.StatusCode}");
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("KeepAlive hosted service is stopping.");
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}