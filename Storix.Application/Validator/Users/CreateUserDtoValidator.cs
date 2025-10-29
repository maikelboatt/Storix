using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using Storix.Application.DTO.Users;

namespace Storix.Application.Validator.Users
{
    public class CreateUserDtoValidator:UserDtoValidatorBase<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            // Use shared rule configurations
            ConfigureUsernameRules(u => u.Username);
            ConfigureRoleRules(u => u.Role);
            ConfigureFullNameRules(u => u.FullName!);
            ConfigureEmailRules(u => u.Email!);

            // Password rules (specific to CreateUserDto)
            RuleFor(u => u.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long")
                .Must(BeStrongPassword)
                .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character");
        }
    }
}
