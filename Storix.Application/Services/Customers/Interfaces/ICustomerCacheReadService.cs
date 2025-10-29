using System.Collections.Generic;
using Storix.Application.DTO.Customers;

namespace Storix.Application.Services.Customers.Interfaces
{
    public interface ICustomerCacheReadService
    {
        CustomerDto? GetCustomerByIdInCache( int customerId );

        CustomerDto? GetCustomerByEmailInCache( string email );

        CustomerDto? GetCustomerByPhoneInCache( string phone );

        IEnumerable<CustomerDto> SearchCustomersInCache( string searchTerm );

        bool CustomerExistsInCache( int customerId );

        bool EmailExistsInCache( string email );

        bool PhoneExistsInCache( string phone );

        int GetCustomerCountInCache();

        int GetActiveCustomerCountInCache();

        void RefreshStoreCache();
    }
}
