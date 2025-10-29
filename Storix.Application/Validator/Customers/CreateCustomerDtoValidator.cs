using FluentValidation;
using Storix.Application.DTO.Customers;

namespace Storix.Application.Validator.Customers
{
    public class CreateCustomerDtoValidator:CustomerDtoValidatorBase<CreateCustomerDto>
    {
        public CreateCustomerDtoValidator()
        {
            // Use shared rule configurations
            ConfigureNameRules(c => c.Name);
            ConfigureEmailRules(c => c.Email!);
            ConfigurePhoneRules(c => c.Phone!);
            ConfigureAddressRules(c => c.Address!);
        }
    }
}
