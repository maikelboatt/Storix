using System;
using System.Linq;
using FluentValidation;
using Storix.Application.DTO.Orders;
using Storix.Domain.Enums;

namespace Storix.Application.Validator.Orders
{
    public class CreateOrderDtoValidator:AbstractValidator<CreateOrderDto>
    {
        public CreateOrderDtoValidator()
        {
            RuleFor(o => o.Type)
                .IsInEnum()
                .WithMessage("Invalid order type");

            RuleFor(o => o.SupplierId)
                .NotNull()
                .WithMessage("Supplier is required for purchase orders")
                .GreaterThan(0)
                .WithMessage("Please select a valid supplier")
                .When(o => o.Type == OrderType.Purchase);

            RuleFor(o => o.CustomerId)
                .NotNull()
                .WithMessage("Customer is required for sale orders")
                .GreaterThan(0)
                .WithMessage("Please select a valid customer")
                .When(o => o.Type == OrderType.Sale);

            RuleFor(o => o.SupplierId)
                .Null()
                .WithMessage("Sale orders should not have a supplier")
                .When(o => o.Type == OrderType.Sale);

            RuleFor(o => o.CustomerId)
                .Null()
                .WithMessage("Purchase orders should not have a customer")
                .When(o => o.Type == OrderType.Purchase);

            RuleFor(o => o.OrderDate)
                .NotEmpty()
                .WithMessage("Order date is required")
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
                .WithMessage("Order date cannot be more than 1 day in the future")
                .GreaterThan(DateTime.UtcNow.AddYears(-5))
                .WithMessage("Order date cannot be more than 5 years in the past");

            RuleFor(o => o.DeliveryDate)
                .GreaterThanOrEqualTo(o => o.OrderDate)
                .WithMessage("Delivery date must be on or after order date")
                .LessThanOrEqualTo(DateTime.UtcNow.AddYears(1))
                .WithMessage("Delivery date cannot be more than 1 year in the future")
                .When(o => o.DeliveryDate.HasValue);

            RuleFor(o => o.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes cannot exceed 1000 characters")
                .When(o => !string.IsNullOrEmpty(o.Notes));

            RuleFor(o => o.CreatedBy)
                .GreaterThan(0)
                .WithMessage("Valid user ID is required");

            // Business rule: Must have either supplier or customer, but not both
            RuleFor(o => o)
                .Must(o => o.SupplierId.HasValue && !o.CustomerId.HasValue ||
                           !o.SupplierId.HasValue && o.CustomerId.HasValue)
                .WithMessage("Order must have either a supplier (for purchase) or customer (for sale), but not both");
        }
    }
}
