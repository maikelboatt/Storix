using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using Storix.Application.DTO.Locations;

namespace Storix.Application.Validator.Locations
{
    public class UpdateLocationDtoValidator:AbstractValidator<UpdateLocationDto>
    {
        public UpdateLocationDtoValidator()
        {
            RuleFor(l => l.LocationId)
                .GreaterThan(0)
                .WithMessage("Location ID must be greater than zero");

            RuleFor(l => l.Name)
                .NotEmpty()
                .WithMessage("Location name is required")
                .Length(2, 100)
                .WithMessage("Location name must be between 2 and 100 characters")
                .Must(BeValidLocationName)
                .WithMessage("Location name contains invalid characters")
                .Must(NotBeReservedName)
                .WithMessage("This location name is reserved and cannot be used");

            RuleFor(l => l.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters")
                .Must(NotContainHtml)
                .WithMessage("Description cannot contain HTML tags")
                .When(l => !string.IsNullOrEmpty(l.Description));

            RuleFor(l => l.Type)
                .IsInEnum()
                .WithMessage("Please select a valid location type");

            RuleFor(l => l.Address)
                .MaximumLength(300)
                .WithMessage("Address cannot exceed 300 characters")
                .Must(BeValidAddress)
                .WithMessage("Address contains invalid characters")
                .When(l => !string.IsNullOrEmpty(l.Address));
        }

        #region Custom validation methods

        /// <summary>
        ///     Must match valid location name pattern (letters, numbers, spaces, and basic punctuation).
        /// </summary>
        /// <param name="name">Name to be verified.</param>
        /// <returns></returns>
        private static bool BeValidLocationName( string name ) => !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[a-zA-Z0-9\s\-&'().,#]+$");

        /// <summary>
        ///     Must not be a reserved name
        /// </summary>
        /// <param name="name">Name to be verified</param>
        /// <returns></returns>
        private static bool NotBeReservedName( string name )
        {
            if (string.IsNullOrEmpty(name)) return true;

            string[] reservedNames = new[]
            {
                "default",
                "admin",
                "api",
                "system",
                "root",
                "all",
                "none",
                "null",
                "undefined",
                "temp",
                "temporary"
            };

            return !reservedNames.Contains(name.ToLowerInvariant());
        }

        /// <summary>
        ///     Validates address format (allows letters, numbers, common punctuation, and address characters)
        /// </summary>
        /// <param name="address">Address to be verified</param>
        /// <returns></returns>
        private static bool BeValidAddress( string address )
        {
            if (string.IsNullOrEmpty(address)) return true;

            // Allow letters, numbers, spaces, and common address characters
            return Regex.IsMatch(address, @"^[a-zA-Z0-9\s\-.,#/()'\n\r]+$");
        }

        /// <summary>
        ///     Checks if description contains HTML tags
        /// </summary>
        /// <param name="description">Description to be verified</param>
        /// <returns></returns>
        private static bool NotContainHtml( string description )
        {
            if (string.IsNullOrEmpty(description)) return true;

            // Simple HTML tag detection
            return !Regex.IsMatch(description, @"<[^>]+>");
        }

        #endregion
    }
}
