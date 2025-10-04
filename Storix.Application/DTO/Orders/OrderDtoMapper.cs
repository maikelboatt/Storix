using System.Collections.Generic;
using System.Linq;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.DTO.Orders
{
    public static class OrderDtoMapper
    {
        public static OrderDto ToDto( this Order order ) => new()
        {
            OrderId = order.OrderId,
            Type = order.Type,
            Status = order.Status,
            SupplierId = order.SupplierId,
            CustomerId = order.CustomerId,
            OrderDate = order.OrderDate,
            DeliveryDate = order.DeliveryDate,
            Notes = order.Notes,
            CreatedBy = order.CreatedBy
        };

        public static Order ToDomain( this OrderDto dto ) => new(
            dto.OrderId,
            dto.Type,
            dto.Status,
            dto.SupplierId,
            dto.CustomerId,
            dto.OrderDate,
            dto.DeliveryDate,
            dto.Notes,
            dto.CreatedBy
        );

        public static CreateOrderDto ToCreateDto( this OrderDto dto ) => new()
        {
            Type = dto.Type,
            SupplierId = dto.SupplierId,
            CustomerId = dto.CustomerId,
            OrderDate = dto.OrderDate,
            DeliveryDate = dto.DeliveryDate,
            Notes = dto.Notes,
            CreatedBy = dto.CreatedBy
        };

        public static UpdateOrderDto ToUpdateDto( this OrderDto dto ) => new()
        {
            OrderId = dto.OrderId,
            Status = dto.Status,
            DeliveryDate = dto.DeliveryDate,
            Notes = dto.Notes
        };

        public static CreateOrderDto ToCreateDto( this Order order ) => new()
        {
            Type = order.Type,
            SupplierId = order.SupplierId,
            CustomerId = order.CustomerId,
            OrderDate = order.OrderDate,
            DeliveryDate = order.DeliveryDate,
            Notes = order.Notes,
            CreatedBy = order.CreatedBy
        };

        public static Order ToDomain( this CreateOrderDto dto ) => new(
            0, // OrderId will be assigned by database
            dto.Type,
            OrderStatus.Draft, // New orders start as Draft
            dto.SupplierId,
            dto.CustomerId,
            dto.OrderDate,
            dto.DeliveryDate,
            dto.Notes,
            dto.CreatedBy
        );

        public static Order ToDomain( this UpdateOrderDto dto, Order existingOrder ) => existingOrder with
        {
            Status = dto.Status,
            DeliveryDate = dto.DeliveryDate,
            Notes = dto.Notes
        };

        public static IEnumerable<OrderDto> ToDto( this IEnumerable<Order> orders ) => orders.Select(o => o.ToDto());
    }
}
