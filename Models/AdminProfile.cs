using Azure;
using Azure.Data.Tables;

namespace AbcRetailer.Models
{
    public class AdminProfile : ITableEntity
    {
        public string PartitionKey { get; set; } = "Admin";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }

        // Required ITableEntity members
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
