using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Domain.Models;

namespace Storix.Application.DTO.Users
{
    public static class UserDtoMapper
    {
        // Domain to DTO - excludes password and soft delete properties
        public static UserDto ToDto( this User user ) => new()
        {
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role,
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive
        };

        // DTO to Domain - sets soft delete properties to defaults, preserves password
        public static User ToDomain( this UserDto dto, string password ) => new(
            dto.UserId,
            dto.Username,
            password, // Password must be provided separately
            dto.Role,
            dto.FullName,
            dto.Email,
            dto.IsActive,
            false,
            null
        );

        // UserDto to CreateUserDto
        public static CreateUserDto ToCreateDto( this UserDto dto, string password ) => new()
        {
            Username = dto.Username,
            Password = password,
            Role = dto.Role,
            FullName = dto.FullName,
            Email = dto.Email,
            IsActive = dto.IsActive
        };

        // UserDto to UpdateUserDto
        public static UpdateUserDto ToUpdateDto( this UserDto dto ) => new()
        {
            UserId = dto.UserId,
            Username = dto.Username,
            Role = dto.Role,
            FullName = dto.FullName,
            Email = dto.Email,
            IsActive = dto.IsActive
        };

        // User to CreateUserDto
        public static CreateUserDto ToCreateDto( this User user ) => new()
        {
            Username = user.Username,
            Password = user.Password, // Note: This should ideally never be called with unhashed passwords
            Role = user.Role,
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive
        };

        // CreateUserDto to Domain - always creates non-deleted users
        public static User ToDomain( this CreateUserDto dto, int userId = 0 ) => new(
            userId,
            dto.Username,
            dto.Password, // Password should be hashed before calling this
            dto.Role,
            dto.FullName,
            dto.Email,
            dto.IsActive,
            false,
            null
        );

        // UpdateUserDto to Domain - preserves existing soft delete state and password
        public static User ToDomain(
            this UpdateUserDto dto,
            string existingPassword,
            bool isDeleted = false,
            DateTime? deletedAt = null ) => new(
            dto.UserId,
            dto.Username,
            existingPassword, // Preserve existing password
            dto.Role,
            dto.FullName,
            dto.Email,
            dto.IsActive,
            isDeleted,
            deletedAt
        );

        // ChangePasswordDto to update existing User
        public static User WithNewPassword( this User user, string newHashedPassword ) => user with
        {
            Password = newHashedPassword
        };

        // Collection mapping
        public static IEnumerable<UserDto> ToDto( this IEnumerable<User> users ) => users.Select(u => u.ToDto());
    }
}
