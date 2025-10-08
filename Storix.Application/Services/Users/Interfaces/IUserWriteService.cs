using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Users;

namespace Storix.Application.Services.Users
{
    public interface IUserWriteService
    {
        Task<DatabaseResult<UserDto>> CreateUserAsync( CreateUserDto createUserDto );

        Task<DatabaseResult<UserDto>> UpdateUserAsync( UpdateUserDto updateUserDto );

        Task<DatabaseResult> ChangePasswordAsync( ChangePasswordDto changePasswordDto );

        Task<DatabaseResult> SoftDeleteUserAsync( int userId );

        Task<DatabaseResult> RestoreUserAsync( int userId );

        Task<DatabaseResult> HardDeleteUserAsync( int userId );
    }
}
