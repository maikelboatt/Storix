using System.Threading.Tasks;
using Storix.Application.Common;

namespace Storix.Application.Services.Suppliers.Interfaces
{
    public interface ISupplierValidationService
    {
        Task<DatabaseResult<bool>> SupplierExistsAsync( int supplierId, bool includeDeleted = false );

        Task<DatabaseResult<bool>> EmailExistsAsync( string email, int? excludedId = null, bool includeDeleted = false );

        Task<DatabaseResult<bool>> PhoneExistsAsync( string phone, int? excludedId = null, bool includeDeleted = false );

        Task<DatabaseResult<bool>> IsSupplierSoftDeleted( int supplierId );

        Task<DatabaseResult> ValidateForDeletion( int supplierId );

        Task<DatabaseResult> ValidateForHardDeletion( int supplierId );

        Task<DatabaseResult> ValidateForRestore( int supplierId );
    }
}
