using Storix.Domain.Enums;
using System;

namespace Storix.Application.DTO.Order
{
    public class UpdateOrderDto
    {
        public int OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }
    }
}
