using FluentValidation;
using Storix.Application.DTO.Customers;

namespace Storix.Application.Validator.Customers
{
    public class UpdateCustomerDtoValidator:CustomerDtoValidatorBase<UpdateCustomerDto>
    {
        public UpdateCustomerDtoValidator()
        {
            // CustomerId validation
            RuleFor(c => c.CustomerId)
                .GreaterThan(0)
                .WithMessage("CustomerId must be greater than zero");

            // Use shared rule configurations
            ConfigureNameRules(c => c.Name);
            ConfigureEmailRules(c => c.Email!);
            ConfigurePhoneRules(c => c.Phone!);
            ConfigureAddressRules(c => c.Address!);
        }
    }
}
