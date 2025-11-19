using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface ICustomerRepository
    {
        /// <summary>
        ///     Check if a customer exists by ID.
        /// </summary>
        Task<bool> ExistsAsync( int customerId, bool includeDeleted = false );

        /// <summary>
        ///     Check if a customer exists by email.
        /// </summary>
        Task<bool> ExistsByEmailAsync( string email, int? excludeUserId = null, bool includeDeleted = false );

        /// <summary>
        ///     Check if a customer exists by phone.
        /// </summary>
        Task<bool> ExistsByPhoneAsync( string phone, int? excludeUserId = null, bool includeDeleted = false );

        /// <summary>
        ///     Gets the total count of customers (including deleted).
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        ///     Gets the count of active customers.
        /// </summary>
        Task<int> GetActiveCountAsync();

        /// <summary>
        ///     Gets the count of deleted customers.
        /// </summary>
        Task<int> GetDeletedCountAsync();

        /// <summary>
        ///     Gets a customer by ID.
        /// </summary>
        Task<Customer?> GetByIdAsync( int customerId, bool includeDeleted = true );

        /// <summary>
        ///     Gets all customers (includes deleted).
        /// </summary>
        Task<IEnumerable<Customer>> GetAllAsync( bool includeDeleted = true );

        /// <summary>
        ///     Gets a customer by email (includes deleted).
        /// </summary>
        Task<Customer?> GetByEmailAsync( string email );

        /// <summary>
        ///     Gets a customer by phone (includes deleted).
        /// </summary>
        Task<Customer?> GetByPhoneAsync( string phone );

        /// <summary>
        ///     Gets a paged list of customers (includes deleted).
        /// </summary>
        Task<IEnumerable<Customer>> GetPagedAsync( int pageNumber, int pageSize );

        /// <summary>
        ///     Searches customers with optional filters (includes deleted).
        /// </summary>
        Task<IEnumerable<Customer>> SearchAsync(
            string? searchTerm = null,
            bool? isDeleted = null );

        /// <summary>
        ///     Creates a new customer and returns it with its generated ID.
        /// </summary>
        Task<Customer> CreateAsync( Customer customer );

        /// <summary>
        ///     Updates an existing customer.
        /// </summary>
        Task<Customer> UpdateAsync( Customer customer );

        /// <summary>
        ///     Soft deletes a customer by ID.
        /// </summary>
        Task<DatabaseResult> SoftDeleteAsync( int customerId );

        /// <summary>
        ///     Restores a soft-deleted customer.
        /// </summary>
        Task<DatabaseResult> RestoreAsync( int customerId );

        /// <summary>
        ///     Permanently deletes a customer by ID.
        ///     WARNING: This permanently removes the customer from the database.
        /// </summary>
        Task<DatabaseResult> HardDeleteAsync( int customerId );
    }
}
