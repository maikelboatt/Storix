using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface ISupplierRepository
    {
        /// <summary>
        ///     Check if a supplier exists by ID.
        /// </summary>
        Task<bool> ExistsAsync( int supplierId, bool includeDeleted = false );

        /// <summary>
        ///     Check if a supplier exists by email.
        /// </summary>
        Task<bool> ExistsByEmailAsync( string email, int? excludeUserId = null, bool includeDeleted = false );

        /// <summary>
        ///     Check if a supplier exists by phone.
        /// </summary>
        Task<bool> ExistsByPhoneAsync( string phone, int? excludeUserId = null, bool includeDeleted = false );

        /// <summary>
        ///     Gets the total count of suppliers (including deleted).
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        ///     Gets the count of active suppliers.
        /// </summary>
        Task<int> GetActiveCountAsync();

        /// <summary>
        ///     Gets the count of deleted suppliers.
        /// </summary>
        Task<int> GetDeletedCountAsync();

        /// <summary>
        ///     Gets a supplier by ID (includes deleted).
        /// </summary>
        Task<Supplier?> GetByIdAsync( int supplierId );

        /// <summary>
        ///     Gets all suppliers (includes deleted).
        /// </summary>
        Task<IEnumerable<Supplier>> GetAllAsync();

        /// <summary>
        ///     Gets a supplier by email (includes deleted).
        /// </summary>
        Task<Supplier?> GetByEmailAsync( string email );

        /// <summary>
        ///     Gets a supplier by phone (includes deleted).
        /// </summary>
        Task<Supplier?> GetByPhoneAsync( string phone );

        /// <summary>
        ///     Gets a paged list of suppliers (includes deleted).
        /// </summary>
        Task<IEnumerable<Supplier>> GetPagedAsync( int pageNumber, int pageSize );

        /// <summary>
        ///     Searches suppliers with optional filters (includes deleted).
        /// </summary>
        Task<IEnumerable<Supplier>> SearchAsync(
            string? searchTerm = null,
            bool? isDeleted = null );

        /// <summary>
        ///     Creates a new supplier and returns it with its generated ID.
        /// </summary>
        Task<Supplier> CreateAsync( Supplier supplier );

        /// <summary>
        ///     Updates an existing supplier.
        /// </summary>
        Task<Supplier> UpdateAsync( Supplier supplier );

        /// <summary>
        ///     Soft deletes a supplier by ID.
        /// </summary>
        Task<DatabaseResult> SoftDeleteAsync( int supplierId );

        /// <summary>
        ///     Restores a soft-deleted supplier.
        /// </summary>
        Task<DatabaseResult> RestoreAsync( int supplierId );

        /// <summary>
        ///     Permanently deletes a supplier by ID.
        ///     WARNING: This permanently removes the supplier from the database.
        /// </summary>
        Task<DatabaseResult> HardDeleteAsync( int supplierId );
    }
}
