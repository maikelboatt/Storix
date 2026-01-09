using System;
using System.Linq;
using FluentValidation;
using Storix.Application.DTO.Orders;
using Storix.Domain.Enums;

namespace Storix.Application.Validator.Orders
{
    public class UpdateOrderDtoValidator:AbstractValidator<UpdateOrderDto>
    {
        public UpdateOrderDtoValidator()
        {
            RuleFor(o => o.OrderId)
                .GreaterThan(0)
                .WithMessage("Valid order ID is required");

            RuleFor(o => o.Status)
                .IsInEnum()
                .WithMessage("Invalid order status");

            RuleFor(o => o.LocationId)
                .GreaterThan(0)
                .WithMessage("Valid location ID is required");


            RuleFor(o => o.DeliveryDate)
                .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
                .WithMessage("Delivery date cannot be in the past")
                .LessThanOrEqualTo(DateTime.UtcNow.AddYears(1))
                .WithMessage("Delivery date cannot be more than 1 year in the future")
                .When(o => o.DeliveryDate.HasValue);

            RuleFor(o => o.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes cannot exceed 1000 characters")
                .When(o => !string.IsNullOrEmpty(o.Notes));
        }
    }
}
