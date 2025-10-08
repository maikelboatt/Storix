using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Users;

namespace Storix.Application.Services.Users.Interfaces
{
    public interface IUserReadService
    {
        Task<DatabaseResult<UserDto?>> GetByIdAsync( int userId, bool includeDeleted = false );

        Task<DatabaseResult<UserDto?>> GetByUsernameAsync( string username, bool includeDeleted = false );

        Task<DatabaseResult<UserDto?>> GetByEmailAsync( string email, bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<UserDto>>> GetAllAsync( bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<UserDto>>> GetAllActiveAsync();

        Task<DatabaseResult<IEnumerable<UserDto>>> GetAllDeletedAsync();

        Task<DatabaseResult<IEnumerable<UserDto>>> GetByRoleAsync( string role, bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<UserDto>>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            bool includeDeleted = false );

        Task<DatabaseResult<IEnumerable<UserDto>>> SearchAsync( string searchTerm, bool includeDeleted = false );

        Task<DatabaseResult<int>> GetTotalCountAsync( bool includeDeleted = false );

        Task<DatabaseResult<int>> GetActiveCountAsync();

        Task<DatabaseResult<int>> GetDeletedCountAsync();

        Task<DatabaseResult<int>> GetCountByRoleAsync( string role, bool includeDeleted = false );
    }
}
