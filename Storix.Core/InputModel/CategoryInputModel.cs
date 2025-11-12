using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using FluentValidation;
using FluentValidation.Results;
using Storix.Application.DTO.Categories;
using Storix.Application.Validator.Categories;
using Storix.Core.ViewModels.Categories;

namespace Storix.Core.InputModel
{
    public class CategoryInputModel():MvxValidatingViewModel, ICategoryViewModel
    {
        private readonly CreateCategoryDtoValidator _createCategoryValidator = new();
        private readonly UpdateCategoryDtoValidator _updateCategoryValidator = new();

        // Backing fields for properties
        private int _categoryId;
        private string? _description;
        private string? _imageUrl = string.Empty;
        private string _name = string.Empty;
        private int? _parentCategoryId;
        private bool _isActive = true;

        public CategoryInputModel( CreateCategoryDto? createCategoryDto ):this()
        {
            if (createCategoryDto != null)
            {
                LoadFromDto(createCategoryDto);
            }
            ValidateAllProperties();
        }

        public CategoryInputModel( UpdateCategoryDto? updateCategoryDto ):this()
        {
            if (updateCategoryDto != null)
            {
                LoadFromDto(updateCategoryDto);
            }
            ValidateAllProperties();
        }

        // Collections for dropdowns
        public ObservableCollection<CategoryDto> ParentCategories { get; set; } = [];

        // Properties with validation
        public int CategoryId
        {
            get => _categoryId;
            set
            {
                if (SetProperty(ref _categoryId, value))
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
                    ValidateProperty(value!);
                }
            }
        }
        public string? ImageUrl
        {
            get => _imageUrl;
            set
            {
                if (SetProperty(ref _imageUrl, value))
                {
                    ValidateProperty(value!);
                    RaisePropertyChanged(() => HasImageUrl);
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
                    ValidateProperty(value!);
                }
            }
        }
        public int? ParentCategoryId
        {
            get => _parentCategoryId;
            set
            {
                if (SetProperty(ref _parentCategoryId, value))
                {
                    ValidateProperty(value!);
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        // Calculated properties
        public bool HasImageUrl => !string.IsNullOrWhiteSpace(_imageUrl);
        public bool IsImageLoading => false; // Can be used for async image loading status


        private void LoadFromDto( CreateCategoryDto dto )
        {
            _name = dto.Name;
            _description = dto.Description;
            _parentCategoryId = dto.ParentCategoryId;
            _imageUrl = dto.ImageUrl;
        }

        private void LoadFromDto( UpdateCategoryDto dto )
        {
            _categoryId = dto.CategoryId;
            _name = dto.Name;
            _description = dto.Description;
            _parentCategoryId = dto.ParentCategoryId;
            _imageUrl = dto.ImageUrl;
        }

        // Methods to convert back to DTOs
        public CreateCategoryDto ToCreateDto() => new()
        {
            Name = _name,
            Description = _description,
            ParentCategoryId = _parentCategoryId,
            ImageUrl = _imageUrl
        };

        public UpdateCategoryDto ToUpdateDto() => new()
        {
            CategoryId = _categoryId,
            Name = _name,
            Description = _description,
            ParentCategoryId = _parentCategoryId,
            ImageUrl = _imageUrl
        };

        private void ValidateProperty( object value, [CallerMemberName] string propertyName = "" )
        {
            ClearErrors(propertyName);

            // Pick the correct validator based on whether CategoryId is set
            IValidator validator = _categoryId == 0
                ? _createCategoryValidator
                : _updateCategoryValidator;

            ValidationResult result;

            if (_categoryId == 0)
            {
                CreateCategoryDto categoryDto = ToCreateDto();
                result = _createCategoryValidator.Validate(categoryDto, options => options.IncludeProperties(propertyName));
            }
            else
            {
                UpdateCategoryDto categoryDto = ToUpdateDto();
                result = _updateCategoryValidator.Validate(categoryDto, options => options.IncludeProperties(propertyName));
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

            if (_categoryId == 0)
            {
                CreateCategoryDto categoryDto = ToCreateDto();
                result = _createCategoryValidator.Validate(categoryDto);
            }
            else
            {
                UpdateCategoryDto categoryDto = ToUpdateDto();
                result = _updateCategoryValidator.Validate(categoryDto);
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

        // Helper method to get the appropriate validator and DTO
        private (IValidator validator, object dto) GetValidatorAndDto()
        {
            if (_categoryId == 0)
            {
                return (_createCategoryValidator, ToCreateDto());
            }
            return (_updateCategoryValidator, ToUpdateDto());
        }

        public bool Validate()
        {
            ValidateAllProperties();
            return IsValid;
        }
    }
}
