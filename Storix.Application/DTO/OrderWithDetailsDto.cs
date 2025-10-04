using Storix.Application.DTO.Orders;

namespace Storix.Application.DTO
{
    public class OrderWithDetailsDto:OrderDto
    {
        public string? SupplierName { get; set; }
        public string? CustomerName { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
    }
}
