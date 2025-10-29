using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Suppliers;
using Storix.Domain.Models;

namespace Storix.Application.Services.Suppliers.Interfaces
{
    public interface ISupplierReadService
    {
        /// <summary>
        /// Gets supplier by ID.
        /// </summary>
        Task<DatabaseResult<SupplierDto?>> GetSupplierByIdAsync( int supplierId );

        /// <summary>
        /// Gets supplier by Email.
        /// </summary>
        Task<DatabaseResult<SupplierDto?>> GetSupplierByEmailAsync( string email );

        /// <summary>
        /// Gets a supplier by Phone.
        /// </summary>
        Task<DatabaseResult<SupplierDto?>> GetSupplierByPhoneAsync( string phone );

        /// <summary>
        /// Gets all suppliers in database.
        /// </summary>
        Task<DatabaseResult<IEnumerable<SupplierDto>>> GetAllAsync();

        /// <summary>
        /// Retrieves all active (non-deleted) suppliers from persistence
        /// and initializes the in-memory store (cache) with them.
        /// </summary>
        /// <returns></returns>
        Task<DatabaseResult<IEnumerable<Supplier>>> GetsAllActiveSuppliersAsync();

        /// <summary>
        /// Retrieves all deleted suppliers from persistence and initializes the in-memory store (cache) with them.
        /// </summary>
        /// <returns></returns>
        Task<DatabaseResult<IEnumerable<Supplier>>> GetsAllDeletedSuppliersAsync();

        /// <summary>
        /// Gets the total number of suppliers in persistence.
        /// </summary>
        Task<DatabaseResult<int>> GetTotalCountAsync();

        /// <summary>
        /// Gets the total number of active suppliers.
        /// </summary>
        Task<DatabaseResult<int>> GetActiveCountAsync();

        /// <summary>
        /// Gets the total number of deleted suppliers.
        /// </summary>
        Task<DatabaseResult<int>> GetDeletedCountAsync();

        /// <summary>
        /// Searches suppliers with a search-term (email,phone,name,address etc...).
        /// </summary>
        /// <param name="searchTerm"></param>
        Task<DatabaseResult<IEnumerable<SupplierDto>>> SearchAsync( string searchTerm );

        /// <summary>
        /// Gets a paged list of suppliers.
        /// </summary>
        Task<DatabaseResult<IEnumerable<SupplierDto>>> GetPagedAsync( int pageNumber, int pageSize );
    }
}
