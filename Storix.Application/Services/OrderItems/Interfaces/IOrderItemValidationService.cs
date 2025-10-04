using System.Threading.Tasks;
using Storix.Application.Common;

namespace Storix.Application.Services.OrderItems.Interfaces
{
    public interface IOrderItemValidationService
    {
        Task<DatabaseResult<bool>> OrderItemExistsAsync( int orderItemId );

        Task<DatabaseResult> ValidateForCreation( int orderId, int productId );

        Task<DatabaseResult> ValidateForUpdate( int orderItemId );

        Task<DatabaseResult> ValidateForDeletion( int orderItemId );

        Task<DatabaseResult<bool>> ProductExistsInOrdersAsync( int productId );
    }
}
