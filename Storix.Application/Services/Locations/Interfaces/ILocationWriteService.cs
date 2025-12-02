using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Locations;

namespace Storix.Application.Services.Locations.Interfaces
{
    public interface ILocationWriteService
    {
        Task<DatabaseResult<LocationDto>> CreateLocationAsync( CreateLocationDto createLocationDto );

        Task<DatabaseResult<LocationDto>> UpdateLocationAsync( UpdateLocationDto updateLocationDto );

        Task<DatabaseResult> SoftDeleteLocationAsync( int locationId );

        Task<DatabaseResult> RestoreLocationAsync( int locationId );

        Task<DatabaseResult> HardDeleteLocationAsync( int locationId );
    }
}
