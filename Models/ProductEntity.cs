using Azure;
using Azure.Data.Tables;
using System;

namespace AbcRetailer.Models
{
    public class ProductEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";

        // Keep RowKey default empty; assign GUID when adding in controller
        public string RowKey { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } // Blob URL
        public string Category { get; set; }

        // Required for ITableEntity
        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }
    }
}
