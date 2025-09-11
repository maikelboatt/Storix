using Storix.Domain.Enums;
using System;

namespace Storix.Application.DTO.Order
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
