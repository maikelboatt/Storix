using System.Collections.Generic;
using Storix.Application.DTO.Locations;
using Storix.Domain.Enums;

namespace Storix.Application.Services.Locations.Interfaces
{
    public interface ILocationCacheReadService
    {
        LocationDto? GetLocationByIdInCache( int locationId );

        LocationDto? GetLocationByNameInCache( string name );

        List<LocationDto> GetLocationsByTypeInCache( LocationType type );

        List<LocationDto> GetAllLocationsInCache();

        List<LocationDto> GetAllActiveLocationsInCache();

        IEnumerable<LocationDto> SearchLocationsInCache( string searchTerm, LocationType? type = null );

        bool LocationExistsInCache( int locationId );

        bool NameExistsInCache( string name );

        int GetLocationCountInCache();

        int GetActiveLocationCountInCache();

        int GetLocationCountByTypeInCache( LocationType type );

        Dictionary<LocationType, int> GetLocationCountsByTypeInCache();

        void RefreshStoreCache();
    }
}
