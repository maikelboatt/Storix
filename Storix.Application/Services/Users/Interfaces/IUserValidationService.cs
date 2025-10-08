using System.Threading.Tasks;
using Storix.Application.Common;

namespace Storix.Application.Services.Users.Interfaces
{
    public interface IUserValidationService
    {
        Task<DatabaseResult<bool>> UserExistAsync( int userId, bool includeDeleted = false );

        Task<DatabaseResult<bool>> UsernameExistsAsync( string username, int? excludedId = null, bool includeDeleted = false );

        Task<DatabaseResult<bool>> EmailExistsAsync( string email, int? excludedId = null, bool includeDeleted = false );

        Task<DatabaseResult> ValidateForDeletion( int userId );

        Task<DatabaseResult> ValidateForHardDeletion( int userId );

        Task<DatabaseResult> ValidateForRestore( int userId );

        Task<DatabaseResult<bool>> IsUserSoftDeleted( int userId );
    }
}
