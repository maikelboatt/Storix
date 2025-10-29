using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Users;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Users
{
    /// <summary>
    ///     In-memory cache for active (non-deleted) users.
    ///     Provides fast lookup for frequently accessed user data.
    /// </summary>
    public class UserStore:IUserStore
    {
        private readonly Dictionary<int, User> _users;
        private readonly Dictionary<string, int> _usernameIndex; // Fast username lookup

        public UserStore( List<User>? initialUsers = null )
        {
            _users = new Dictionary<int, User>();
            _usernameIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (initialUsers == null) return;

            // Only cache active users
            foreach (User user in initialUsers.Where(u => !u.IsDeleted))
            {
                _users[user.UserId] = user;
                _usernameIndex[user.Username] = user.UserId;
            }
        }

        public void Initialize( IEnumerable<User> users )
        {
            _users.Clear();
            _usernameIndex.Clear();

            // Only cache active users
            foreach (User user in users.Where(u => !u.IsDeleted))
            {
                _users[user.UserId] = user;
                _usernameIndex[user.Username] = user.UserId;
            }
        }

        public void Clear()
        {
            _users.Clear();
            _usernameIndex.Clear();
        }

        public UserDto? Create( int userId, CreateUserDto createDto )
        {
            if (string.IsNullOrWhiteSpace(createDto.Username))
            {
                return null;
            }

            // Check if username already exists
            if (UsernameExists(createDto.Username))
            {
                return null;
            }

            // Check if email already exists (if provided)
            if (!string.IsNullOrWhiteSpace(createDto.Email) && EmailExists(createDto.Email))
            {
                return null;
            }

            User user = new(
                userId,
                createDto.Username.Trim(),
                createDto.Password, // Should be hashed before calling this
                createDto.Role,
                createDto.FullName?.Trim(),
                createDto.Email?.Trim(),
                createDto.IsActive,
                false,
                null
            );

            _users[userId] = user;
            _usernameIndex[user.Username] = userId;
            return user.ToDto();
        }

        public UserDto? Update( UpdateUserDto updateDto )
        {
            // Only update active users
            if (!_users.TryGetValue(updateDto.UserId, out User? existingUser))
            {
                return null; // User not found in active cache
            }

            if (string.IsNullOrWhiteSpace(updateDto.Username))
            {
                return null;
            }

            // Check username availability (excluding current user)
            if (UsernameExists(updateDto.Username, updateDto.UserId))
            {
                return null;
            }

            // Check email availability (excluding current user)
            if (!string.IsNullOrWhiteSpace(updateDto.Email) && EmailExists(updateDto.Email, updateDto.UserId))
            {
                return null;
            }

            // Remove old username from index if it changed
            if (!existingUser.Username.Equals(updateDto.Username, StringComparison.OrdinalIgnoreCase))
            {
                _usernameIndex.Remove(existingUser.Username);
            }

            User updatedUser = existingUser with
            {
                Username = updateDto.Username.Trim(),
                Role = updateDto.Role,
                FullName = updateDto.FullName?.Trim(),
                Email = updateDto.Email?.Trim(),
                IsActive = updateDto.IsActive
                // Password, IsDeleted, DeletedAt remain unchanged
            };

            _users[updateDto.UserId] = updatedUser;
            _usernameIndex[updatedUser.Username] = updatedUser.UserId;
            return updatedUser.ToDto();
        }

        public bool Delete( int userId )
        {
            // Remove from active cache
            if (!_users.Remove(userId, out User? user)) return false;

            _usernameIndex.Remove(user.Username);
            return true;
        }

        public UserDto? GetById( int userId ) =>
            // Only searches active users
            _users.TryGetValue(userId, out User? user)
                ? user.ToDto()
                : null;

        public UserDto? GetByUsername( string username )
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            // Fast username lookup using index
            if (_usernameIndex.TryGetValue(username, out int userId))
            {
                return _users.TryGetValue(userId, out User? user)
                    ? user.ToDto()
                    : null;
            }

            return null;
        }

        public UserDto? GetByEmail( string email )
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            User? user = _users.Values
                               .FirstOrDefault(u => !string.IsNullOrEmpty(u.Email) &&
                                                    u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            return user?.ToDto();
        }

        public List<UserDto> GetAll()
        {
            return _users
                   .Values
                   .OrderBy(u => u.Username)
                   .Select(u => u.ToDto())
                   .ToList();
        }

        public List<UserDto> GetByRole( string role )
        {
            return _users
                   .Values
                   .Where(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase))
                   .OrderBy(u => u.Username)
                   .Select(u => u.ToDto())
                   .ToList();
        }

        public List<UserDto> Search( string searchTerm )
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return GetAll();
            }

            string searchLower = searchTerm.ToLowerInvariant();

            return _users
                   .Values
                   .Where(u =>
                              u
                                  .Username.ToLowerInvariant()
                                  .Contains(searchLower) ||
                              u.FullName != null && u
                                                    .FullName.ToLowerInvariant()
                                                    .Contains(searchLower) ||
                              u.Email != null && u
                                                 .Email.ToLowerInvariant()
                                                 .Contains(searchLower))
                   .OrderBy(u => u.Username)
                   .Select(u => u.ToDto())
                   .ToList();
        }

        public bool Exists( int userId ) =>
            // Only checks active users
            _users.ContainsKey(userId);

        public bool UsernameExists( string username, int? excludeUserId = null )
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            if (_usernameIndex.TryGetValue(username, out int userId))
            {
                return excludeUserId == null || userId != excludeUserId.Value;
            }

            return false;
        }

        public bool EmailExists( string email, int? excludeUserId = null )
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            return _users.Values.Any(u =>
                                         !string.IsNullOrEmpty(u.Email) &&
                                         u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                                         (excludeUserId == null || u.UserId != excludeUserId.Value));
        }

        public int GetCount() => _users.Count;

        public int GetCountByRole( string role )
        {
            return _users.Values.Count(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
        }

        public int GetActiveCount() => _users.Count;

        public List<UserDto> GetActiveUsers() => GetAll();

        public IEnumerable<User> GetAllUsers()
        {
            return _users.Values.OrderBy(u => u.Username);
        }
    }
}
