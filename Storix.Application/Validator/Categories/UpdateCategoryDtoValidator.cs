using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using Storix.Application.DTO.Categories;

namespace Storix.Application.Validator.Categories
{
    public class UpdateCategoryDtoValidator:AbstractValidator<UpdateCategoryDto>
    {
        public UpdateCategoryDtoValidator()
        {
            RuleFor(c => c.CategoryId)
                .GreaterThan(0).WithMessage("Category ID must be greater than zero");

            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Category name is required")
                .Length(2, 100).WithMessage("Category must be between 2 and 100 characters")
                .Must(BeValidCategoryName).WithMessage("Category name contains invalid characters")
                .Must(NotBeReservedName).WithMessage("This category name is reserved and cannot be used");

            RuleFor(c => c.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
                .Must(NotContainHtml).WithMessage("Description cannot contain HTML tags")
                .When(c => !string.IsNullOrEmpty(c.Description));

            RuleFor(c => c.ParentCategoryId)
                .GreaterThan(0).WithMessage("Please select a valid parent category")
                .When(c => c.ParentCategoryId.HasValue);
        }

        #region Custom validation methods

        /// <summary>
        ///     Must match valid category name pattern (letters, numbers, spaces, and basic punctuation).
        /// </summary>
        /// <param name="name" > Name to be verified.</param>
        /// <returns></returns>
        private static bool BeValidCategoryName( string name ) => !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[a-zA-Z0-9\s\-&'().,!]+$");

        /// <summary>
        ///     Must not be a reserved name
        /// </summary>
        /// <param name="name" >Name to be verified</param>
        /// <returns></returns>
        private static bool NotBeReservedName( string name )
        {
            if (string.IsNullOrEmpty(name)) return true;

            string[] reservedNames = new[]
            {
                "uncategorized", "all products", "default", "general", "admin", "api", "system", "root", "all", "none", "null", "undefined"
            };

            return !reservedNames.Contains(name.ToLowerInvariant());
        }

        private static bool NotContainHtml( string description )
        {
            if (string.IsNullOrEmpty(description)) return true;

            // Simple HTML tag detection
            return !Regex.IsMatch(description, @"<[^>]+>");
        }

        #endregion
    }
}
