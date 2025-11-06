using System.Collections.Generic;
using Storix.Application.DTO.Suppliers;

namespace Storix.Application.Services.Suppliers.Interfaces
{
    public interface ISupplierCacheReadService
    {
        SupplierDto? GetSupplierByIdInCache( int supplierId );

        SupplierDto? GetSupplierByEmailInCache( string email );

        SupplierDto? GetSupplierByPhoneInCache( string phone );

        IEnumerable<SupplierDto> GetAllActiveSuppliersInCache();

        IEnumerable<SupplierDto> SearchSuppliersInCache( string searchTerm );

        bool SupplierExistsInCache( int supplierId );

        bool EmailExistsInCache( string email );

        bool PhoneExistsInCache( string phone );

        int GetSupplierCountInCache();

        int GetActiveSupplierCountInCache();

        void RefreshStoreCache();
    }
}
