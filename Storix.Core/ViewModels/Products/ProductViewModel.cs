using System.ComponentModel;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Products;
using Storix.Application.Services.Products.Interfaces;
using Storix.Core.InputModel;

namespace Storix.Core.ViewModels.Products
{
    public class ProductViewModel:MvxViewModel<int>, IProductViewModel
    {
        private readonly IProductService _productService;
        private ProductInputModel _inputModel;
        private bool _isEditMode;
        private bool _isLoading;
        private int _productId;

        // Constructor
        public ProductViewModel( IProductService productService )
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _isEditMode = false;

            // Initialize commands
            SaveCommand = new MvxAsyncCommand(ExecuteSaveCommandAsync, () => CanSave);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
            ResetCommand = new MvxCommand(ExecuteResetCommand, () => !IsLoading);
        }


        // Commands
        public IMvxCommand ResetCommand { get; }
        public IMvxCommand SaveCommand { get; }
        public IMvxCommand CancelCommand { get; }

        // Lifecycle methods
        public override Task Initialize()
        {
            if (_productId > 0)
                // Load existing product for editing
                LoadProductById(_productId);

            SubscribeToInputModelEvents();

            return base.Initialize();
        }

        // Navigation parameter support
        public override void Prepare( int parameters )
        {
            _productId = parameters;
        }

        // Business operations
        public CreateProductDto? GetCreateProductDto() => _inputModel?.ToCreateDto();

        public bool ValidateInput() => _inputModel?.Validate() ?? false;

        #region Methods

        private void LoadProduct( ProductDto productDto )
        {
            UpdateProductDto updateDto = productDto.ToUpdateDto();
            Input = new ProductInputModel(updateDto);
            IsEditMode = true;

            RaiseAllPropertiesChanged();
        }

        private void LoadProductById( int productId )
        {
            IsLoading = true;
            try
            {
                ProductDto? product = _productService.GetProductById(productId);
                if (product != null)
                {
                    LoadProduct(product);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ResetForm()
        {
            Input = new ProductInputModel();
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

                // Logic to close modal and notify success
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

        public string Title => IsEditMode ? "Edit Product" : "Create Product";
        public string SaveButtonText => IsEditMode ? "Update" : "Create";

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
