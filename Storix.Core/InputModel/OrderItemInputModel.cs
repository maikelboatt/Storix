using System.Runtime.CompilerServices;
using FluentValidation;
using FluentValidation.Results;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.Validator.OrderItems;
using Storix.Application.Validator.Orders;
using Storix.Core.ViewModels.Orders;

namespace Storix.Core.InputModel
{
    public class OrderItemInputModel():MvxValidatingViewModel
    {
        private readonly CreateOrderItemDtoValidator _createOrderItemValidator = new();
        private readonly UpdateOrderItemDtoValidator _updateOrderItemValidator = new();

        // Backing fields for properties
        private int _orderItemId;
        private int _orderId;
        private int _productId;
        private int _quantity;
        private decimal _unitPrice;

        public OrderItemInputModel( CreateOrderItemDto? createOrderItemDto ):this()
        {
            if (createOrderItemDto != null)
            {
                LoadFromDto(createOrderItemDto);
            }
            ValidateAllProperties();
        }

        public OrderItemInputModel( UpdateOrderItemDto? updateOrderItemDto ):this()
        {
            if (updateOrderItemDto != null)
            {
                LoadFromDto(updateOrderItemDto);
            }
            ValidateAllProperties();
        }

        // Properties with validation
        public int OrderItemId
        {
            get => _orderItemId;
            set
            {
                if (SetProperty(ref _orderItemId, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public int OrderId
        {
            get => _orderId;
            set
            {
                if (SetProperty(ref _orderId, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public int ProductId
        {
            get => _productId;
            set
            {
                if (SetProperty(ref _productId, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        private void LoadFromDto( CreateOrderItemDto dto )
        {
            _orderId = dto.OrderId;
            _productId = dto.ProductId;
            _quantity = dto.Quantity;
            _unitPrice = dto.UnitPrice;
        }

        private void LoadFromDto( UpdateOrderItemDto dto )
        {
            _orderItemId = dto.OrderItemId;
            _quantity = dto.Quantity;
            _unitPrice = dto.UnitPrice;
        }

        // Methods to convert back to DTOs
        public CreateOrderItemDto ToCreateDto() => new()
        {
            OrderId = _orderId,
            ProductId = _productId,
            Quantity = _quantity,
            UnitPrice = _unitPrice
        };

        public UpdateOrderItemDto ToUpdateDto() => new()
        {
            OrderItemId = _orderItemId,
            Quantity = _quantity,
            UnitPrice = _unitPrice
        };

        private void ValidateProperty( object value, [CallerMemberName] string propertyName = "" )
        {
            ClearErrors(propertyName);

            // Pick the correct validator based on whether OrderItemId is set
            IValidator validator = _orderItemId == 0
                ? _createOrderItemValidator
                : _updateOrderItemValidator;

            ValidationResult result;

            if (_orderItemId == 0)
            {
                CreateOrderItemDto orderItemDto = ToCreateDto();
                result = _createOrderItemValidator.Validate(orderItemDto, options => options.IncludeProperties(propertyName));
            }
            else
            {
                UpdateOrderItemDto orderItemDto = ToUpdateDto();
                result = _updateOrderItemValidator.Validate(orderItemDto, options => options.IncludeProperties(propertyName));
            }

            if (!result.IsValid)
            {
                List<string> propertyErrors = result
                                              .Errors.Where(e => e.PropertyName == propertyName)
                                              .Select(e => e.ErrorMessage)
                                              .ToList();
                if (propertyErrors.Count != 0)
                    AddErrors(propertyName, propertyErrors);
            }

            RaisePropertyChanged(() => IsValid);
        }

        private void ValidateAllProperties()
        {
            ClearAllErrors();

            ValidationResult result;

            if (_orderItemId == 0)
            {
                CreateOrderItemDto orderItemDto = ToCreateDto();
                result = _createOrderItemValidator.Validate(orderItemDto);
            }
            else
            {
                UpdateOrderItemDto orderItemDto = ToUpdateDto();
                result = _updateOrderItemValidator.Validate(orderItemDto);
            }

            if (!result.IsValid)
            {
                foreach (ValidationFailure error in result.Errors)
                {
                    AddError(error.PropertyName, error.ErrorMessage);
                }
            }

            RaisePropertyChanged(() => IsValid);
        }

        public bool Validate()
        {
            ValidateAllProperties();
            return IsValid;
        }
    }
}
