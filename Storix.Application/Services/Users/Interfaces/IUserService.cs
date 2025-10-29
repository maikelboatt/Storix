using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Users;
using Storix.Domain.Models;

namespace Storix.Application.Services.Users
{
    public interface IUserService
    {
        Task<DatabaseResult<UserDto?>> GetByIdAsync( int userId );

        Task<DatabaseResult<UserDto?>> GetByUsernameAsync( string username );

        Task<DatabaseResult<UserDto?>> GetByEmailAsync( string email, bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<UserDto>>> GetAllAsync();

        Task<DatabaseResult<IEnumerable<User>>> GetAllActiveAsync();

        Task<DatabaseResult<IEnumerable<UserDto>>> GetAllDeletedAsync();

        Task<DatabaseResult<IEnumerable<UserDto>>> GetByRoleAsync( string role );

        Task<DatabaseResult<IEnumerable<UserDto>>> GetPagedAsync(
            int pageNumber,
            int pageSize
        );

        Task<DatabaseResult<IEnumerable<UserDto>>> SearchAsync( string searchTerm );

        Task<DatabaseResult<int>> GetTotalCountAsync();

        Task<DatabaseResult<int>> GetActiveCountAsync();

        Task<DatabaseResult<int>> GetDeletedCountAsync();

        Task<DatabaseResult<int>> GetCountByRoleAsync( string role );

        Task<DatabaseResult<UserDto>> CreateUserAsync( CreateUserDto createUserDto );

        Task<DatabaseResult<UserDto>> UpdateUserAsync( UpdateUserDto updateUserDto );

        Task<DatabaseResult> ChangePasswordAsync( ChangePasswordDto changePasswordDto );

        Task<DatabaseResult> SoftDeleteUserAsync( int userId );

        Task<DatabaseResult> RestoreUserAsync( int userId );

        Task<DatabaseResult> HardDeleteUserAsync( int userId );

        Task<DatabaseResult<bool>> UserExistsAsync( int userId, bool includeDeleted = false );

        Task<DatabaseResult<bool>> UsernameExistsAsync(
            string username,
            int? excludeUserId = null,
            bool includeDeleted = false
        );

        Task<DatabaseResult<bool>> EmailExistsAsync(
            string email,
            int? excludeUserId = null,
            bool includeDeleted = false
        );

        Task<DatabaseResult> ValidateForDeletion( int userId );

        Task<DatabaseResult> ValidateForHardDeletion( int userId );

        Task<DatabaseResult> ValidateForRestore( int userId );

        Task<DatabaseResult<bool>> IsUserSoftDeleted( int userId );

        UserDto? GetByIdFromCache( int userId );

        UserDto? GetByUsernameFromCache( string username );

        UserDto? GetByEmailFromCache( string email );

        List<UserDto> GetAllFromCache();

        List<UserDto> GetByRoleFromCache( string role );

        List<UserDto> SearchInCache( string searchTerm );

        bool ExistsInCache( int userId );

        bool UsernameExistsInCache( string username, int? excludeUserId = null );

        bool EmailExistsInCache( string email, int? excludeUserId = null );

        int GetCountFromCache();

        int GetCountByRoleFromCache( string role );

        void RefreshStoreCache();

        /// <summary>
        ///     Soft deletes multiple users in bulk.
        ///     Each user is validated and deleted individually.
        /// </summary>
        Task<DatabaseResult<IEnumerable<UserDto>>> BulkSoftDeleteAsync( IEnumerable<int> userIds );

        /// <summary>
        ///     Restores multiple soft-deleted users in bulk.
        ///     Each user is validated and restored individually.
        /// </summary>
        Task<DatabaseResult<IEnumerable<UserDto>>> BulkRestoreAsync( IEnumerable<int> userIds );
    }
}
