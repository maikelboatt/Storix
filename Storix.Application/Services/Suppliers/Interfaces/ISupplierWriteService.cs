using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Suppliers;

namespace Storix.Application.Services.Suppliers.Interfaces
{
    public interface ISupplierWriteService
    {
        Task<DatabaseResult<SupplierDto>> CreateSupplierAsync( CreateSupplierDto createSupplierDto );

        Task<DatabaseResult<SupplierDto>> UpdateSupplierAsync( UpdateSupplierDto updateSupplierDto );

        Task<DatabaseResult> SoftDeleteSupplierAsync( int supplierId );

        Task<DatabaseResult> RestoreSupplierAsync( int supplierId );

        Task<DatabaseResult> HardDeleteSupplierAsync( int supplierId );
    }
}
