using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Customers;
using Storix.Domain.Models;

namespace Storix.Application.Services.Customers.Interfaces
{
    public interface ICustomerService
    {
        Task<DatabaseResult<CustomerDto?>> GetCustomerById( int customerId );

        Task<DatabaseResult<CustomerDto?>> GetCustomerByEmail( string email );

        Task<DatabaseResult<CustomerDto?>> GetCustomerByPhone( string phone );

        Task<DatabaseResult<IEnumerable<CustomerDto>>> GetAllAsync();

        Task<DatabaseResult<IEnumerable<CustomerDto>>> GetAllActiveCustomersAsync();

        Task<DatabaseResult<IEnumerable<Customer>>> GetAllDeletedAsync();

        Task<DatabaseResult<int>> GetTotalCountAsync();

        Task<DatabaseResult<int>> GetTotalActiveCountAsync();

        Task<DatabaseResult<int>> GetTotalDeletedCountAsync();

        Task<DatabaseResult<IEnumerable<CustomerDto>>> SearchCustomersAsync( string searchTerm );

        Task<DatabaseResult<IEnumerable<CustomerDto>>> GetCustomersPagedAsync( int pageNumber, int pageSize );

        Task<DatabaseResult<CustomerDto>> CreateCustomerAsync( CreateCustomerDto createDto );

        Task<DatabaseResult<CustomerDto>> UpdateCustomerAsync( UpdateCustomerDto updateDto );

        Task<DatabaseResult> SoftDeleteCustomerAsync( int customerId );

        Task<DatabaseResult> RestoreCustomerAsync( int customerId );

        Task<DatabaseResult> HardDeleteCustomerAsync( int customerId );

        Task<DatabaseResult<bool>> CustomerExistsAsync( int customerId, bool includeDeleted = false );

        Task<DatabaseResult<bool>> EmailExistAsync( string email, int? excludedId = null, bool includeDeleted = false );

        Task<DatabaseResult<bool>> PhoneExistAsync( string email, int? excludedId = null, bool includeDeleted = false );

        Task<DatabaseResult<bool>> IsCustomerSoftDeleted( int customerId );

        Task<DatabaseResult> ValidateForDeletion( int customerId );

        Task<DatabaseResult> ValidateForHardDeletion( int customerId );

        Task<DatabaseResult> ValidateForRestore( int customerId );

        Task<DatabaseResult<IEnumerable<CustomerDto>>> BulkSoftDeleteAsync( IEnumerable<int> customerIds );

        Task<DatabaseResult<IEnumerable<CustomerDto>>> BulkRestoreAsync( IEnumerable<int> customerIds );
    }
}
