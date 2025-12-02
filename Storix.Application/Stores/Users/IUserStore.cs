using System.Collections.Generic;
using Storix.Application.DTO.Users;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Users
{
    /// <summary>
    ///     In-memory cache interface for active (non-deleted) users.
    ///     All methods work only with active users.
    /// </summary>
    public interface IUserStore
    {
        // Lifecycle
        void Initialize( IEnumerable<User> users );

        void Clear();

        // Write operations
        UserDto? Create( int userId, CreateUserDto createDto );

        UserDto? Update( UpdateUserDto updateDto );

        bool Delete( int userId ); // Removes from cache

        // Read operations (single)
        UserDto? GetById( int userId );

        string? GetUsername( int userId );

        UserDto? GetByUsername( string username );

        UserDto? GetByEmail( string email );

        // Read operations (collection)
        List<UserDto> GetAll();

        List<UserDto> GetByRole( string role );

        List<UserDto> Search( string searchTerm );

        List<UserDto> GetActiveUsers();

        IEnumerable<User> GetAllUsers();

        // Validation
        bool Exists( int userId );

        bool UsernameExists( string username, int? excludeUserId = null );

        bool EmailExists( string email, int? excludeUserId = null );

        // Counts
        int GetCount();

        int GetCountByRole( string role );

        int GetActiveCount();
    }
}
