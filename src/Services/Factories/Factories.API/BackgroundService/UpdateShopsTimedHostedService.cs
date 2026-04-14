using Factories.Domain.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RtuItLab.Infrastructure.MassTransit.Shops.Requests;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Factories.API.BackgroundService
{
    // FIX: файл отсутствовал в репозитории — проект не компилировался.
    // Сервис каждые 30 секунд вычисляет накопленную продукцию фабрик
    // и отправляет её в Shops через RabbitMQ.
    public class UpdateShopsTimedHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<UpdateShopsTimedHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBusControl _busControl;
        private readonly Uri _shopsQueue = new Uri("rabbitmq://rabbit/shopsQueue");
        private Timer _timer;

        public UpdateShopsTimedHostedService(
            ILogger<UpdateShopsTimedHostedService> logger,
            IServiceScopeFactory scopeFactory,
            IBusControl busControl)
        {
            _logger       = logger;
            _scopeFactory = scopeFactory;
            _busControl   = busControl;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Factories UpdateShopsTimedHostedService started.");
            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var factoriesService = scope.ServiceProvider.GetRequiredService<IFactoriesService>();
                var products = await factoriesService.CreateRequestInShops();

                if (products.Count == 0)
                {
                    _logger.LogDebug("Factories: no products ready for shops yet.");
                    return;
                }

                var endpoint = await _busControl.GetSendEndpoint(_shopsQueue);
                await endpoint.Send(new AddProductsByFactoryRequest
                {
                    Products = new System.Collections.Generic.List<RtuItLab.Infrastructure.Models.Shops.ProductByFactory>(products)
                });

                _logger.LogInformation($"Factories: sent {products.Count} product batches to Shops.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Factories UpdateShopsTimedHostedService: error during DoWork");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Factories UpdateShopsTimedHostedService stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose() => _timer?.Dispose();
    }
}
