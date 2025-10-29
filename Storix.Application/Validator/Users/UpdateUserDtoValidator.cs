using FluentValidation;
using Storix.Application.DTO.Users;

namespace Storix.Application.Validator.Users
{
    public class UpdateUserDtoValidator:UserDtoValidatorBase<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            // UserId validation (specific to UpdateUserDto)
            RuleFor(u => u.UserId)
                .GreaterThan(0)
                .WithMessage("User ID must be a positive integer");

            // Use shared rule configurations
            ConfigureUsernameRules(u => u.Username);
            ConfigureRoleRules(u => u.Role);
            ConfigureFullNameRules(u => u.FullName!);
            ConfigureEmailRules(u => u.Email!);

            // Note: No password rules - password changes use ChangePasswordDto
        }
    }
}
