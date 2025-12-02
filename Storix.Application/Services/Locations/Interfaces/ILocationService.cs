using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Locations;
using Storix.Domain.Enums;

namespace Storix.Application.Services.Locations.Interfaces
{
    public interface ILocationService
    {
        Task<DatabaseResult<LocationDto?>> GetLocationById( int locationId );

        Task<DatabaseResult<LocationDto?>> GetLocationByName( string name );

        Task<DatabaseResult<IEnumerable<LocationDto>>> GetLocationsByType( LocationType type );

        Task<DatabaseResult<IEnumerable<LocationDto>>> GetAllAsync();

        Task<DatabaseResult<IEnumerable<LocationDto>>> GetAllActiveLocationsAsync();

        Task<DatabaseResult<IEnumerable<LocationDto>>> GetAllDeletedAsync();

        Task<DatabaseResult<int>> GetTotalCountAsync();

        Task<DatabaseResult<int>> GetTotalActiveCountAsync();

        Task<DatabaseResult<int>> GetTotalDeletedCountAsync();

        Task<DatabaseResult<IEnumerable<LocationDto>>> SearchLocationsAsync(
            string searchTerm,
            LocationType? type = null );

        Task<DatabaseResult<IEnumerable<LocationDto>>> GetLocationsPagedAsync( int pageNumber, int pageSize );

        void RefreshStoreCache();

        Task<DatabaseResult<LocationDto>> CreateLocationAsync( CreateLocationDto createDto );

        Task<DatabaseResult<LocationDto>> UpdateLocationAsync( UpdateLocationDto updateDto );

        Task<DatabaseResult> SoftDeleteLocationAsync( int locationId );

        Task<DatabaseResult> RestoreLocationAsync( int locationId );

        Task<DatabaseResult> HardDeleteLocationAsync( int locationId );

        Task<DatabaseResult<bool>> LocationExistsAsync( int locationId, bool includeDeleted = false );

        Task<DatabaseResult<bool>> NameExistsAsync( string name, int? excludedId = null, bool includeDeleted = false );

        Task<DatabaseResult<bool>> IsLocationSoftDeleted( int locationId );

        Task<DatabaseResult> ValidateForDeletion( int locationId );

        Task<DatabaseResult> ValidateForHardDeletion( int locationId );

        Task<DatabaseResult> ValidateForRestore( int locationId );

        Task<DatabaseResult<IEnumerable<LocationDto>>> BulkSoftDeleteAsync( IEnumerable<int> locationIds );

        Task<DatabaseResult<IEnumerable<LocationDto>>> BulkRestoreAsync( IEnumerable<int> locationIds );

        Task<DatabaseResult<Dictionary<LocationType, int>>> GetLocationCountsByTypeAsync();
    }
}
