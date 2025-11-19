using System;
using Storix.Domain.Enums;

namespace Storix.Application.DTO.Orders
{
    public class SalesOrderListDto
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
    }
}
