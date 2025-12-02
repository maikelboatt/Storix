using System.Runtime.CompilerServices;
using FluentValidation;
using FluentValidation.Results;
using Storix.Application.DTO.Customers;
using Storix.Application.Validator.Customers;

namespace Storix.Core.InputModel
{
    public class CustomerInputModel:MvxValidatingViewModel
    {
        private readonly CreateCustomerDtoValidator _createCustomerValidator = new();
        private readonly UpdateCustomerDtoValidator _updateCustomerValidator = new();

        // Backing fields for properties
        private int _customerId;
        private string _name = string.Empty;
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private string _address = string.Empty;

        public CustomerInputModel()
        {
        }

        public CustomerInputModel( CreateCustomerDto? createCustomerDto ):this()
        {
            if (createCustomerDto != null)
            {
                LoadFromDto(createCustomerDto);
            }
            ValidateAllProperties();
        }

        public CustomerInputModel( UpdateCustomerDto? updateCustomerDto ):this()
        {
            if (updateCustomerDto != null)
            {
                LoadFromDto(updateCustomerDto);
            }
            ValidateAllProperties();
        }

        // Properties with validation
        public int CustomerId
        {
            get => _customerId;
            set
            {
                if (SetProperty(ref _customerId, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public string Phone
        {
            get => _phone;
            set
            {
                if (SetProperty(ref _phone, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public string Address
        {
            get => _address;
            set
            {
                if (SetProperty(ref _address, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        // Helper properties
        public bool IsEditMode => _customerId != 0;

        private void LoadFromDto( CreateCustomerDto dto )
        {
            _name = dto.Name;
            _email = dto.Email;
            _phone = dto.Phone;
            _address = dto.Address;
        }

        private void LoadFromDto( UpdateCustomerDto dto )
        {
            _customerId = dto.CustomerId;
            _name = dto.Name;
            _email = dto.Email;
            _phone = dto.Phone;
            _address = dto.Address;
        }

        // Methods to convert back to DTOs
        public CreateCustomerDto ToCreateDto() => new()
        {
            Name = _name,
            Email = _email,
            Phone = _phone,
            Address = _address
        };

        public UpdateCustomerDto ToUpdateDto() => new()
        {
            CustomerId = _customerId,
            Name = _name,
            Email = _email,
            Phone = _phone,
            Address = _address
        };

        private void ValidateProperty( object value, [CallerMemberName] string propertyName = "" )
        {
            ClearErrors(propertyName);

            // Pick the correct validator based on whether CustomerId is set
            ValidationResult result;

            if (_customerId == 0)
            {
                CreateCustomerDto customerDto = ToCreateDto();
                result = _createCustomerValidator.Validate(
                    customerDto,
                    options => options.IncludeProperties(propertyName));
            }
            else
            {
                UpdateCustomerDto customerDto = ToUpdateDto();
                result = _updateCustomerValidator.Validate(
                    customerDto,
                    options => options.IncludeProperties(propertyName));
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

            if (_customerId == 0)
            {
                CreateCustomerDto customerDto = ToCreateDto();
                result = _createCustomerValidator.Validate(customerDto);
            }
            else
            {
                UpdateCustomerDto customerDto = ToUpdateDto();
                result = _updateCustomerValidator.Validate(customerDto);
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
