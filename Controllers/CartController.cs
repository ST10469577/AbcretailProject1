using Microsoft.AspNetCore.Mvc;
using AbcRetailer.Services;
using Azure.Storage.Queues;
using System.Text.Json;

namespace AbcRetailer.Controllers
{
    public class CartController : Controller
    {
        private readonly IConfiguration _config;

        public CartController(IConfiguration config)
        {
            _config = config;
        }

        // ---------------- Add to Cart ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(string productId, string productName, double price)
        {
            var cartItem = new
            {
                ProductId = productId,
                ProductName = productName,
                Price = price,
                AddedAt = DateTime.UtcNow
            };

            var message = JsonSerializer.Serialize(cartItem);

            var queueClient = new QueueClient(_config.GetConnectionString("AzureStorage"), "cart-queue");
            queueClient.CreateIfNotExists();

            queueClient.SendMessage(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message)));

            TempData["Message"] = $"{productName} added to cart!";
            return RedirectToAction("Dashboard", "Customer");
        }

        // ---------------- View Cart / Checkout ----------------
        [HttpGet]
        public IActionResult Checkout()
        {
            var cartItems = GetCartItems();
            return View(cartItems); // pass the List<dynamic> to the view
        }

        // ---------------- Helper: Get items from Queue ----------------
        private List<dynamic> GetCartItems()
        {
            var queueClient = new QueueClient(_config.GetConnectionString("AzureStorage"), "cart-queue");
            queueClient.CreateIfNotExists();

            var response = queueClient.ReceiveMessages(maxMessages: 32);
            var messages = response.Value;

            var items = new List<dynamic>();

            foreach (var msg in messages)
            {
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(msg.MessageText));
                var item = JsonSerializer.Deserialize<dynamic>(json);
                items.Add(item);
            }

            return items;
        }
    }
}
