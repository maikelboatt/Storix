using System;
using System.Collections.Generic;
using Storix.Application.DTO.OrderItems;
using Storix.Domain.Enums;

namespace Storix.Application.DTO.Orders
{
    public class CreateOrderDto
    {
        public OrderType Type { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Draft;
        public int? SupplierId { get; set; }
        public int? CustomerId { get; set; }
        public int LocationId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }

        public List<CreateOrderItemDto> OrderItems { get; set; } = [];
    }
}
