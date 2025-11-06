using Storix.Application.DTO.Customers;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Validator.Customers;

namespace Storix.Application.Validator.Suppliers
{
    public class CreateSupplierDtoValidator:SupplerDtoValidatorBase<CreateSupplierDto>
    {
        public CreateSupplierDtoValidator()
        {
            // Use shared rule configurations
            ConfigureNameRules(s => s.Name);
            ConfigureEmailRules(s => s.Email!);
            ConfigurePhoneRules(s => s.Phone!);
            ConfigureAddressRules(s => s.Address!);
        }
    }
}
