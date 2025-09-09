using System.Collections.Generic;

namespace AbcRetailer.Models
{
    public class ManageUsersViewModel
    {
        public List<EmployeeProfile> Employees { get; set; } = new();
        public List<CustomerProfile> Customers { get; set; } = new();
        public List<AdminProfile> Admins { get; set; } = new();
        public List<ProductEntity> Products { get; set; } = new();
    }
}
