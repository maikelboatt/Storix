using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentValidation;
using FluentValidation.Results;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Customers;
using Storix.Application.DTO.Orders;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Validator.Orders;
using Storix.Domain.Enums;

namespace Storix.Core.InputModel
{
    public class OrderInputModel:MvxValidatingViewModel
    {
        private readonly CreateOrderDtoValidator _createOrderValidator = new();
        private readonly UpdateOrderDtoValidator _updateOrderValidator = new();

        // Backing fields for properties
        private int _orderId;
        private OrderType _type;
        private OrderStatus _status = OrderStatus.Draft;
        private int? _supplierId;
        private int? _customerId;
        private DateTime _orderDate;
        private DateTime? _deliveryDate;
        private string? _notes;
        private int _createdBy;

        public OrderInputModel()
        {
        }

        public OrderInputModel( CreateOrderDto? createOrderDto ):this()
        {
            if (createOrderDto != null)
            {
                LoadFromDto(createOrderDto);
            }
            ValidateAllProperties();
        }

        public OrderInputModel( UpdateOrderDto? updateOrderDto ):this()
        {
            if (updateOrderDto != null)
            {
                LoadFromDto(updateOrderDto);
            }
            ValidateAllProperties();
        }

        // Collections for dropdowns
        public ObservableCollection<CustomerDto> Customers { get; set; } = new();
        public ObservableCollection<SupplierDto> Suppliers { get; set; } = new();

        // Properties with validation
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

        public OrderType Type
        {
            get => _type;
            set
            {
                if (SetProperty(ref _type, value))
                {
                    ValidateProperty(value);
                    RaisePropertyChanged(() => IsSalesOrder);
                    RaisePropertyChanged(() => IsPurchaseOrder);
                }
            }
        }

        public OrderStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public int? SupplierId
        {
            get => _supplierId;
            set
            {
                if (SetProperty(ref _supplierId, value))
                {
                    ValidateProperty(value!);
                }
            }
        }

        public int? CustomerId
        {
            get => _customerId;
            set
            {
                if (SetProperty(ref _customerId, value))
                {
                    ValidateProperty(value!);
                }
            }
        }

        public DateTime OrderDate
        {
            get => _orderDate;
            set
            {
                if (SetProperty(ref _orderDate, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public DateTime? DeliveryDate
        {
            get => _deliveryDate;
            set
            {
                if (SetProperty(ref _deliveryDate, value))
                {
                    ValidateProperty(value!);
                    RaisePropertyChanged(() => IsOverdue);
                }
            }
        }

        public string? Notes
        {
            get => _notes;
            set
            {
                if (SetProperty(ref _notes, value))
                {
                    ValidateProperty(value!);
                }
            }
        }

        public int CreatedBy
        {
            get => _createdBy;
            set
            {
                if (SetProperty(ref _createdBy, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        // Computed properties
        public bool IsOverdue => _deliveryDate.HasValue && _deliveryDate < DateTime.UtcNow;
        public bool IsSalesOrder => _type == OrderType.Sale;
        public bool IsPurchaseOrder => _type == OrderType.Purchase;

        private void LoadFromDto( CreateOrderDto dto )
        {
            _type = dto.Type;
            _supplierId = dto.SupplierId;
            _customerId = dto.CustomerId;
            _orderDate = dto.OrderDate;
            _deliveryDate = dto.DeliveryDate;
            _notes = dto.Notes;
            _createdBy = dto.CreatedBy;
        }

        private void LoadFromDto( UpdateOrderDto dto )
        {
            _orderId = dto.OrderId;
            _status = dto.Status;
            _deliveryDate = dto.DeliveryDate;
            _notes = dto.Notes;
        }

        // Methods to convert back to DTOs
        public CreateOrderDto ToCreateDto() => new()
        {
            Type = _type,
            SupplierId = _supplierId,
            CustomerId = _customerId,
            OrderDate = _orderDate,
            DeliveryDate = _deliveryDate,
            Notes = _notes,
            CreatedBy = _createdBy
        };

        public UpdateOrderDto ToUpdateDto() => new()
        {
            OrderId = _orderId,
            Status = _status,
            DeliveryDate = _deliveryDate,
            Notes = _notes
        };

        private void ValidateProperty( object value, [CallerMemberName] string propertyName = "" )
        {
            ClearErrors(propertyName);

            ValidationResult result;

            if (_orderId == 0)
            {
                CreateOrderDto orderDto = ToCreateDto();
                result = _createOrderValidator.Validate(orderDto, options => options.IncludeProperties(propertyName));
            }
            else
            {
                UpdateOrderDto orderDto = ToUpdateDto();
                result = _updateOrderValidator.Validate(orderDto, options => options.IncludeProperties(propertyName));
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

            if (_orderId == 0)
            {
                CreateOrderDto orderDto = ToCreateDto();
                result = _createOrderValidator.Validate(orderDto);
            }
            else
            {
                UpdateOrderDto orderDto = ToUpdateDto();
                result = _updateOrderValidator.Validate(orderDto);
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
