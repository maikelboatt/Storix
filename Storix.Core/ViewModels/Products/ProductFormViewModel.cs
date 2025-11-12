using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.Products;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Services.Products;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Application.Stores;
using Storix.Core.Control;
using Storix.Core.InputModel;
using Storix.Domain.Models;

namespace Storix.Core.ViewModels.Products
{
    public class ProductFormViewModel:MvxViewModel<int>, IProductViewModel
    {
        private readonly IProductService _productService;
        private readonly IProductCacheReadService _productCacheReadService;
        private readonly ICategoryCacheReadService _categoryCacheReadService;
        private readonly ISupplierCacheReadService _supplierCacheReadService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<ProductFormViewModel> _logger;
        private ProductInputModel _inputModel;
        private bool _isEditMode;
        private bool _isLoading;
        private int _productId;

        // Constructor
        public ProductFormViewModel( IProductService productService,
            IProductCacheReadService productCacheReadService,
            ICategoryCacheReadService categoryCacheReadService,
            ISupplierCacheReadService supplierCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<ProductFormViewModel> logger )
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _productCacheReadService = productCacheReadService;
            _categoryCacheReadService = categoryCacheReadService;
            _supplierCacheReadService = supplierCacheReadService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;
            _isEditMode = false;
            _inputModel = new ProductInputModel();


            // Initialize commands
            SaveCommand = new MvxAsyncCommand(ExecuteSaveCommandAsync, () => CanSave);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
            ResetCommand = new MvxCommand(ExecuteResetCommand, () => !IsLoading);
        }


        #region Commands

        public IMvxCommand ResetCommand { get; }
        public IMvxCommand SaveCommand { get; }
        public IMvxCommand CancelCommand { get; }

        #endregion

        #region Lifecycle methods

        public override void Prepare( int parameters )
        {
            _productId = parameters;
        }

        public override async Task Initialize()
        {
            IsLoading = true;

            try
            {
                // Load dropdown data first
                LoadDropdownDataFromCache();

                if (_productId > 0)
                {
                    // Load product for editing
                    LoadProductFromCache(_productId);
                }

                SubscribeToInputModelEvents();
            }
            finally
            {
                IsLoading = false;
            }

            await base.Initialize();
        }

        #endregion

        // Business operations
        public CreateProductDto? GetCreateProductDto() => _inputModel?.ToCreateDto();

        public bool ValidateInput() => _inputModel?.Validate() ?? false;

        #region Methods

        private void LoadDropdownDataFromCache()
        {
            // Load Categories from cache
            IEnumerable<CategoryDto> categories =
                _categoryCacheReadService.GetAllActiveCategoriesInCache();

            Input.Categories.Clear();
            foreach (CategoryDto category in categories)
            {
                Input.Categories.Add(category);
            }
            _logger.LogInformation(
                "✅ Loaded {CategoriesCount} categories from cache", Input.Categories.Count);

            // Load Suppliers from cache
            IEnumerable<SupplierDto> suppliers =
                _supplierCacheReadService.GetAllActiveSuppliersInCache();

            Input.Suppliers.Clear();
            foreach (SupplierDto supplier in suppliers)
            {
                Input.Suppliers.Add(supplier);
            }
            _logger.LogInformation(
                "✅ Loaded {SuppliersCount} suppliers from cache", Input.Suppliers.Count);
        }


        private void LoadProduct( ProductDto productDto )
        {
            UpdateProductDto updateDto = productDto.ToUpdateDto();

            // Store the current collections
            ObservableCollection<CategoryDto> categories = Input.Categories;
            ObservableCollection<SupplierDto> suppliers = Input.Suppliers;

            // Create new input model with the DTO
            Input = new ProductInputModel(updateDto)
            {
                Categories = categories,
                Suppliers = suppliers
            };

            IsEditMode = true;
            RaiseAllPropertiesChanged();
        }

        private void LoadProductFromCache( int productId )
        {
            ProductDto? product = _productCacheReadService.GetProductByIdFromCache(productId);
            if (product != null)
            {
                LoadProduct(product);
            }
        }

        private void ResetForm()
        {
            // Store the current collections
            ObservableCollection<CategoryDto> categories = Input.Categories;
            ObservableCollection<SupplierDto> suppliers = Input.Suppliers;

            // Create fresh input model
            Input = new ProductInputModel
            {
                Categories = categories,
                Suppliers = suppliers
            };

            IsEditMode = false;
            RaiseAllPropertiesChanged();
        }

        #endregion

        #region Command Implementations

        private async Task ExecuteSaveCommandAsync()
        {
            if (!_inputModel.Validate())
                return;

            IsLoading = true;

            try
            {
                if (IsEditMode)
                    await PerformUpdate();
                else
                    await PerformCreate();

                _modalNavigationControl.Close();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PerformCreate()
        {
            CreateProductDto createDto = _inputModel.ToCreateDto();
            await _productService.CreateProductAsync(createDto);
        }

        private async Task PerformUpdate()
        {
            UpdateProductDto updateDto = _inputModel.ToUpdateDto();
            await _productService.UpdateProductAsync(updateDto);
        }

        private void ExecuteCancelCommand()
        {
            // Close Modal
            _modalNavigationControl.Close();
        }

        private void ExecuteResetCommand()
        {
            ResetForm();
        }

        #endregion

        #region Properties

        public ProductInputModel Input
        {
            get => _inputModel;
            private set
            {
                if (_inputModel != value)
                {
                    UnsubscribeFromInputModelEvents();
                    SetProperty(ref _inputModel, value);
                    SubscribeToInputModelEvents();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, () => RaisePropertyChanged(() => CanSave));
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            private set => SetProperty(ref _isEditMode, value);
        }

        public string Title => IsEditMode
            ? "Edit Product"
            : "Create Product";

        public string SaveButtonText => IsEditMode
            ? "Update"
            : "Create";

        // Validation state (delegated to input model)
        public bool IsValid => _inputModel?.IsValid ?? false;
        public bool HasErrors => _inputModel?.HasErrors ?? false;

        // Command availability
        public bool CanSave => IsValid && !IsLoading;
        public bool CanCancel => !IsLoading;

        #endregion

        #region Event Handling

        private void SubscribeToInputModelEvents()
        {
            if (_inputModel == null) return;
            _inputModel.PropertyChanged += OnInputModelPropertyChanged;
            _inputModel.ErrorsChanged += OnInputModelErrorsChanged;
        }

        private void UnsubscribeFromInputModelEvents()
        {
            if (_inputModel != null)
            {
                _inputModel.PropertyChanged -= OnInputModelPropertyChanged;
                _inputModel.ErrorsChanged -= OnInputModelErrorsChanged;
            }
        }

        private void OnInputModelPropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);

            // Refresh commands
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void OnInputModelErrorsChanged( object sender, DataErrorsChangedEventArgs e )
        {
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);

            // Refresh commands
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void RaiseAllPropertiesChanged()
        {
            RaisePropertyChanged(() => Title);
            RaisePropertyChanged(() => SaveButtonText);
            RaisePropertyChanged(() => IsEditMode);
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);
            RaisePropertyChanged(() => CanCancel);
        }

        #endregion
    }
}
