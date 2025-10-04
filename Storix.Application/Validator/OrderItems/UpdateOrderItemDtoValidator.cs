using FluentValidation;
using Storix.Application.DTO.OrderItems;

namespace Storix.Application.Validator.OrderItems
{
    public class UpdateOrderItemDtoValidator:AbstractValidator<UpdateOrderItemDto>
    {
        public UpdateOrderItemDtoValidator()
        {
            RuleFor(oi => oi.OrderItemId)
                .GreaterThan(0)
                .WithMessage("Valid order item ID is required");

            RuleFor(oi => oi.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero")
                .LessThanOrEqualTo(10000)
                .WithMessage("Quantity cannot exceed 10,000 units");

            RuleFor(oi => oi.UnitPrice)
                .GreaterThan(0)
                .WithMessage("Unit price must be greater than zero")
                .LessThanOrEqualTo(1000000)
                .WithMessage("Unit price cannot exceed 1,000,000")
                .PrecisionScale(18, 2, false)
                .WithMessage("Unit price can have maximum 2 decimal places");

        }
    }
}
