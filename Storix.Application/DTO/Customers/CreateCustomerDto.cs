using System;

namespace Storix.Application.DTO.Customers
{
    public class CreateCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}
