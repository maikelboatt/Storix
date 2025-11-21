using System;

namespace Storix.Application.DTO.Customers
{
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        public override string ToString() => Name;
    }
}
