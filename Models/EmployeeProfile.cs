using Azure;
using Azure.Data.Tables;

namespace AbcRetailer.Models
{
    public class EmployeeProfile : ITableEntity
    {
        public string PartitionKey { get; set; } = "Employee";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }

        // Required ITableEntity properties
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
