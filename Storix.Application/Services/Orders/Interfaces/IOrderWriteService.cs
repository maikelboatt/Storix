using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Domain.Enums;

namespace Storix.Application.Services.Orders.Interfaces
{
    public interface IOrderWriteService
    {
        Task<DatabaseResult<OrderDto>> CreateOrderAsync( CreateOrderDto createOrderDto );

        Task<DatabaseResult<OrderDto>> UpdateOrderAsync( UpdateOrderDto updateOrderDto );

        Task<DatabaseResult> TransferOrderToLocationAsync( int orderId, int newLocationId, string? reason = null );

        Task<DatabaseResult> RevertToDraftOrderAsync( int orderId, OrderStatus originalStatus );

        Task<DatabaseResult> ActivateOrderAsync( int orderId, OrderStatus originalStatus );

        Task<DatabaseResult> FulfillOrderAsync( int orderId, OrderStatus originalStatus );

        Task<DatabaseResult> CompleteOrderAsync( int orderId, OrderStatus originalStatus );

        Task<DatabaseResult> CancelOrderAsync( int orderId, OrderStatus originalStatus, string? reason = null );

        Task<DatabaseResult> DeleteOrderAsync( int orderId );
    }
}
