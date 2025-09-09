using Azure;
using Azure.Data.Tables;

namespace AbcRetailer.Models
{
    public class CustomerProfile : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }

        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
