using Storix.Domain.Enums;

namespace Storix.Application.DTO.Location
{
    public class CreateLocationDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public LocationType Type { get; set; }
        public string? Address { get; set; }
    }
}
