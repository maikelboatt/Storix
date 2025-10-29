using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using FluentValidation;

namespace Storix.Application.Validator.Suppliers
{
    /// <summary>
    /// Basic validator containing shared validation logic for the Supplier Dto.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SupplerDtoValidatorBase<T>:AbstractValidator<T>
    {
        #region Shared Custom Validation Methods

        protected static bool BeValidName( string fullName ) => string.IsNullOrEmpty(fullName) ||
                                                                // Optional field
                                                                // Letters, spaces, hyphens, apostrophes, periods (for initials)
                                                                Regex.IsMatch(fullName, @"^[a-zA-Z\s\-'.]+$");

        protected static bool NotBeDisposableEmail( string email )
        {
            if (string.IsNullOrEmpty(email)) return true;

            // Common disposable email domains
            string[] disposableDomains =
            [
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
            ];

            string? domain = email
                             .Split('@')
                             .LastOrDefault()
                             ?.ToLowerInvariant();
            return !disposableDomains.Contains(domain);
        }

        protected static bool BeValidAddress( string address )
        {
            if (string.IsNullOrWhiteSpace(address)) return false; // Required field

            // Reasonable length (not too short, not too long)
            if (address.Length < 5 || address.Length > 500) return false;

            // Contains at least one letter or digit (not just symbols)
            return Regex.IsMatch(address, @"[a-zA-Z0-9]");
        }

        /// <summary>
        /// Validates phone number format. Supports international formats.
        /// Optional field - returns true if null/empty.
        /// Accepts formats like:
        /// - +1234567890
        /// - +1 (234) 567-8900
        /// - (234) 567-8900
        /// - 234-567-8900
        /// - 234.567.8900
        /// - 234 567 8900
        /// - 2345678900
        /// </summary>
        protected static bool BeValidPhoneNumber( string phone )
        {
            if (string.IsNullOrEmpty(phone)) return true; // Optional field

            // Remove all whitespace for length check
            string digitsOnly = Regex.Replace(phone, @"\D", "");

            // Phone number should have between 10 and 15 digits (international standard)
            if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
                return false;

            // Validate format: optional +, digits, spaces, hyphens, parentheses, and periods
            // Must start with + or digit, must end with digit
            return Regex.IsMatch(phone, @"^[\+]?[(]?[0-9]{1,4}[)]?[-\s\.]?[(]?[0-9]{1,4}[)]?[-\s\.]?[0-9]{1,5}[-\s\.]?[0-9]{1,5}$");
        }

        #endregion

        #region Shared Rule Definitions

        protected void ConfigureNameRules( Expression<Func<T, string>> fullNameSelector )
        {
            RuleFor(fullNameSelector)
                .MaximumLength(100)
                .WithMessage("Full name cannot exceed 100 characters")
                .Must(BeValidName)
                .WithMessage("Full name contains invalid characters")
                .When(dto => !string.IsNullOrEmpty(
                          fullNameSelector
                              .Compile()(dto)));
        }

        protected void ConfigureAddressRules( Expression<Func<T, string>> addressSelector )
        {
            RuleFor(addressSelector)
                .Length(5, 500)
                .WithMessage("Address must be between 5 and 500 characters")
                .Must(BeValidAddress)
                .WithMessage("Address contains invalid characters")
                .When(dto => !string.IsNullOrEmpty(
                          addressSelector
                              .Compile()(dto)));
        }

        protected void ConfigurePhoneRules( Expression<Func<T, string>> phoneSelector )
        {
            RuleFor(phoneSelector)
                .Length(10, 15)
                .WithMessage("Phone number must be between 10 and 15 digits")
                .Must(BeValidPhoneNumber)
                .WithMessage("Phone contains invalid digits")
                .When(dto => !string.IsNullOrEmpty(
                          phoneSelector
                              .Compile()(dto)));
        }

        protected void ConfigureEmailRules( Expression<Func<T, string>> emailSelector )
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
