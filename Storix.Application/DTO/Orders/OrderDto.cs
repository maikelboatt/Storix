using System;
using Storix.Domain.Enums;

namespace Storix.Application.DTO.Orders
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public OrderType Type { get; set; }
        public OrderStatus Status { get; set; }
        public int? SupplierId { get; set; }
        public int? CustomerId { get; set; }
        public int LocationId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
    }
}
