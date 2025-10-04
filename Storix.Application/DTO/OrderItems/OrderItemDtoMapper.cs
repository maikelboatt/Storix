using System.Collections.Generic;
using System.Linq;
using Storix.Domain.Models;


namespace Storix.Application.DTO.OrderItems
{
    public static class OrderItemDtoMapper
    {
        public static OrderItemDto ToDto( this OrderItem orderItem ) => new()
        {
            OrderItemId = orderItem.OrderItemId,
            OrderId = orderItem.OrderId,
            ProductId = orderItem.ProductId,
            Quantity = orderItem.Quantity,
            UnitPrice = orderItem.UnitPrice,
            TotalPrice = orderItem.TotalPrice
        };

        public static OrderItem ToDomain( this OrderItemDto dto ) => new(
            dto.OrderItemId,
            dto.OrderId,
            dto.ProductId,
            dto.Quantity,
            dto.UnitPrice,
            dto.TotalPrice
        );

        public static OrderItem ToDomain( this CreateOrderItemDto dto ) => new(
            0, // OrderItemId will be assigned by database
            dto.OrderId,
            dto.ProductId,
            dto.Quantity,
            dto.UnitPrice,
            dto.UnitPrice * dto.Quantity // Calculate TotalPrice
        );

        public static OrderItem ToDomain( this UpdateOrderItemDto dto, OrderItem existingOrderItem ) => existingOrderItem with
        {
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            TotalPrice = dto.UnitPrice * dto.Quantity // Recalculate TotalPrice
        };

        public static IEnumerable<OrderItemDto> ToDto( this IEnumerable<OrderItem> orderItems ) => orderItems.Select(oi => oi.ToDto());
    }
}
