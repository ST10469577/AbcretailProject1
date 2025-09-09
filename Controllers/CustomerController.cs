using Microsoft.AspNetCore.Mvc;
using AbcRetailer.Services;
using AbcRetailer.Models;
using Azure.Data.Tables;
using System.Linq;
using System.Collections.Generic;

namespace AbcRetailer.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AzureStorageService _storage;

        public CustomerController(AzureStorageService storage)
        {
            _storage = storage;
        }

        // ----------------- List Customers -----------------
        [HttpGet]
        public IActionResult Index()
        {
            var table = _storage.GetTableClient("Profiles");
            var customers = table.Query<CustomerProfile>(c => c.PartitionKey == "Customer").ToList();
            return View(customers);
        }

        // ----------------- Dashboard / Online Store -----------------
        [HttpGet]
        public IActionResult Dashboard(string category = null)
        {
            var table = _storage.GetTableClient("Products");

            // Get all products first (convert Pageable to List to allow LINQ filtering)
            List<ProductEntity> allProducts = table.Query<ProductEntity>(p => p.PartitionKey == "Product").ToList();

            // Apply category filter on the in-memory list
            List<ProductEntity> products = string.IsNullOrEmpty(category)
                ? allProducts
                : allProducts.Where(p => p.Category == category).ToList();

            // Get distinct categories for filter dropdown
            List<string> categories = allProducts.Select(p => p.Category)
                                                 .Distinct()
                                                 .ToList();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;

            return View(products);
        }
    }
}
