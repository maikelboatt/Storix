using System.Threading.Tasks;
using Storix.Application.Common;

namespace Storix.Application.Services.Customers.Interfaces
{
    public interface ICustomerValidationService
    {
        Task<DatabaseResult<bool>> CustomerExistsAsync( int customerId, bool includeDeleted = false );

        Task<DatabaseResult<bool>> EmailExistsAsync( string email, int? excludedId = null, bool includeDeleted = false );

        Task<DatabaseResult<bool>> PhoneExistsAsync( string phone, int? excludedId = null, bool includeDeleted = false );

        Task<DatabaseResult<bool>> IsCustomerSoftDeleted( int customerId );

        Task<DatabaseResult> ValidateForDeletion( int customerId );

        Task<DatabaseResult> ValidateForHardDeletion( int customerId );

        Task<DatabaseResult> ValidateForRestore( int customerId );
    }
}
