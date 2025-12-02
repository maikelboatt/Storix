using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Locations;
using Storix.Application.Enums;
using Storix.Application.Repositories;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Application.Stores.Locations;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Services.Locations
{
    /// <summary>
    ///     Service responsible for location read operations with ISoftDeletable support.
    /// </summary>
    public class LocationReadService(
        ILocationRepository locationRepository,
        ILocationStore locationStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<LocationReadService> logger ):ILocationReadService
    {
        public async Task<DatabaseResult<IEnumerable<LocationDto>>> GetLocationsByTypeAsync( LocationType type )
        {
            DatabaseResult<IEnumerable<Location>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.GetByTypeAsync(type, false),
                $"Retrieving locations of type {type}"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {LocationCount} locations of type {LocationType}.",
                    result.Value.Count(),
                    type);
                IEnumerable<LocationDto> locationDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<LocationDto>>.Success(locationDtos);
            }

            logger.LogWarning("Failed to retrieve locations of type {LocationType}: {ErrorMessage}", type, result.ErrorMessage);
            return DatabaseResult<IEnumerable<LocationDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<LocationDto?>> GetLocationByNameAsync( string name )
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                logger.LogWarning("Null or empty name provided");
                return DatabaseResult<LocationDto?>.Failure(
                    "Name cannot be null or empty.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Location?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.GetByNameAsync(name),
                $"Retrieving location by name {name}",
                enableRetry: false);

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve location by Name {Name}: {ErrorMessage}", name, result.ErrorMessage);
                return DatabaseResult<LocationDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("Location with Name {Name} not found", name);
                return DatabaseResult<LocationDto?>.Failure("Location not found", DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved location with Name {Email}", name);
            return DatabaseResult<LocationDto?>.Success(result.Value.ToDto());
        }

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> GetLocationPagedAsync( int pageNumber, int pageSize )
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                string errorMsg = pageNumber <= 0
                    ? "Page number must be positive"
                    : "Page size must be positive";
                logger.LogWarning("Invalid pagination parameters: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
                return DatabaseResult<IEnumerable<LocationDto>>.Failure(errorMsg, DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Location>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.GetPagedAsync(pageNumber, pageSize),
                $"Getting locations page {pageNumber} with size {pageSize}."
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved page {PageNumber} of locations ({LocationCount} items.)",
                    pageNumber,
                    result.Value.Count());

                IEnumerable<LocationDto> locationDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<LocationDto>>.Success(locationDtos);
            }

            logger.LogWarning(
                "Failed to retrieve locations page {PageNumber}: {ErrorMessage}",
                pageNumber,
                result.ErrorMessage);
            return DatabaseResult<IEnumerable<LocationDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetTotalLocationCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                locationRepository.GetTotalCountAsync,
                "Getting total location count",
                false
            );

            if (result.IsSuccess)
                logger.LogInformation("Total location count: {LocationCount}.", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<LocationDto>> GetLocationByIdAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<LocationDto>.Failure("Location ID must be positive integer", DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Location?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.GetByIdAsync(locationId),
                $"Retrieving location with ID {locationId}"
            );

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to retrieve location {LocationId}: {ErrorMessage}", locationId, result.ErrorMessage);
                return DatabaseResult<LocationDto>.Failure(result.ErrorMessage!, result.ErrorCode);
            }

            if (result.Value == null)
            {
                logger.LogWarning("Location with ID {LocationId} not found", locationId);
                return DatabaseResult<LocationDto>.Failure("Location not found", DatabaseErrorCode.NotFound);
            }

            logger.LogInformation("Successfully retrieved location with ID {LocationId}", locationId);
            return DatabaseResult<LocationDto>.Success(result.Value.ToDto());
        }

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> GetAllLocationsAsync()
        {
            DatabaseResult<IEnumerable<Location>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.GetAllAsync(true),
                "Retrieving all locations"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                IEnumerable<LocationDto> locationDtos = result.Value.ToDto();

                logger.LogInformation("Successfully loaded {LocationCount} locations.", result.Value.Count());

                return DatabaseResult<IEnumerable<LocationDto>>.Success(locationDtos);
            }

            logger.LogWarning("Failed to retrieve locations: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<LocationDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetActiveLocationCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                locationRepository.GetActiveCountAsync,
                "Getting active location count",
                false
            );

            if (result.IsSuccess)
                logger.LogInformation("Active location count: {LocationCount}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetDeletedLocationCountAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                locationRepository.GetDeletedCountAsync,
                "Getting deleted location count",
                false
            );

            if (result.IsSuccess)
                logger.LogInformation("Deleted location count: {LocationCount}", result.Value);

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> GetAllDeletedLocationsAsync()
        {
            DatabaseResult<IEnumerable<LocationDto>> result = await GetAllLocationsAsync();

            if (result is { IsSuccess: true, Value: not null })
            {
                IEnumerable<Location> enumerable = result.Value.Select(l => l.ToDomain());
                IEnumerable<Location> deleted = enumerable
                                                .Where(l => l.IsDeleted)
                                                .ToList();

                logger.LogInformation("Successfully retrieved {DeletedLocationCount} deleted locations", deleted.Count());
                IEnumerable<LocationDto> locationDtos = deleted.ToDto();

                return DatabaseResult<IEnumerable<LocationDto>>.Success(locationDtos);
            }

            logger.LogWarning("Failed to retrieve deleted locations: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<LocationDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> GetAllActiveLocationsAsync()
        {
            // Fetches only active locations from database
            DatabaseResult<IEnumerable<Location>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.GetAllAsync(false), // Filter in SQL
                "Retrieving active locations"
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                List<Location> active = result.Value.ToList();
                IEnumerable<LocationDto> locationDtos = active.ToDto();

                logger.LogInformation("Successfully retrieved {ActiveLocationCount} active locations", active.Count);
                locationStore.Initialize(active);

                return DatabaseResult<IEnumerable<LocationDto>>.Success(locationDtos);
            }

            logger.LogWarning("Failed to retrieve active locations: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<LocationDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<LocationDto>>> SearchAsync( string searchTerm, LocationType? type = null )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                logger.LogWarning("Search term is null or empty");
                return DatabaseResult<IEnumerable<LocationDto>>.Success([]);
            }

            DatabaseResult<IEnumerable<Location>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => locationRepository.SearchAsync(searchTerm.Trim(), type, false),
                $"Searching locations with term '{searchTerm}' and type '{type}'."
            );

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Search for '{SearchTerm}' (type: {LocationType}) returned {LocationCount} locations.",
                    searchTerm,
                    type?.ToString() ?? "All",
                    result.Value.Count());
                IEnumerable<LocationDto> locationDtos = result.Value.ToDto();
                return DatabaseResult<IEnumerable<LocationDto>>.Success(locationDtos);
            }

            logger.LogWarning("Failed to search locations with term '{SearchTerm}': {ErrorMessage}", searchTerm, result.ErrorMessage);
            return DatabaseResult<IEnumerable<LocationDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }
    }
}
