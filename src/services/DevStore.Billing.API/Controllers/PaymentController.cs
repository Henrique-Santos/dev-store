using DevStore.Billing.API.Models;
using DevStore.Billing.API.Services;
using DevStore.Core.Messages.Integration;
using DevStore.WebAPI.Core.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DevStore.Billing.API.Controllers
{
    public class PaymentController : MainController
    {
        [HttpPost("/payment/order-initiated")]
        public async Task<IActionResult> PostAsync(OrderInitiatedIntegrationEvent @event, [FromServices] IBillingService billingService)
        {
            var payment = GetPaymentFrom(@event);

            var response = await billingService.AuthorizeTransaction(payment);

            return Ok(response);
        }

        private static Payment GetPaymentFrom(OrderInitiatedIntegrationEvent @event)
        {
            return new Payment
            {
                OrderId = @event.OrderId,
                PaymentType = (PaymentType)@event.PaymentType,
                Amount = @event.Amount,
                CreditCard = new CreditCard(@event.Holder, @event.CardNumber, @event.ExpirationDate, @event.SecurityCode)
            };
        }
    }
}