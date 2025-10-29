using System;

namespace Storix.Application.DTO.Users
{
    public class UpdateUserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
    }
}
