using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;

namespace Storix.Application.Managers.Interfaces
{
    public interface IOrderItemManager
    {
        Task<DatabaseResult<IEnumerable<OrderItemDto>>> CreateBulkOrderItemsAsync(
            IEnumerable<CreateOrderItemDto> createOrderItemDtos );

        Task<DatabaseResult> DeleteOrderItemsByOrderIdAsync( int orderId );

        Task<DatabaseResult<int>> GetOrderItemCountAsync( int orderId );

        Task<DatabaseResult<int>> GetOrderTotalQuantityAsync( int orderId );

        Task<DatabaseResult<decimal>> GetOrderTotalValueAsync( int orderId );

        Task<DatabaseResult<OrderSummaryDto>> GetOrderSummaryAsync( int orderId );
    }
}
