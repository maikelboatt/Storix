using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Customers;

namespace Storix.Application.Services.Customers.Interfaces
{
    public interface ICustomerWriteService
    {
        Task<DatabaseResult<CustomerDto>> CreateCustomerAsync( CreateCustomerDto createCustomerDto );

        Task<DatabaseResult<CustomerDto>> UpdateCustomerAsync( UpdateCustomerDto updateCustomerDto );

        Task<DatabaseResult> SoftDeleteCustomerAsync( int customerId );

        Task<DatabaseResult> RestoreCustomerAsync( int customerId );

        Task<DatabaseResult> HardDeleteCustomerAsync( int customerId );
    }
}
