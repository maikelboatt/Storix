using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;

namespace Storix.Application.Services.OrderItems.Interfaces
{
    public interface IOrderItemWriteService
    {
        Task<DatabaseResult<OrderItemDto>> CreateOrderItemAsync( CreateOrderItemDto createOrderItemDto );

        Task<DatabaseResult<OrderItemDto>> UpdateOrderItemAsync( UpdateOrderItemDto updateOrderItemDto );

        Task<DatabaseResult> DeleteOrderItemAsync( int orderItemId );
    }
}
