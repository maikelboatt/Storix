using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;

namespace Storix.Application.Services.OrderItems.Interfaces
{
    public interface IOrderItemService
    {
        OrderItemDto? GetOrderItemById( int orderItemId );

        Task<DatabaseResult<IEnumerable<OrderItemDto>>> GetOrderItemsByOrderIdAsync( int orderId );

        Task<DatabaseResult<IEnumerable<OrderItemDto>>> GetOrderItemsByProductIdAsync( int productId );

        Task<DatabaseResult<OrderItemDto>> CreateOrderItemAsync( CreateOrderItemDto createOrderItemDto );

        Task<DatabaseResult<OrderItemDto>> UpdateOrderItemAsync( UpdateOrderItemDto updateOrderItemDto );

        Task<DatabaseResult> DeleteOrderItemAsync( int orderItemId );

        Task<DatabaseResult<bool>> OrderItemExistsAsync( int orderItemId );

        Task<DatabaseResult<bool>> ProductExistsInOrdersAsync( int productId );
    }
}
