using DevStore.Billing.API.Models;
using DevStore.Core.DomainObjects;
using DevStore.Core.Messages.Integration;
using DevStore.MessageBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevStore.Billing.API.Services
{
    public class BillingIntegrationHandler : BackgroundService
    {
        private readonly IMessageBus _bus;
        private readonly IServiceProvider _serviceProvider;

        public BillingIntegrationHandler(
                            IServiceProvider serviceProvider,
                            IMessageBus bus)
        {
            _serviceProvider = serviceProvider;
            _bus = bus;
        }

        /*
        private async Task SetResponse()
        {
            await _bus.RespondAsync<OrderInitiatedIntegrationEvent, ResponseMessage>(AuthorizeTransaction);
        }
        */

        private async Task SetSubscribersAsync(CancellationToken stoppingToken)
        {
            await _bus.ConsumerAsync<OrderCanceledIntegrationEvent>("OrderCanceled", CancelTransaction, stoppingToken);

            await _bus.ConsumerAsync<OrderLoweredStockIntegrationEvent>("OrderLoweredStock", CapturePayment, stoppingToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await SetResponse();
            await SetSubscribersAsync(stoppingToken);
        }

        private async Task<ResponseMessage> AuthorizeTransaction(OrderInitiatedIntegrationEvent message)
        {
            using var scope = _serviceProvider.CreateScope();
            var billingService = scope.ServiceProvider.GetRequiredService<IBillingService>();
            var transaction = new Payment
            {
                OrderId = message.OrderId,
                PaymentType = (PaymentType)message.PaymentType,
                Amount = message.Amount,
                CreditCard = new CreditCard(
                    message.Holder, message.CardNumber, message.ExpirationDate, message.SecurityCode)
            };

            return await billingService.AuthorizeTransaction(transaction);
        }

        private async Task CancelTransaction(OrderCanceledIntegrationEvent message)
        {
            using var scope = _serviceProvider.CreateScope();

            var pagamentoService = scope.ServiceProvider.GetRequiredService<IBillingService>();

            var Response = await pagamentoService.CancelTransaction(message.OrderId);

            if (!Response.ValidationResult.IsValid)
                throw new DomainException($"Failed to cancel order payment {message.OrderId}");
        }

        private async Task CapturePayment(OrderLoweredStockIntegrationEvent message)
        {
            using var scope = _serviceProvider.CreateScope();

            var pagamentoService = scope.ServiceProvider.GetRequiredService<IBillingService>();

            var Response = await pagamentoService.GetTransaction(message.OrderId);

            if (!Response.ValidationResult.IsValid)
                throw new DomainException($"Error trying to get order payment {message.OrderId}");

            await _bus.ProducerAsync("OrderPaid", new OrderPaidIntegrationEvent(message.CustomerId, message.OrderId));
        }
    }
}