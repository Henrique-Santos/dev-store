using DevStore.WebApp.MVC.Models;
using DevStore.WebApp.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DevStore.WebApp.MVC.Controllers
{
    public class OrderController : MainController
    {
        private readonly IClientService _clientService;
        private readonly ICheckoutBffService _checkoutBffService;

        public OrderController(IClientService clientService,
            ICheckoutBffService checkoutBffService)
        {
            _clientService = clientService;
            _checkoutBffService = checkoutBffService;
        }

        [HttpGet]
        [Route("delivery-address")]
        public async Task<IActionResult> DeliveryAddress()
        {
            var carrinho = await _checkoutBffService.GetShoppingCart();
            if (carrinho.Items.Count == 0) return RedirectToAction("Index", "ShoppingCart");

            var endereco = await _clientService.GetAddress();
            var pedido = _checkoutBffService.MapToOrder(carrinho, endereco);

            return View(pedido);
        }

        [HttpGet]
        [Route("payment")]
        public async Task<IActionResult> Payment()
        {
            var carrinho = await _checkoutBffService.GetShoppingCart();
            if (carrinho.Items.Count == 0) return RedirectToAction("Index", "ShoppingCart");

            var pedido = _checkoutBffService.MapToOrder(carrinho, null);

            return View(pedido);
        }

        [HttpPost]
        [Route("finish-order")]
        public async Task<IActionResult> FinishOrder(TransactionViewModel transaction)
        {
            if (!ModelState.IsValid) return View("Payment", _checkoutBffService.MapToOrder(await _checkoutBffService.GetShoppingCart(), null));

            var retorno = await _checkoutBffService.FinishOrder(transaction);

            if (ResponseHasErrors(retorno))
            {
                var carrinho = await _checkoutBffService.GetShoppingCart();
                if (carrinho.Items.Count == 0) return RedirectToAction("Index", "ShoppingCart");

                var pedidoMap = _checkoutBffService.MapToOrder(carrinho, null);
                return View("Payment", pedidoMap);
            }

            return RedirectToAction("OrderDone");
        }

        [HttpGet]
        [Route("order-done")]
        public async Task<IActionResult> OrderDone()
        {
            return View("OrderDone", await _checkoutBffService.GetLastOrder());
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> MyOrders()
        {
            return View(await _checkoutBffService.GetClientsById());
        }
    }
}