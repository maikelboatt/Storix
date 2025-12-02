using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;

namespace Storix.Application.Services.Orders.Interfaces
{
    public interface IOrderWriteService
    {
        Task<DatabaseResult<OrderDto>> CreateOrderAsync( CreateOrderDto createOrderDto );

        Task<DatabaseResult<OrderDto>> UpdateOrderAsync( UpdateOrderDto updateOrderDto );

        Task<DatabaseResult> ActivateOrderAsync( int orderId );

        Task<DatabaseResult> FulfillOrderAsync( int orderId );

        Task<DatabaseResult> CompleteOrderAsync( int orderId );

        Task<DatabaseResult> CancelOrderAsync( int orderId, string? reason = null );

        Task<DatabaseResult> DeleteOrderAsync( int orderId );
    }
}
