using System.Collections.Generic;
using System.Threading.Tasks;
using Storix.Application.Common;
using Storix.Application.DTO.Users;
using Storix.Domain.Models;

namespace Storix.Application.Services.Users
{
    public interface IUserReadService
    {
        Task<DatabaseResult<UserDto?>> GetByIdAsync( int userId );

        Task<DatabaseResult<UserDto?>> GetByUsernameAsync( string username );

        Task<DatabaseResult<UserDto?>> GetByEmailAsync( string email );

        Task<DatabaseResult<IEnumerable<UserDto>>> GetAllUsersAsync();

        /// <summary>
        /// Retrieves all active (non-deleted) users from persistence
        /// and initializes the in-memory store with them.
        /// </summary>
        Task<DatabaseResult<IEnumerable<User>>> GetAllActiveUsersAsync();

        Task<DatabaseResult<IEnumerable<UserDto>>> GetAllDeletedAsync();

        Task<DatabaseResult<IEnumerable<UserDto>>> GetByRoleAsync( string role );

        Task<DatabaseResult<IEnumerable<UserDto>>> GetPagedAsync( int pageNumber, int pageSize );

        Task<DatabaseResult<IEnumerable<UserDto>>> SearchAsync( string searchTerm );

        Task<DatabaseResult<int>> GetTotalCountAsync();

        Task<DatabaseResult<int>> GetActiveCountAsync();

        Task<DatabaseResult<int>> GetDeletedCountAsync();

        Task<DatabaseResult<int>> GetCountByRoleAsync( string role );
    }
}
