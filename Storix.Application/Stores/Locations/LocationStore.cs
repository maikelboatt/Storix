using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Locations;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Locations
{
    /// <summary>
    ///     In-memory cache for active (non-deleted) locations.
    ///     Provides fast lookup for frequently accessed location data.
    /// </summary>
    public class LocationStore:ILocationStore
    {
        private readonly Dictionary<int, Location> _locations;
        private readonly Dictionary<string, int> _nameIndex;                // Fast name lookup
        private readonly Dictionary<LocationType, HashSet<int>> _typeIndex; // Fast type filtering

        public LocationStore( List<Location>? initialLocations = null )
        {
            _locations = new Dictionary<int, Location>();
            _nameIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _typeIndex = new Dictionary<LocationType, HashSet<int>>();

            // Initialize type index with all enum values
            foreach (LocationType type in Enum.GetValues<LocationType>())
            {
                _typeIndex[type] = [];
            }

            if (initialLocations == null) return;

            // Only cache active locations
            foreach (Location location in initialLocations.Where(l => !l.IsDeleted))
            {
                _locations[location.LocationId] = location;

                if (!string.IsNullOrWhiteSpace(location.Name))
                    _nameIndex[location.Name] = location.LocationId;

                _typeIndex[location.Type]
                    .Add(location.LocationId);
            }
        }

        public void Initialize( IEnumerable<Location> locations )
        {
            _locations.Clear();
            _nameIndex.Clear();

            // Clear type index - BETTER: Just recreate all HashSets
            foreach (LocationType type in Enum.GetValues<LocationType>())
            {
                _typeIndex[type] = new HashSet<int>();
            }

            // Only cache active locations
            foreach (Location location in locations.Where(l => !l.IsDeleted))
            {
                _locations[location.LocationId] = location;

                if (!string.IsNullOrWhiteSpace(location.Name))
                    _nameIndex[location.Name] = location.LocationId;

                _typeIndex[location.Type]
                    .Add(location.LocationId);
            }
        }

        public void Clear()
        {
            _locations.Clear();
            _nameIndex.Clear();

            foreach (LocationType type in Enum.GetValues(typeof(LocationType)))
            {
                _typeIndex[type]
                    .Clear();
            }
        }

        public event Action<Location>? LocationAdded;
        public event Action<Location>? LocationUpdated;
        public event Action<int>? LocationDeleted;

        public LocationDto? Create( int locationId, CreateLocationDto createDto )
        {
            if (string.IsNullOrWhiteSpace(createDto.Name))
            {
                return null;
            }

            // Check if name already exists
            if (NameExists(createDto.Name))
            {
                return null;
            }

            Location location = new(
                locationId,
                createDto.Name.Trim(),
                createDto.Description?.Trim(),
                createDto.Type,
                createDto.Address?.Trim(),
                false,
                null
            );

            _locations[locationId] = location;

            if (!string.IsNullOrWhiteSpace(location.Name))
                _nameIndex[location.Name] = locationId;

            _typeIndex[location.Type]
                .Add(locationId);

            LocationAdded?.Invoke(location);
            return location.ToDto();
        }

        public LocationDto? Update( UpdateLocationDto updateDto )
        {
            // Only update active locations
            if (!_locations.TryGetValue(updateDto.LocationId, out Location? existingLocation))
            {
                return null; // Location not found in active cache
            }

            if (string.IsNullOrWhiteSpace(updateDto.Name))
            {
                return null;
            }

            // Check name availability (excluding current location)
            if (NameExists(updateDto.Name, updateDto.LocationId))
            {
                return null;
            }

            // Remove old name from index if it changed
            if (!string.IsNullOrWhiteSpace(existingLocation.Name) &&
                !string.Equals(existingLocation.Name, updateDto.Name, StringComparison.OrdinalIgnoreCase))
            {
                _nameIndex.Remove(existingLocation.Name);
            }

            // Remove from old type index if type changed
            if (existingLocation.Type != updateDto.Type)
            {
                _typeIndex[existingLocation.Type]
                    .Remove(updateDto.LocationId);
            }

            Location updatedLocation = existingLocation with
            {
                Name = updateDto.Name.Trim(),
                Description = updateDto.Description?.Trim(),
                Type = updateDto.Type,
                Address = updateDto.Address?.Trim()
                // IsDeleted, DeletedAt remain unchanged
            };

            _locations[updateDto.LocationId] = updatedLocation;

            if (!string.IsNullOrWhiteSpace(updatedLocation.Name))
                _nameIndex[updatedLocation.Name] = updatedLocation.LocationId;

            _typeIndex[updatedLocation.Type]
                .Add(updatedLocation.LocationId);

            LocationUpdated?.Invoke(updatedLocation);
            return updatedLocation.ToDto();
        }

        public bool Delete( int locationId )
        {
            // Remove from active cache
            if (!_locations.Remove(locationId, out Location? location)) return false;

            if (!string.IsNullOrWhiteSpace(location.Name))
                _nameIndex.Remove(location.Name);

            _typeIndex[location.Type]
                .Remove(locationId);

            LocationDeleted?.Invoke(locationId);
            return true;
        }

        public LocationDto? GetById( int locationId ) =>
            // Only searches active locations
            _locations.TryGetValue(locationId, out Location? location)
                ? location.ToDto()
                : null;

        public string? GetLocationName( int locationId ) => GetById(locationId)
            ?.Name;

        public LocationDto? GetByName( string name )
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            // Fast name lookup using index
            if (_nameIndex.TryGetValue(name, out int locationId))
            {
                return _locations.TryGetValue(locationId, out Location? location)
                    ? location.ToDto()
                    : null;
            }

            return null;
        }

        public List<LocationDto> GetByType( LocationType type )
        {
            if (!_typeIndex.TryGetValue(type, out HashSet<int>? locationIds))
            {
                return new List<LocationDto>();
            }

            return locationIds
                   .Select(id => _locations.TryGetValue(id, out Location? location)
                               ? location
                               : null)
                   .Where(location => location != null)
                   .Select(location => location!.ToDto())
                   .OrderBy(dto => dto.Name)
                   .ToList();
        }

        public List<LocationDto> GetAll()
        {
            return _locations
                   .Values
                   .OrderBy(l => l.Name)
                   .Select(l => l.ToDto())
                   .ToList();
        }

        public List<LocationDto> Search( string searchTerm, LocationType? type = null )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return type.HasValue
                    ? GetByType(type.Value)
                    : GetAll();
            }

            string searchLower = searchTerm.ToLowerInvariant();

            IEnumerable<Location> query = _locations.Values;

            // Filter by type if specified
            if (type.HasValue)
            {
                query = query.Where(l => l.Type == type.Value);
            }

            return query
                   .Where(l =>
                              l
                                  .Name.ToLowerInvariant()
                                  .Contains(searchLower) ||
                              l.Description != null && l
                                                       .Description.ToLowerInvariant()
                                                       .Contains(searchLower) ||
                              l.Address != null && l
                                                   .Address.ToLowerInvariant()
                                                   .Contains(searchLower))
                   .OrderBy(l => l.Name)
                   .Select(l => l.ToDto())
                   .ToList();
        }

        public bool Exists( int locationId ) =>
            // Only checks active locations
            _locations.ContainsKey(locationId);

        public bool NameExists( string name, int? excludeLocationId = null )
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (_nameIndex.TryGetValue(name, out int locationId))
            {
                return excludeLocationId == null || locationId != excludeLocationId.Value;
            }

            return false;
        }

        public int GetCount() => _locations.Count;

        public int GetActiveCount() => _locations.Count;

        public int GetCountByType( LocationType type ) => _typeIndex.TryGetValue(type, out HashSet<int>? locationIds)
            ? locationIds.Count
            : 0;

        public List<LocationDto> GetActiveLocations() => GetAll();

        public IEnumerable<Location> GetAllLocations()
        {
            return _locations.Values.OrderBy(l => l.Name);
        }

        public Dictionary<LocationType, int> GetLocationCountsByType()
        {
            return _typeIndex.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count
            );
        }
    }
}
