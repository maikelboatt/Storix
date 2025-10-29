using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;

namespace Storix.Application.Validator.Users
{
    /// <summary>
    ///     Base validator containing shared validation logic for user DTOs.
    /// </summary>
    public abstract class UserDtoValidatorBase<T>:AbstractValidator<T>
    {
        #region Shared Custom Validation Methods

        protected static bool BeValidUsername( string username ) => !string.IsNullOrEmpty(username) &&
                                                                    // Username: letters, numbers, underscores, hyphens
                                                                    // Must start with a letter or number
                                                                    Regex.IsMatch(username, @"^[a-zA-Z0-9][a-zA-Z0-9_-]*$");

        protected static bool NotBeReservedUsername( string username )
        {
            if (string.IsNullOrEmpty(username)) return true;

            string[] reservedUsernames = new[]
            {

                "admin",
                "administrator",
                "root",
                "system",
                "sysadmin",
                "superuser",
                "guest",
                "user",
                "default",
                "test",
                "demo",
                "api",
                "null",
                "undefined",
                "everyone",
                "all",
                "public",
                "private",
                "support",
                "help",
                "info",
                "webmaster",
                "postmaster",
                "hostmaster",
                "abuse",
                "noreply",
                "no-reply"
            };

            return !reservedUsernames.Contains(username.ToLowerInvariant());
        }

        protected static bool BeStrongPassword( string password )
        {
            if (string.IsNullOrEmpty(password)) return false;

            // At least one uppercase, one lowercase, one digit, one special character
            bool hasUppercase = Regex.IsMatch(password, @"[A-Z]");
            bool hasLowercase = Regex.IsMatch(password, @"[a-z]");
            bool hasDigit = Regex.IsMatch(password, @"\d");
            bool hasSpecialChar = Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]");

            return hasUppercase && hasLowercase && hasDigit && hasSpecialChar;
        }

        protected static bool BeValidRole( string role )
        {
            if (string.IsNullOrEmpty(role)) return false;

            string[] validRoles = new[]
            {
                "Admin",
                "Manager",
                "Staff",
                "User"
            };
            return validRoles.Contains(role, System.StringComparer.OrdinalIgnoreCase);
        }

        protected static bool BeValidFullName( string fullName ) => string.IsNullOrEmpty(fullName) ||
                                                                    // Optional field
                                                                    // Letters, spaces, hyphens, apostrophes, periods (for initials)
                                                                    Regex.IsMatch(fullName, @"^[a-zA-Z\s\-'.]+$");

        protected static bool NotBeDisposableEmail( string email )
        {
            if (string.IsNullOrEmpty(email)) return true;

            // Common disposable email domains
            string[] disposableDomains = new[]
            {
                "tempmail.com",
                "throwaway.email",
                "guerrillamail.com",
                "mailinator.com",
                "10minutemail.com",
                "maildrop.cc",
                "trashmail.com",
                "yopmail.com",
                "temp-mail.org",
                "fakeinbox.com",
                "dispostable.com",
                "getnada.com"
            };

            string? domain = email
                             .Split('@')
                             .LastOrDefault()
                             ?.ToLowerInvariant();
            return !disposableDomains.Contains(domain);
        }

        #endregion

        #region Shared Rule Definitions

        protected void ConfigureUsernameRules( System.Linq.Expressions.Expression<System.Func<T, string>> usernameSelector )
        {
            RuleFor(usernameSelector)
                .NotEmpty()
                .WithMessage("Username is required")
                .Length(3, 50)
                .WithMessage("Username must be between 3 and 50 characters")
                .Must(BeValidUsername)
                .WithMessage("Username can only contain letters, numbers, underscores, and hyphens")
                .Must(NotBeReservedUsername)
                .WithMessage("This username is reserved and cannot be used");
        }

        protected void ConfigureRoleRules( System.Linq.Expressions.Expression<System.Func<T, string>> roleSelector )
        {
            RuleFor(roleSelector)
                .NotEmpty()
                .WithMessage("Role is required")
                .Must(BeValidRole)
                .WithMessage("Role must be one of: Admin, Manager, Staff, or User");
        }

        protected void ConfigureFullNameRules( System.Linq.Expressions.Expression<System.Func<T, string>> fullNameSelector )
        {
            RuleFor(fullNameSelector)
                .MaximumLength(100)
                .WithMessage("Full name cannot exceed 100 characters")
                .Must(BeValidFullName)
                .WithMessage("Full name contains invalid characters")
                .When(dto => !string.IsNullOrEmpty(
                          fullNameSelector
                              .Compile()(dto)));
        }

        protected void ConfigureEmailRules( System.Linq.Expressions.Expression<System.Func<T, string>> emailSelector )
        {
            RuleFor(emailSelector)
                .EmailAddress()
                .WithMessage("Invalid email address format")
                .MaximumLength(255)
                .WithMessage("Email cannot exceed 255 characters")
                .Must(NotBeDisposableEmail)
                .WithMessage("Disposable email addresses are not allowed")
                .When(dto => !string.IsNullOrEmpty(
                          emailSelector
                              .Compile()(dto)));
        }

        #endregion
    }
}
