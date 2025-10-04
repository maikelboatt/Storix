using System;

namespace Storix.Application.DTO.Suppliers
{
    public class UpdateSupplierDto
    {
        public int SupplierId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; } = null;
    }
}
