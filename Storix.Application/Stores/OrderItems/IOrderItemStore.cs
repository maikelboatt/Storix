using System.Collections.Generic;
using Storix.Application.DTO.OrderItems;
using Storix.Domain.Models;

namespace Storix.Application.Stores.OrderItems
{
    public interface IOrderItemStore
    {
        void Initialize( IEnumerable<OrderItem> orderItems );

        void Clear();

        OrderItemDto? Create( int orderItemId, OrderItemDto orderItemDto );

        OrderItemDto? GetById( int orderItemId );

        OrderItemDto? Update( OrderItemDto orderItemDto );

        bool Delete( int orderItemId );

        List<OrderItemDto> GetByOrderId( int orderId );

        List<OrderItemDto> GetByProductId( int productId );

        bool DeleteByOrderId( int orderId );

        bool Exists( int orderItemId );

        bool OrderHasItems( int orderId );

        bool ProductExistsInOrders( int productId );

        bool ProductExistsInOrder( int orderId, int productId );

        int GetOrderItemCount( int orderId );

        int GetOrderTotalQuantity( int orderId );

        decimal GetOrderTotalValue( int orderId );

        int GetTotalCount();
    }
}
