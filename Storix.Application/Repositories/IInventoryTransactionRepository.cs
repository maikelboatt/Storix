using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface IInventoryTransactionRepository
    {
        /// <summary>
        ///     Check if a transaction exists by ID.
        /// </summary>
        Task<bool> ExistsAsync( int transactionId );

        /// <summary>
        ///     Gets the total count of transactions.
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        ///     Gets the count of transactions by type.
        /// </summary>
        Task<int> GetCountByTypeAsync( TransactionType type );

        /// <summary>
        ///     Gets the count of transactions for a specific product.
        /// </summary>
        Task<int> GetCountByProductIdAsync( int productId );

        /// <summary>
        ///     Gets the count of transactions for a specific location.
        /// </summary>
        Task<int> GetCountByLocationIdAsync( int locationId );

        /// <summary>
        ///     Gets a transaction by ID.
        /// </summary>
        Task<InventoryTransaction?> GetByIdAsync( int transactionId );

        /// <summary>
        ///     Gets all transactions for a specific product.
        /// </summary>
        Task<IEnumerable<InventoryTransaction>> GetByProductIdAsync( int productId );

        /// <summary>
        ///     Gets all transactions for a specific location.
        /// </summary>
        Task<IEnumerable<InventoryTransaction>> GetByLocationIdAsync( int locationId );

        /// <summary>
        ///     Gets transactions by type.
        /// </summary>
        Task<IEnumerable<InventoryTransaction>> GetByTypeAsync( TransactionType type );

        /// <summary>
        ///     Gets transactions by user who created them.
        /// </summary>
        Task<IEnumerable<InventoryTransaction>> GetByCreatedByAsync( int userId );

        /// <summary>
        ///     Gets transactions within a date range.
        /// </summary>
        Task<IEnumerable<InventoryTransaction>> GetByDateRangeAsync( DateTime startDate, DateTime endDate );

        ///     Gets transactions by reference.
        /// </summary>
        Task<IEnumerable<InventoryTransaction>> GetByReferenceAsync( string reference );

        /// <summary>
        ///     Gets all inventory transactions.
        /// </summary>
        Task<IEnumerable<InventoryTransaction>> GetAllAsync();

        /// <summary>
        ///     Gets a paged list of transactions.
        /// </summary>
        Task<IEnumerable<InventoryTransaction>> GetPagedAsync( int pageNumber, int pageSize );

        /// <summary>
        ///     Searches transactions with optional filters.
        /// </summary>
        Task<IEnumerable<InventoryTransaction>> SearchAsync(
            int? productId = null,
            int? locationId = null,
            TransactionType? type = null,
            int? createdBy = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? reference = null );

        /// <summary>
        ///     Creates a new inventory transaction and returns it with its generated ID.
        /// </summary>
        Task<InventoryTransaction> CreateAsync( InventoryTransaction transaction );

        /// <summary>
        ///     Permanently deletes a transaction by ID.
        ///     WARNING: This permanently removes the transaction record.
        /// </summary>
        Task<DatabaseResult> DeleteAsync( int transactionId );

        /// <summary>
        ///     Gets total quantity by transaction type for a product.
        /// </summary>
        Task<Dictionary<TransactionType, int>> GetQuantityByTypeForProductAsync( int productId );

        /// <summary>
        ///     Gets total value of transactions by type.
        /// </summary>
        Task<Dictionary<TransactionType, decimal>> GetTotalValueByTypeAsync();
    }
}
