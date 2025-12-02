using Storix.Domain.Enums;

namespace Storix.Application.DTO.Locations
{
    public class LocationDto
    {
        public int LocationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public LocationType Type { get; set; }
        public string? Address { get; set; }

        public override string ToString() => Name;
    }
}
