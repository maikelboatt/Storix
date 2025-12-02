using System.Runtime.CompilerServices;
using FluentValidation;
using FluentValidation.Results;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Validator.Suppliers;

namespace Storix.Core.InputModel
{
    public class SupplierInputModel:MvxValidatingViewModel
    {
        private readonly CreateSupplierDtoValidator _createSupplierValidator = new();
        private readonly UpdateSupplierDtoValidator _updateSupplierValidator = new();

        // Backing fields for properties
        private int _supplierId;
        private string _name = string.Empty;
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private string _address = string.Empty;

        public SupplierInputModel()
        {
        }

        public SupplierInputModel( CreateSupplierDto? createSupplierDto ):this()
        {
            if (createSupplierDto != null)
            {
                LoadFromDto(createSupplierDto);
            }
            ValidateAllProperties();
        }

        public SupplierInputModel( UpdateSupplierDto? updateSupplierDto ):this()
        {
            if (updateSupplierDto != null)
            {
                LoadFromDto(updateSupplierDto);
            }
            ValidateAllProperties();
        }

        // Properties with validation
        public int SupplierId
        {
            get => _supplierId;
            set
            {
                if (SetProperty(ref _supplierId, value))
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
        public bool IsEditMode => _supplierId != 0;

        private void LoadFromDto( CreateSupplierDto dto )
        {
            _name = dto.Name;
            _email = dto.Email;
            _phone = dto.Phone;
            _address = dto.Address;
        }

        private void LoadFromDto( UpdateSupplierDto dto )
        {
            _supplierId = dto.SupplierId;
            _name = dto.Name;
            _email = dto.Email;
            _phone = dto.Phone;
            _address = dto.Address;
        }

        // Methods to convert back to DTOs
        public CreateSupplierDto ToCreateDto() => new()
        {
            Name = _name,
            Email = _email,
            Phone = _phone,
            Address = _address
        };

        public UpdateSupplierDto ToUpdateDto() => new()
        {
            SupplierId = _supplierId,
            Name = _name,
            Email = _email,
            Phone = _phone,
            Address = _address
        };

        private void ValidateProperty( object value, [CallerMemberName] string propertyName = "" )
        {
            ClearErrors(propertyName);

            // Pick the correct validator based on whether SupplierId is set
            ValidationResult result;

            if (_supplierId == 0)
            {
                CreateSupplierDto supplierDto = ToCreateDto();
                result = _createSupplierValidator.Validate(
                    supplierDto,
                    options => options.IncludeProperties(propertyName));
            }
            else
            {
                UpdateSupplierDto supplierDto = ToUpdateDto();
                result = _updateSupplierValidator.Validate(
                    supplierDto,
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

            if (_supplierId == 0)
            {
                CreateSupplierDto supplierDto = ToCreateDto();
                result = _createSupplierValidator.Validate(supplierDto);
            }
            else
            {
                UpdateSupplierDto supplierDto = ToUpdateDto();
                result = _updateSupplierValidator.Validate(supplierDto);
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
