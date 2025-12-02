using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Locations;
using Storix.Domain.Enums;

namespace Storix.Application.Services.Locations.Interfaces
{
    public interface ILocationReadService
    {
        Task<DatabaseResult<IEnumerable<LocationDto>>> GetLocationsByTypeAsync( LocationType type );

        Task<DatabaseResult<LocationDto?>> GetLocationByNameAsync( string name );

        Task<DatabaseResult<IEnumerable<LocationDto>>> GetLocationPagedAsync( int pageNumber, int pageSize );

        Task<DatabaseResult<int>> GetTotalLocationCountAsync();

        Task<DatabaseResult<LocationDto>> GetLocationByIdAsync( int locationId );

        Task<DatabaseResult<IEnumerable<LocationDto>>> GetAllLocationsAsync();

        Task<DatabaseResult<int>> GetActiveLocationCountAsync();

        Task<DatabaseResult<int>> GetDeletedLocationCountAsync();

        Task<DatabaseResult<IEnumerable<LocationDto>>> GetAllDeletedLocationsAsync();

        Task<DatabaseResult<IEnumerable<LocationDto>>> GetAllActiveLocationsAsync();

        Task<DatabaseResult<IEnumerable<LocationDto>>> SearchAsync( string searchTerm, LocationType? type = null );
    }
}
