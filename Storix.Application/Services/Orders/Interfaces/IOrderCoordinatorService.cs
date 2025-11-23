using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;

namespace Storix.Application.Services.Orders.Interfaces
{
    public interface IOrderCoordinatorService
    {
        Task<DatabaseResult<OrderDto>> UpdateOrderWithItemsAsync(
            UpdateOrderDto updateOrderDto,
            IEnumerable<OrderItemUpdateDto> orderItems );
    }
}
