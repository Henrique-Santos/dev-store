using DevStore.Core.Messages.Integration;
using DevStore.MessageBus;
using DevStore.Orders.API.Application.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevStore.Orders.API.Services
{
    public class OrderOrquestratorIntegrationHandler : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderOrquestratorIntegrationHandler> _logger;
        private Timer _timer;

        public OrderOrquestratorIntegrationHandler(ILogger<OrderOrquestratorIntegrationHandler> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Order service initialized.");

            _timer = new Timer(ProcessarPedidos, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(15));

            return Task.CompletedTask;
        }

        private async void ProcessarPedidos(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var pedidoQueries = scope.ServiceProvider.GetRequiredService<IOrderQueries>();
                var pedido = await pedidoQueries.GetAuthorizedOrders();

                if (pedido == null) return;

                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                var authorizedOrder = new OrderAuthorizedIntegrationEvent(pedido.ClientId, pedido.Id,
                    pedido.OrderItems.ToDictionary(p => p.ProductId, p => p.Quantity));

                await bus.PublishAsync(authorizedOrder);

                _logger.LogInformation($"Order ID: {pedido.Id} was sent to lower at stock.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Order service finished.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}