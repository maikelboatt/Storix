using System.Threading.Tasks;
using Storix.Application.Common;

namespace Storix.Application.Services.Orders.Interfaces
{
    public interface IOrderValidationService
    {
        Task<DatabaseResult<bool>> OrderExistsAsync( int orderId );

        Task<DatabaseResult> ValidateForActivation( int orderId );

        Task<DatabaseResult> ValidateForCompletion( int orderId );

        Task<DatabaseResult> ValidForCancellation( int orderId );

        Task<DatabaseResult> ValidateForDeletion( int orderId );

        Task<DatabaseResult<bool>> SupplierHasOrdersAsync( int supplierId, bool activeOnly = false );

        Task<DatabaseResult<bool>> CustomerHasOrdersAsync( int customerId, bool activeOnly = false );
    }
}
