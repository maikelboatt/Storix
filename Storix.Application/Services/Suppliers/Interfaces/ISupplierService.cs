using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Suppliers;
using Storix.Domain.Models;

namespace Storix.Application.Services.Suppliers.Interfaces
{
    public interface ISupplierService
    {
        Task<DatabaseResult<SupplierDto?>> GetSupplierById( int supplierId );

        Task<DatabaseResult<SupplierDto?>> GetSupplierByEmail( string email );

        Task<DatabaseResult<SupplierDto?>> GetSupplierByPhone( string phone );

        Task<DatabaseResult<IEnumerable<SupplierDto>>> GetAllAsync();

        Task<DatabaseResult<IEnumerable<SupplierDto>>> GetAllActiveSuppliersAsync();

        Task<DatabaseResult<IEnumerable<Supplier>>> GetAllDeletedAsync();

        Task<DatabaseResult<int>> GetTotalCountAsync();

        Task<DatabaseResult<int>> GetTotalActiveCountAsync();

        Task<DatabaseResult<int>> GetTotalDeletedCountAsync();

        Task<DatabaseResult<IEnumerable<SupplierDto>>> SearchSuppliersAsync( string searchTerm );

        Task<DatabaseResult<IEnumerable<SupplierDto>>> GetSuppliersPagedAsync( int pageNumber, int pageSize );

        Task<DatabaseResult<SupplierDto>> CreateSupplierAsync( CreateSupplierDto createDto );

        Task<DatabaseResult<SupplierDto>> UpdateSupplierAsync( UpdateSupplierDto updateDto );

        Task<DatabaseResult> SoftDeleteSupplierAsync( int supplierId );

        Task<DatabaseResult> RestoreSupplierAsync( int supplierId );

        Task<DatabaseResult> HardDeleteSupplierAsync( int supplierId );

        Task<DatabaseResult<bool>> SupplierExistsAsync( int supplierId, bool includeDeleted = false );

        Task<DatabaseResult<bool>> EmailExistAsync( string email, int? excludedId = null, bool includeDeleted = false );

        Task<DatabaseResult<bool>> PhoneExistAsync( string email, int? excludedId = null, bool includeDeleted = false );

        Task<DatabaseResult<bool>> IsSupplierSoftDeleted( int supplierId );

        Task<DatabaseResult> ValidateForDeletion( int supplierId );

        Task<DatabaseResult> ValidateForHardDeletion( int supplierId );

        Task<DatabaseResult> ValidateForRestore( int supplierId );

        Task<DatabaseResult<IEnumerable<SupplierDto>>> BulkSoftDeleteAsync( IEnumerable<int> supplierIds );

        Task<DatabaseResult<IEnumerable<SupplierDto>>> BulkRestoreAsync( IEnumerable<int> supplierIds );
    }
}
