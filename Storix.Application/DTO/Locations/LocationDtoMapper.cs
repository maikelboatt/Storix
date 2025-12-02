using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Domain.Models;

namespace Storix.Application.DTO.Locations
{
    public static class LocationDtoMapper
    {
        public static LocationDto ToDto( this Location location ) => new()
        {
            LocationId = location.LocationId,
            Name = location.Name,
            Description = location.Description,
            Type = location.Type,
            Address = location.Address
        };

        public static CreateLocationDto ToCreateDto( this LocationDto dto ) => new()
        {
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            Address = dto.Address
        };

        public static UpdateLocationDto ToUpdateDto( this LocationDto dto ) => new()
        {
            LocationId = dto.LocationId,
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            Address = dto.Address
        };

        public static Location ToDomain( this LocationDto dto ) => new(
            dto.LocationId,
            dto.Name,
            dto.Description,
            dto.Type,
            dto.Address,
            false,
            null);

        public static Location ToDomain( this CreateLocationDto dto ) => new(
            0,
            dto.Name,
            dto.Description,
            dto.Type,
            dto.Address,
            false,
            null);

        public static Location ToDomain( this UpdateLocationDto dto ) => new(
            dto.LocationId,
            dto.Name,
            dto.Description,
            dto.Type,
            dto.Address,
            false,
            null);

        public static Location ToDomain( this UpdateLocationDto dto, bool isDeleted, DateTime? deletedAt ) => new(
            dto.LocationId,
            dto.Name,
            dto.Description,
            dto.Type,
            dto.Address,
            isDeleted,
            deletedAt);

        public static IEnumerable<LocationDto> ToDto( this IEnumerable<Location> locations ) => locations.Select(ToDto);
    }
}
