using System;
using Storix.Domain.Enums;

namespace Storix.Application.DTO.Orders
{
    public class UpdateOrderDto
    {
        public int OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public int? LocationId { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }
    }
}
