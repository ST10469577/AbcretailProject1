using Microsoft.AspNetCore.Mvc;
using AbcRetailer.Services;
using AbcRetailer.Models;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AbcRetailer.Controllers
{
    public class AdminController : Controller
    {
        private readonly AzureStorageService _storage;

        public AdminController(AzureStorageService storage)
        {
            _storage = storage;
        }

        // ----------------- Register -----------------
        [HttpGet]
        public IActionResult Register()
        {
            return View(new AdminProfile());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AdminProfile admin)
        {
            if (!ModelState.IsValid)
                return View(admin);

            var table = _storage.GetTableClient("Profiles");

            admin.PartitionKey = "Admin";
            admin.RowKey = Guid.NewGuid().ToString();

            await table.AddEntityAsync(admin);

            TempData["Success"] = "Admin registered successfully. Please log in.";
            return RedirectToAction("Login");
        }

        // ----------------- Login -----------------
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password)
        {
            var table = _storage.GetTableClient("Profiles");
            var admins = table.Query<AdminProfile>(a => a.PartitionKey == "Admin" && a.Username == username).ToList();

            if (admins.Any() && admins.First().Password == password)
            {
                TempData["Admin"] = username;
                return RedirectToAction("Index");
            }

            ViewBag.Error = "Invalid credentials.";
            return View();
        }

        public IActionResult Index() => View();

        // ----------------- Inventory CRUD -----------------
        [HttpGet]
        [Route("Admin/Inventory")]
        public IActionResult ManageInventory()
        {
            var table = _storage.GetTableClient("Products");
            var products = table.Query<ProductEntity>().ToList();
            return View(products);
        }

        [HttpGet]
        public IActionResult AddProduct() => View(new ProductEntity());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(ProductEntity product, IFormFile ImageFile)
        {
            var table = _storage.GetTableClient("Products");

            product.PartitionKey ??= "Product";
            product.RowKey ??= Guid.NewGuid().ToString();

            if (ImageFile != null && ImageFile.Length > 0)
                product.ImageUrl = await _storage.UploadFileToBlobAsync(ImageFile);

            await table.AddEntityAsync(product);
            return RedirectToAction("ManageInventory");
        }

        [HttpGet]
        public IActionResult EditProduct(string rowKey)
        {
            var table = _storage.GetTableClient("Products");
            var product = table.Query<ProductEntity>(p => p.PartitionKey == "Product" && p.RowKey == rowKey).FirstOrDefault();

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(ProductEntity product, IFormFile ImageFile)
        {
            var table = _storage.GetTableClient("Products");

            if (ImageFile != null && ImageFile.Length > 0)
                product.ImageUrl = await _storage.UploadFileToBlobAsync(ImageFile);

            await table.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);
            return RedirectToAction("ManageInventory");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(string rowKey)
        {
            var table = _storage.GetTableClient("Products");

            try
            {
                await table.DeleteEntityAsync("Product", rowKey);
            }
            catch
            {
                return NotFound();
            }

            return RedirectToAction("ManageInventory");
        }

        // ----------------- Logs (Azure File Share) -----------------
        [HttpGet]
        public async Task<IActionResult> Logs()
        {
            var logs = await _storage.ListLogsAsync();
            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadLog(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return NotFound();

            var logContent = await _storage.DownloadLogAsync(fileName);
            if (string.IsNullOrEmpty(logContent))
                return NotFound();

            var bytes = System.Text.Encoding.UTF8.GetBytes(logContent);
            return File(bytes, "text/plain", fileName);
        }
    }
}
