using FluentValidation;
using Storix.Application.DTO.Products;

namespace Storix.Application.Validator.Products
{
    public class CreateProductDtoValidator:AbstractValidator<CreateProductDto>
    {
        public CreateProductDtoValidator()
        {
            RuleFor(p => p.Name)
                .NotEmpty().WithMessage("Product name is required")
                .Length(2, 100).WithMessage("Product name must be between 2 and 100 characters");

            RuleFor(p => p.SKU)
                .NotEmpty().WithMessage("SKU is required")
                .Matches(@"^[A-Z]{2,3}-\d{3,6}$").WithMessage("SKU must follow format: ABC-1234");

            RuleFor(p => p.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

            RuleFor(p => p.Barcode)
                .Matches(@"^\d{12,13}$").WithMessage("Barcode must be 12-13 digits")
                .When(p => !string.IsNullOrEmpty(p.Barcode));

            RuleFor(p => p.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0")
                .LessThan(100000).WithMessage("Price cannot exceed $100,000");

            RuleFor(p => p.Cost)
                .GreaterThanOrEqualTo(0).WithMessage("Cost cannot be negative")
                .LessThan(p => p.Price).WithMessage("Cost must be less than price");

            RuleFor(p => p.MinStockLevel)
                .GreaterThanOrEqualTo(0).WithMessage("Minimum stock level cannot be negative");

            RuleFor(p => p.MaxStockLevel)
                .GreaterThan(p => p.MinStockLevel).WithMessage("Maximum stock level must be greater than minimum stock level");

            RuleFor(p => p.SupplierId)
                .GreaterThan(0).WithMessage("Please select a valid supplier");

            RuleFor(p => p.CategoryId)
                .GreaterThan(0).WithMessage("Please select a valid category");
        }
    }
}
