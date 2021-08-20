using DevStore.Bff.Checkout.Models;
using DevStore.Bff.Checkout.Services;
using DevStore.WebAPI.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DevStore.Bff.Checkout.Controllers
{
    [Authorize, Route("orders")]
    public class OrderController : MainController
    {
        private readonly ICatalogService _catalogService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderService _orderService;
        private readonly IClientService _clientService;

        public OrderController(
            ICatalogService catalogService,
            IShoppingCartService shoppingCartService,
            IOrderService orderService,
            IClientService clientService)
        {
            _catalogService = catalogService;
            _shoppingCartService = shoppingCartService;
            _orderService = orderService;
            _clientService = clientService;
        }

        [HttpPost]
        [Route("")]
        public async Task<IActionResult> AddOrder(OrderDto order)
        {
            var carrinho = await _shoppingCartService.GetShoppingCart();
            var produtos = await _catalogService.GetItems(carrinho.Items.Select(p => p.ProductId));
            var endereco = await _clientService.GetAddress();

            if (!await CheckShoppingCartProducts(carrinho, produtos)) return CustomResponse();

            PopulateOrderData(carrinho, endereco, order);

            return CustomResponse(await _orderService.FinishOrder(order));
        }

        [HttpGet("last")]
        public async Task<IActionResult> LastOrder()
        {
            var pedido = await _orderService.GetLastOrder();
            if (pedido is null)
            {
                AddErrorToStack("Order not found!");
                return CustomResponse();
            }

            return CustomResponse(pedido);
        }

        [HttpGet("clients")]
        public async Task<IActionResult> ClientList()
        {
            var pedidos = await _orderService.GetClients();

            return pedidos == null ? NotFound() : CustomResponse(pedidos);
        }

        private async Task<bool> CheckShoppingCartProducts(ShoppingCartDto shoppingCart, IEnumerable<ProductDto> produtos)
        {
            if (shoppingCart.Items.Count != produtos.Count())
            {
                var itensIndisponiveis = shoppingCart.Items.Select(c => c.ProductId).Except(produtos.Select(p => p.Id)).ToList();

                foreach (var itemId in itensIndisponiveis)
                {
                    var itemCarrinho = shoppingCart.Items.FirstOrDefault(c => c.ProductId == itemId);
                    AddErrorToStack($"The item {itemCarrinho.Name} is not available at our catalog. Remove it from shoppingCart to continue shopping.");
                }

                return false;
            }

            foreach (var itemCarrinho in shoppingCart.Items)
            {
                var produtoCatalogo = produtos.FirstOrDefault(p => p.Id == itemCarrinho.ProductId);

                if (produtoCatalogo.Price != itemCarrinho.Price)
                {
                    var msgErro = $"The price of product {itemCarrinho.Name} has changed (from: " +
                                  $"{string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", itemCarrinho.Price)} to: " +
                                  $"{string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", produtoCatalogo.Price)}) since it has added to shoppingCart.";

                    AddErrorToStack(msgErro);

                    var responseRemove = await _shoppingCartService.RemoveItem(itemCarrinho.ProductId);
                    if (ResponsePossuiErros(responseRemove))
                    {
                        AddErrorToStack($"It was not possible to auto remove the product {itemCarrinho.Name} from your shopping cart, _" +
                                                   "remove and add it again.");
                        return false;
                    }

                    itemCarrinho.Price = produtoCatalogo.Price;
                    var responseAdd = await _shoppingCartService.AddItem(itemCarrinho);

                    if (ResponsePossuiErros(responseAdd))
                    {
                        AddErrorToStack($"It was not possible to auto update you product {itemCarrinho.Name} from your shopping cart, _" +
                                                   "add it again.");
                        return false;
                    }

                    CleanErrors();
                    AddErrorToStack(msgErro + " We've updated your shopping cart. Check it again.");

                    return false;
                }
            }

            return true;
        }

        private void PopulateOrderData(ShoppingCartDto shoppingCart, AddressDto address, OrderDto order)
        {
            order.Voucher = shoppingCart.Voucher?.Code;
            order.HasVoucher = shoppingCart.HasVoucher;
            order.Amount = shoppingCart.Total;
            order.Discount = shoppingCart.Discount;
            order.OrderItems = shoppingCart.Items;

            order.Address = address;
        }
    }
}
