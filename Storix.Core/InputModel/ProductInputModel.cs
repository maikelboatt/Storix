using System.Runtime.CompilerServices;
using FluentValidation;
using FluentValidation.Results;
using Storix.Application.DTO.Products;
using Storix.Application.Validator.Products;
using Storix.Core.ViewModels.Products;

namespace Storix.Core.InputModel
{
    public class ProductInputModel():MvxValidatingViewModel, IProductViewModel
    {
        private readonly CreateProductDtoValidator _createProductValidator = new();
        private readonly UpdateProductDtoValidator _updateProductValidator = new();

        // Backing fields for properties
        private string? _barcode;
        private int _categoryId;
        private decimal _cost;
        private string _description = string.Empty;
        private bool _isActive = true;
        private int _maxStockLevel;
        private int _minStockLevel;
        private string _name = string.Empty;
        private decimal _price;
        private int _productId;
        private string _sku = string.Empty;
        private int _supplierId;

        public ProductInputModel( CreateProductDto? createProductValidator ):this()
        {
            if (createProductValidator != null)
            {
                LoadFromDto(createProductValidator);
            }
            ValidateAllProperties();
        }

        public ProductInputModel( UpdateProductDto? updateProductValidator ):this()
        {
            if (updateProductValidator != null) LoadFromDto(updateProductValidator);
            ValidateAllProperties();
        }


        // Properties with validation
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

        public string SKU
        {
            get => _sku;
            set
            {
                if (SetProperty(ref _sku, value))
                {
                    ValidateProperty(value);
                }
            }
        }

        public string Description
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

        public string? Barcode
        {
            get => _barcode;
            set
            {
                if (SetProperty(ref _barcode, value))
                {
                    ValidateProperty(value!);
                }
            }
        }

        public decimal Price
        {
            get => _price;
            set
            {
                if (!SetProperty(ref _price, value)) return;
                ValidateProperty(value);
                // Also validate cost since it depends on price
                ValidateProperty(_cost, nameof(Cost));
                RaisePropertyChanged(() => ProfitMargin);
            }
        }

        public decimal Cost
        {
            get => _cost;
            set
            {
                if (!SetProperty(ref _cost, value)) return;
                ValidateProperty(value);
                RaisePropertyChanged(() => ProfitMargin);
            }
        }

        public int MinStockLevel
        {
            get => _minStockLevel;
            set
            {
                if (!SetProperty(ref _minStockLevel, value)) return;
                ValidateProperty(value);
                // Also validate max stock level since it depends on min
                ValidateProperty(_maxStockLevel, nameof(MaxStockLevel));
            }
        }

        public int MaxStockLevel
        {
            get => _maxStockLevel;
            set
            {
                if (SetProperty(ref _maxStockLevel, value))
                {
                    ValidateProperty(value);
                }
            }
        }

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

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        // Calculated properties
        public decimal ProfitMargin => Price - Cost;

        // Load data from DTO into backing fields
        private void LoadFromDto( CreateProductDto dto )
        {
            _name = dto.Name;
            _sku = dto.SKU;
            _description = dto.Description;
            _barcode = dto.Barcode;
            _price = dto.Price;
            _cost = dto.Cost;
            _minStockLevel = dto.MinStockLevel;
            _maxStockLevel = dto.MaxStockLevel;
            _supplierId = dto.SupplierId;
            _categoryId = dto.CategoryId;
        }

        private void LoadFromDto( UpdateProductDto dto )
        {
            _productId = dto.ProductId;
            _name = dto.Name;
            _sku = dto.SKU;
            _description = dto.Description;
            _barcode = dto.Barcode;
            _price = dto.Price;
            _cost = dto.Cost;
            _minStockLevel = dto.MinStockLevel;
            _maxStockLevel = dto.MaxStockLevel;
            _supplierId = dto.SupplierId;
            _categoryId = dto.CategoryId;
        }

        // Build DTO from current field values
        public CreateProductDto ToCreateDto() => new()
        {
            Name = _name,
            SKU = _sku,
            Description = _description,
            Barcode = _barcode,
            Price = _price,
            Cost = _cost,
            MinStockLevel = _minStockLevel,
            MaxStockLevel = _maxStockLevel,
            SupplierId = _supplierId,
            CategoryId = _categoryId
        };

        public UpdateProductDto ToUpdateDto() => new()
        {
            ProductId = _productId,
            Name = _name,
            SKU = _sku,
            Description = _description,
            Barcode = _barcode,
            Price = _price,
            Cost = _cost,
            MinStockLevel = _minStockLevel,
            MaxStockLevel = _maxStockLevel,
            SupplierId = _supplierId,
            CategoryId = _categoryId
        };

        private void ValidateProperty( object value, [CallerMemberName] string propertyName = "" )
        {
            ClearErrors(propertyName);


            ValidationResult result;

            if (_productId == 0)
            {
                CreateProductDto dto = ToCreateDto();
                result = _createProductValidator.Validate(dto, options => options.IncludeProperties(propertyName));
            }
            else
            {
                UpdateProductDto dto = ToUpdateDto();
                result = _updateProductValidator.Validate(dto, options => options.IncludeProperties(propertyName));
            }

            if (!result.IsValid)
            {
                List<string> propertyErrors = result.Errors.Where(e => e.PropertyName == propertyName)
                                                    .Select(e => e.ErrorMessage).ToList();
                if (propertyErrors.Count != 0)
                    AddErrors(propertyName, propertyErrors);
            }

            RaisePropertyChanged(() => IsValid);
        }

        private void ValidateAllProperties()
        {
            ClearAllErrors();

            ValidationResult result;

            if (_productId == 0)
            {
                CreateProductDto dto = ToCreateDto();
                result = _createProductValidator.Validate(dto);
            }
            else
            {
                UpdateProductDto dto = ToUpdateDto();
                result = _updateProductValidator.Validate(dto);
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
