using System.Threading.Tasks;
using Storix.Application.Common;

namespace Storix.Application.Services.Locations.Interfaces
{
    public interface ILocationValidationService
    {
        Task<DatabaseResult<bool>> LocationExistsAsync( int locationId, bool includeDeleted = false );

        Task<DatabaseResult<bool>> LocationNameExistsAsync( string name, int? excludeLocationId = null, bool includeDeleted = false );

        Task<DatabaseResult> ValidateForDeletion( int locationId );

        Task<DatabaseResult> ValidateForHardDeletion( int locationId );

        Task<DatabaseResult> ValidateForRestore( int locationId );

        Task<DatabaseResult<bool>> IsLocationSoftDeleted( int locationId );
    }
}
