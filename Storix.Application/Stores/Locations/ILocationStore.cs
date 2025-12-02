using System;
using System.Collections.Generic;
using Storix.Application.DTO.Locations;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Locations
{
    public interface ILocationStore
    {
        void Initialize( IEnumerable<Location> locations );

        void Clear();

        event Action<Location>? LocationAdded;
        event Action<Location>? LocationUpdated;
        event Action<int>? LocationDeleted;

        LocationDto? Create( int locationId, CreateLocationDto createDto );

        LocationDto? Update( UpdateLocationDto updateDto );

        bool Delete( int locationId );

        LocationDto? GetById( int locationId );

        string? GetLocationName( int locationId );

        LocationDto? GetByName( string name );

        List<LocationDto> GetByType( LocationType type );

        List<LocationDto> GetAll();

        List<LocationDto> Search( string searchTerm, LocationType? type = null );

        bool Exists( int locationId );

        bool NameExists( string name, int? excludeLocationId = null );

        int GetCount();

        int GetActiveCount();

        int GetCountByType( LocationType type );

        List<LocationDto> GetActiveLocations();

        IEnumerable<Location> GetAllLocations();

        Dictionary<LocationType, int> GetLocationCountsByType();
    }
}
