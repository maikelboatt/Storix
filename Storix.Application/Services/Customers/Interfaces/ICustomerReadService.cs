using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Customers;
using Storix.Domain.Models;

namespace Storix.Application.Services.Customers.Interfaces
{
    public interface ICustomerReadService
    {
        /// <summary>
        /// Gets a customer by ID.
        /// </summary>
        Task<DatabaseResult<CustomerDto>> GetCustomerByIdAsync( int customerId );

        /// <summary>
        /// Gets a customer by Email.
        /// </summary>
        Task<DatabaseResult<CustomerDto?>> GetCustomerByEmailAsync( string email );

        /// <summary>
        /// Gets a customer by Phone.
        /// </summary>
        Task<DatabaseResult<CustomerDto?>> GetCustomerByPhoneAsync( string phone );

        /// <summary>
        /// Gets all customers in database.
        /// </summary>
        Task<DatabaseResult<IEnumerable<CustomerDto>>> GetAllCustomersAsync();

        /// <summary>
        /// Retrieves all active (non-deleted) customers from persistence and initializes the in-memory store (cache) with them.
        /// </summary>
        /// <returns></returns>
        Task<DatabaseResult<IEnumerable<CustomerDto>>> GetsAllActiveCustomersAsync();

        /// <summary>
        /// Retrieves all deleted customers from persistence.
        /// </summary>
        /// <returns></returns>
        Task<DatabaseResult<IEnumerable<Customer>>> GetsAllDeletedCustomersAsync();

        /// <summary>
        /// Gets the total counts of customers in persistence.
        /// </summary>
        Task<DatabaseResult<int>> GetTotalCountAsync();

        /// <summary>
        /// Gets the total number of active customers.
        /// </summary>
        Task<DatabaseResult<int>> GetActiveCountAsync();

        /// <summary>
        /// Gets the total number of deleted customers.
        /// </summary>
        Task<DatabaseResult<int>> GetDeletedCountAsync();

        /// <summary>
        /// Searches customers with a search-term (email,phone,name,address etc...).
        /// </summary>
        /// <param name="searchTerm"></param>
        Task<DatabaseResult<IEnumerable<CustomerDto>>> SearchAsync( string searchTerm );

        /// <summary>
        /// Gets a paged list of customers.
        /// </summary>
        Task<DatabaseResult<IEnumerable<CustomerDto>>> GetPagedAsync( int pageNumber, int pageSize );
    }
}
