using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Locations;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Application.Stores.Locations;
using Storix.Domain.Enums;

namespace Storix.Application.Services.Locations
{
    public class LocationCacheReadService(
        ILocationStore locationStore,
        ILocationReadService readService,
        ILogger<LocationCacheReadService> logger ):ILocationCacheReadService
    {
        public LocationDto? GetLocationByIdInCache( int locationId )
        {
            logger.LogInformation("Retrieving location with ID: {LocationId} from cache", locationId);

            return locationStore.GetById(locationId);
        }

        public LocationDto? GetLocationByNameInCache( string name )
        {
            logger.LogInformation("Retrieving location with Name: {LocationName} from cache", name);

            return locationStore.GetByName(name);
        }

        public List<LocationDto> GetLocationsByTypeInCache( LocationType type )
        {
            logger.LogInformation("Retrieving locations of type: {LocationType} from cache", type);

            return locationStore.GetByType(type);
        }

        public List<LocationDto> GetAllLocationsInCache()
        {
            logger.LogInformation("Retrieving all locations from cache");

            return locationStore.GetAll();
        }

        public List<LocationDto> GetAllActiveLocationsInCache()
        {
            logger.LogInformation("Retrieving all active locations from cache");

            return locationStore.GetActiveLocations();
        }

        public IEnumerable<LocationDto> SearchLocationsInCache( string searchTerm, LocationType? type = null )
        {
            logger.LogInformation(
                "Searching locations in cache with term '{SearchTerm}' and type '{Type}'",
                searchTerm,
                type?.ToString() ?? "All");

            return locationStore.Search(searchTerm, type);
        }

        public bool LocationExistsInCache( int locationId ) => locationStore.Exists(locationId);

        public bool NameExistsInCache( string name ) => locationStore.NameExists(name);

        public int GetLocationCountInCache() => locationStore.GetCount();

        public int GetActiveLocationCountInCache() => locationStore.GetActiveCount();

        public int GetLocationCountByTypeInCache( LocationType type ) => locationStore.GetCountByType(type);

        public Dictionary<LocationType, int> GetLocationCountsByTypeInCache() => locationStore.GetLocationCountsByType();

        public void RefreshStoreCache()
        {
            logger.LogInformation("Initiating location store cache refresh (active locations only)");
            _ = Task.Run(async () =>
            {
                try
                {
                    // Gets only active locations from database
                    DatabaseResult<IEnumerable<LocationDto>> result = await readService.GetAllActiveLocationsAsync();

                    if (result is { IsSuccess: true, Value: not null })
                    {
                        logger.LogInformation(
                            "Location store cache refreshed successfully with {Count} active locations",
                            result.Value.Count());
                    }
                    else
                    {
                        logger.LogWarning("Failed to refresh location store cache: {Error}", result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception occurred while refreshing location store cache");
                }
            });
        }
    }
}
