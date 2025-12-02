using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Repositories
{
    public interface ILocationRepository
    {
        /// <summary>
        ///     Check if a location exists by ID.
        /// </summary>
        Task<bool> ExistsAsync( int locationId, bool includeDeleted = false );

        /// <summary>
        ///     Check if a location exists by name.
        /// </summary>
        Task<bool> ExistsByNameAsync( string name, int? excludeLocationId = null, bool includeDeleted = false );

        /// <summary>
        ///     Gets the total count of locations (including deleted).
        /// </summary>
        Task<int> GetTotalCountAsync();

        /// <summary>
        ///     Gets the count of active locations.
        /// </summary>
        Task<int> GetActiveCountAsync();

        /// <summary>
        ///     Gets the count of deleted locations.
        /// </summary>
        Task<int> GetDeletedCountAsync();

        /// <summary>
        ///     Gets the count of locations by type.
        /// </summary>
        Task<int> GetCountByTypeAsync( LocationType type, bool includeDeleted = false );

        /// <summary>
        ///     Gets a location by ID (includes deleted).
        /// </summary>
        Task<Location?> GetByIdAsync( int locationId, bool includeDeleted = true );

        /// <summary>
        ///     Gets all locations (includes deleted).
        /// </summary>
        Task<IEnumerable<Location>> GetAllAsync( bool includeDeleted = true );

        /// <summary>
        ///     Gets a location by name (includes deleted).
        /// </summary>
        Task<Location?> GetByNameAsync( string name, bool includeDeleted = true );

        /// <summary>
        ///     Gets locations by type (includes deleted).
        /// </summary>
        Task<IEnumerable<Location>> GetByTypeAsync( LocationType type, bool includeDeleted = true );

        /// <summary>
        ///     Gets a paged list of locations (includes deleted).
        /// </summary>
        Task<IEnumerable<Location>> GetPagedAsync( int pageNumber, int pageSize );

        /// <summary>
        ///     Searches locations with optional filters (includes deleted).
        /// </summary>
        Task<IEnumerable<Location>> SearchAsync(
            string? searchTerm = null,
            LocationType? type = null,
            bool? isDeleted = null );

        /// <summary>
        ///     Creates a new location and returns it with its generated ID.
        /// </summary>
        Task<Location> CreateAsync( Location location );

        /// <summary>
        ///     Updates an existing location.
        /// </summary>
        Task<Location> UpdateAsync( Location location );

        /// <summary>
        ///     Soft deletes a location by ID.
        /// </summary>
        Task<DatabaseResult> SoftDeleteAsync( int locationId );

        /// <summary>
        ///     Restores a soft-deleted location.
        /// </summary>
        Task<DatabaseResult> RestoreAsync( int locationId );

        /// <summary>
        ///     Permanently deletes a location by ID.
        ///     WARNING: This permanently removes the location from the database.
        /// </summary>
        Task<DatabaseResult> HardDeleteAsync( int locationId );
    }
}
