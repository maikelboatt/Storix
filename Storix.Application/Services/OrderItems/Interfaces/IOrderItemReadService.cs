using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;

namespace Storix.Application.Services.OrderItems.Interfaces
{
    public interface IOrderItemReadService
    {
        OrderItemDto? GetOrderItemById( int orderItemId );

        Task<DatabaseResult<IEnumerable<OrderItemDto>>> GetOrderItemsByOrderIdAsync( int orderId );

        Task<DatabaseResult<IEnumerable<OrderItemDto>>> GetOrderItemsByProductIdAsync( int productId );
    }
}
