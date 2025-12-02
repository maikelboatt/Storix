using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.DTO.Locations;
using Storix.Application.Enums;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Domain.Enums;

namespace Storix.Application.Services.Locations
{
    /// <summary>
    /// Main service for managing location operations with ISoftDeletable support and enhanced error handling.
    /// </summary>
    public class LocationService(
        ILocationReadService locationReadService,
        ILocationCacheReadService locationCacheReadService,
        ILocationValidationService locationValidationService,
        ILocationWriteService locationWriteService,
        ILogger<LocationService> logger ):ILocationService
    {
        #region Read Operations

        public async Task<DatabaseResult<LocationDto?>> GetLocationById( int locationId )
        {
            LocationDto? cached = locationCacheReadService.GetLocationByIdInCache(locationId);
            if (cached != null)
                return DatabaseResult<LocationDto?>.Success(cached);

            return (await locationReadService.GetLocationByIdAsync(locationId))!;
        }

        public async Task<DatabaseResult<LocationDto?>> GetLocationByName( string name )
        {
            LocationDto? cached = locationCacheReadService.GetLocationByNameInCache(name);
            if (cached != null)
                return DatabaseResult<LocationDto?>.Success(cached);

            return await locationReadService.GetLocationByNameAsync(name);
        }

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> GetLocationsByType( LocationType type )
        {
            List<LocationDto> cached = locationCacheReadService.GetLocationsByTypeInCache(type);
            if (cached.Count != 0)
                return DatabaseResult<IEnumerable<LocationDto>>.Success(cached);

            return await locationReadService.GetLocationsByTypeAsync(type);
        }

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> GetAllAsync() => await locationReadService.GetAllLocationsAsync();

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> GetAllActiveLocationsAsync() => await locationReadService.GetAllActiveLocationsAsync();

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> GetAllDeletedAsync() => await locationReadService.GetAllDeletedLocationsAsync();

        public async Task<DatabaseResult<int>> GetTotalCountAsync() => await locationReadService.GetTotalLocationCountAsync();

        public async Task<DatabaseResult<int>> GetTotalActiveCountAsync() => await locationReadService.GetActiveLocationCountAsync();

        public async Task<DatabaseResult<int>> GetTotalDeletedCountAsync() => await locationReadService.GetDeletedLocationCountAsync();

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> SearchLocationsAsync(
            string searchTerm,
            LocationType? type = null ) => await locationReadService.SearchAsync(searchTerm, type);

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> GetLocationsPagedAsync( int pageNumber, int pageSize ) =>
            await locationReadService.GetLocationPagedAsync(pageNumber, pageSize);

        public void RefreshStoreCache() => locationCacheReadService.RefreshStoreCache();

        #endregion

        #region Write Operations

        public async Task<DatabaseResult<LocationDto>> CreateLocationAsync( CreateLocationDto createDto ) =>
            await locationWriteService.CreateLocationAsync(createDto);

        public async Task<DatabaseResult<LocationDto>> UpdateLocationAsync( UpdateLocationDto updateDto ) =>
            await locationWriteService.UpdateLocationAsync(updateDto);

        public async Task<DatabaseResult> SoftDeleteLocationAsync( int locationId ) => await locationWriteService.SoftDeleteLocationAsync(locationId);

        public async Task<DatabaseResult> RestoreLocationAsync( int locationId ) => await locationWriteService.RestoreLocationAsync(locationId);

        // Permanent deletion - use with extreme caution
        [Obsolete("Use SoftDeleteLocationAsync instead. This method will be removed in a future version.")]
        public async Task<DatabaseResult> HardDeleteLocationAsync( int locationId ) => await locationWriteService.HardDeleteLocationAsync(locationId);

        #endregion

        #region Validation

        public async Task<DatabaseResult<bool>> LocationExistsAsync( int locationId, bool includeDeleted = false ) =>
            await locationValidationService.LocationExistsAsync(locationId, includeDeleted);

        public async Task<DatabaseResult<bool>> NameExistsAsync( string name, int? excludedId = null, bool includeDeleted = false ) =>
            await locationValidationService.LocationNameExistsAsync(name, excludedId, includeDeleted);

        public async Task<DatabaseResult<bool>> IsLocationSoftDeleted( int locationId ) => await locationValidationService.IsLocationSoftDeleted(locationId);

        public async Task<DatabaseResult> ValidateForDeletion( int locationId ) => await locationValidationService.ValidateForDeletion(locationId);

        public async Task<DatabaseResult> ValidateForHardDeletion( int locationId ) => await locationValidationService.ValidateForHardDeletion(locationId);

        public async Task<DatabaseResult> ValidateForRestore( int locationId ) => await locationValidationService.ValidateForRestore(locationId);

        #endregion

        #region Bulk Operations

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> BulkSoftDeleteAsync( IEnumerable<int> locationIds )
        {
            IEnumerable<int> enumerable = locationIds.ToList();
            logger.LogInformation("Starting bulk soft delete for {Count} locations", enumerable.Count());

            List<LocationDto> processedLocations = [];
            List<string> errors = [];

            foreach (int locationId in enumerable)
            {
                DatabaseResult result = await SoftDeleteLocationAsync(locationId);
                if (!result.IsSuccess)
                {
                    errors.Add($"Location {locationId}: {result.ErrorMessage}");
                    logger.LogWarning("Failed to soft delete location {LocationId}: {Error}", locationId, result.ErrorMessage);
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk soft delete completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<LocationDto>>.Failure(
                    $"Bulk soft delete completed with errors: {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation("Bulk soft delete completed successfully for {Count} locations", enumerable.Count());
            return DatabaseResult<IEnumerable<LocationDto>>.Success(processedLocations);
        }

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> BulkRestoreAsync( IEnumerable<int> locationIds )
        {
            IEnumerable<int> enumerable = locationIds.ToList();
            logger.LogInformation("Starting bulk restore for {Count} locations", enumerable.Count());

            List<LocationDto> processedLocations = [];
            List<string> errors = [];

            foreach (int locationId in enumerable)
            {
                DatabaseResult result = await RestoreLocationAsync(locationId);
                if (!result.IsSuccess)
                {
                    errors.Add($"Location {locationId}: {result.ErrorMessage}");
                    logger.LogWarning("Failed to restore location {LocationId}: {Error}", locationId, result.ErrorMessage);
                }
            }

            if (errors.Any())
            {
                string combinedErrors = string.Join("; ", errors);
                logger.LogWarning("Bulk restore completed with {ErrorCount} errors", errors.Count);
                return DatabaseResult<IEnumerable<LocationDto>>.Failure(
                    $"Bulk restore completed with errors: {combinedErrors}",
                    DatabaseErrorCode.PartialFailure);
            }

            logger.LogInformation("Bulk restore completed successfully for {Count} locations", enumerable.Count());
            return DatabaseResult<IEnumerable<LocationDto>>.Success(processedLocations);
        }

        #endregion

        #region Statistics

        public async Task<DatabaseResult<Dictionary<LocationType, int>>> GetLocationCountsByTypeAsync()
        {
            // Try cache first
            Dictionary<LocationType, int> cachedCounts = locationCacheReadService.GetLocationCountsByTypeInCache();
            if (cachedCounts.Values.Sum() > 0)
                return DatabaseResult<Dictionary<LocationType, int>>.Success(cachedCounts);

            // Fallback to database
            logger.LogInformation("Calculating location counts by type from database");

            DatabaseResult<IEnumerable<LocationDto>> allLocationsResult = await GetAllActiveLocationsAsync();

            if (!allLocationsResult.IsSuccess || allLocationsResult.Value == null)
                return DatabaseResult<Dictionary<LocationType, int>>.Failure(
                    allLocationsResult.ErrorMessage ?? "Failed to retrieve locations",
                    allLocationsResult.ErrorCode);

            Dictionary<LocationType, int> counts = allLocationsResult
                                                   .Value
                                                   .GroupBy(l => l.Type)
                                                   .ToDictionary(g => g.Key, g => g.Count());

            // Fill in missing types with 0
            foreach (LocationType type in Enum.GetValues(typeof(LocationType)))
            {
                if (!counts.ContainsKey(type))
                    counts[type] = 0;
            }

            return DatabaseResult<Dictionary<LocationType, int>>.Success(counts);
        }

        #endregion
    }
}
