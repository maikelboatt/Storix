using System.Runtime.CompilerServices;
using FluentValidation;
using FluentValidation.Results;
using Storix.Application.DTO.Locations;
using Storix.Application.Validator.Locations;
using Storix.Domain.Enums;

namespace Storix.Core.InputModel
{
    public class LocationInputModel:MvxValidatingViewModel
    {
        private readonly CreateLocationDtoValidator _createLocationValidator = new();
        private readonly UpdateLocationDtoValidator _updateLocationValidator = new();

        // Backing fields for properties
        private int _locationId;
        private string _name = string.Empty;
        private string? _description = string.Empty;
        private LocationType _type;
        private string? _address = string.Empty;

        public LocationInputModel()
        {
        }

        public LocationInputModel( CreateLocationDto? createLocationDto ):this()
        {
            if (createLocationDto != null)
            {
                LoadFromDto(createLocationDto);
            }
            ValidateAllProperties();
        }

        public LocationInputModel( UpdateLocationDto? updateLocationDto ):this()
        {
            if (updateLocationDto != null)
            {
                LoadFromDto(updateLocationDto);
            }
            ValidateAllProperties();
        }

        // Properties with validation
        public int LocationId
        {
            get => _locationId;
            set
            {
                if (SetProperty(ref _locationId, value))
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

        public string? Description
        {
            get => _description;
            set
            {
                if (SetProperty(ref _description, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public LocationType Type
        {
            get => _type;
            set
            {
                if (SetProperty(ref _type, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public string? Address
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
        public bool IsEditMode => _locationId != 0;

        private void LoadFromDto( CreateLocationDto dto )
        {
            _name = dto.Name;
            _description = dto.Description;
            _type = dto.Type;
            _address = dto.Address;
        }

        private void LoadFromDto( UpdateLocationDto dto )
        {
            _locationId = dto.LocationId;
            _name = dto.Name;
            _description = dto.Description;
            _type = dto.Type;
            _address = dto.Address;
        }

        // Methods to convert back to DTOs
        public CreateLocationDto ToCreateDto() => new()
        {
            Name = _name,
            Description = _description,
            Type = _type,
            Address = _address
        };

        public UpdateLocationDto ToUpdateDto() => new()
        {
            LocationId = _locationId,
            Name = _name,
            Description = _description,
            Type = _type,
            Address = _address
        };

        private void ValidateProperty( object? value, [CallerMemberName] string propertyName = "" )
        {
            ClearErrors(propertyName);

            // Pick the correct validator based on whether LocationId is set
            ValidationResult result;

            if (_locationId == 0)
            {
                CreateLocationDto locationDto = ToCreateDto();
                result = _createLocationValidator.Validate(
                    locationDto,
                    options => options.IncludeProperties(propertyName));
            }
            else
            {
                UpdateLocationDto locationDto = ToUpdateDto();
                result = _updateLocationValidator.Validate(
                    locationDto,
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

            if (_locationId == 0)
            {
                CreateLocationDto locationDto = ToCreateDto();
                result = _createLocationValidator.Validate(locationDto);
            }
            else
            {
                UpdateLocationDto locationDto = ToUpdateDto();
                result = _updateLocationValidator.Validate(locationDto);
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
