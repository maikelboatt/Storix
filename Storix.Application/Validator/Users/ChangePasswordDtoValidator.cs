using FluentValidation;
using Storix.Application.DTO.Users;

namespace Storix.Application.Validator.Users
{
    public class ChangePasswordDtoValidator:UserDtoValidatorBase<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(c => c.UserId)
                .GreaterThan(0)
                .WithMessage("User ID must be a positive integer");

            RuleFor(c => c.CurrentPassword)
                .NotEmpty()
                .WithMessage("Current password is required");

            RuleFor(c => c.NewPassword)
                .NotEmpty()
                .WithMessage("New password is required")
                .MinimumLength(8)
                .WithMessage("New password must be at least 8 characters long")
                .Must(BeStrongPassword)
                .WithMessage("New password must contain at least one uppercase letter, one lowercase letter, one number, and one special character");

            RuleFor(c => c.NewPassword)
                .NotEqual(c => c.CurrentPassword)
                .WithMessage("New password must be different from current password")
                .When(c => !string.IsNullOrEmpty(c.CurrentPassword) && !string.IsNullOrEmpty(c.NewPassword));
        }
    }
}
