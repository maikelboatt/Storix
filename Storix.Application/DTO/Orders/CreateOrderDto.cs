using System;
using Storix.Domain.Enums;

namespace Storix.Application.DTO.Orders
{
    public class CreateOrderDto
    {
        public OrderType Type { get; set; }
        public int? SupplierId { get; set; }
        public int? CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
    }
}
