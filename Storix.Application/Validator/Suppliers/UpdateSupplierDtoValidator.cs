using FluentValidation;
using Storix.Application.DTO.Suppliers;

namespace Storix.Application.Validator.Suppliers
{
    public class UpdateSupplierDtoValidator:SupplerDtoValidatorBase<UpdateSupplierDto>
    {
        public UpdateSupplierDtoValidator()
        {
            // SupplierId validation
            RuleFor(s => s.SupplierId)
                .GreaterThan(0)
                .WithMessage("Supplier Id must be greater than zero");

            // Use shared rule configurations
            ConfigureNameRules(s => s.Name);
            ConfigureEmailRules(s => s.Email!);
            ConfigurePhoneRules(s => s.Phone!);
            ConfigureAddressRules(s => s.Address!);
        }
    }
}
