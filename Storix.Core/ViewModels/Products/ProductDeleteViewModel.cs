using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Products;
using Storix.Application.Services.Products;
using Storix.Application.Services.Products.Interfaces;
using Storix.Core.Control;

namespace Storix.Core.ViewModels.Products
{
    /// <summary>
    /// ViewModel for product deletion confirmation dialog.
    /// Displays full product details and handles the deletion operation.
    /// </summary>
    public class ProductDeleteViewModel:MvxViewModel<int>, IProductViewModel
    {
        private readonly IProductService _productService;
        private readonly IProductCacheReadService _productCacheReadService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<ProductDeleteViewModel> _logger;

        private ProductDto? _product;
        private bool _isLoading;
        private int _productId;

        #region Constructor

        public ProductDeleteViewModel(
            IProductService productService,
            IProductCacheReadService productCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<ProductDeleteViewModel> logger )
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _productCacheReadService = productCacheReadService ?? throw new ArgumentNullException(nameof(productCacheReadService));
            _modalNavigationControl = modalNavigationControl ?? throw new ArgumentNullException(nameof(modalNavigationControl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize commands
            DeleteCommand = new MvxAsyncCommand(ExecuteDeleteCommandAsync, () => CanDelete);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
        }

        #endregion

        #region Commands

        public IMvxCommand DeleteCommand { get; }
        public IMvxCommand CancelCommand { get; }

        #endregion

        #region Lifecycle Methods

        public override void Prepare( int parameter )
        {
            _productId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;

            try
            {
                await LoadProductAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load product with ID: {ProductId}", _productId);
                // Optionally show error message to user
                _modalNavigationControl.Close();
            }
            finally
            {
                IsLoading = false;
            }

            await base.Initialize();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The product to be deleted with all its details
        /// </summary>
        public ProductDto? Product
        {
            get => _product;
            private set => SetProperty(
                ref _product,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanDelete);
                    DeleteCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Indicates whether a deletion operation is in progress
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(
                ref _isLoading,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanDelete);
                    RaisePropertyChanged(() => CanCancel);
                    DeleteCommand.RaiseCanExecuteChanged();
                    CancelCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Whether the delete command can be executed
        /// </summary>
        public bool CanDelete => Product != null && !IsLoading;

        /// <summary>
        /// Whether the cancel command can be executed
        /// </summary>
        public bool CanCancel => !IsLoading;

        #endregion

        #region Methods

        /// <summary>
        /// Loads the product details from cache
        /// </summary>
        private async Task LoadProductAsync()
        {
            _logger.LogInformation("📦 Loading product with ID: {ProductId}", _productId);

            // Load from cache (synchronous)
            ProductDto? product = _productCacheReadService.GetProductByIdFromCache(_productId);

            if (product == null)
            {
                _logger.LogWarning("⚠️ Product with ID {ProductId} not found in cache", _productId);

                // Optionally: Try loading directly from service/database
                // product = await _productService.GetProductByIdAsync(_productId);
            }

            Product = product;

            if (Product != null)
            {
                _logger.LogInformation(
                    "✅ Loaded product: {ProductName} (SKU: {SKU})",
                    Product.Name,
                    Product.SKU);
            }

            await Task.CompletedTask;
        }

        #endregion

        #region Command Implementations

        /// <summary>
        /// Executes the product deletion
        /// </summary>
        private async Task ExecuteDeleteCommandAsync()
        {
            if (Product == null)
            {
                _logger.LogWarning("⚠️ Cannot delete: Product is null");
                return;
            }

            IsLoading = true;

            try
            {
                _logger.LogInformation(
                    "🗑️ Deleting product: {ProductId} - {ProductName}",
                    Product.ProductId,
                    Product.Name);

                await _productService.SoftDeleteProductAsync(Product.ProductId);

                _logger.LogInformation(
                    "✅ Successfully deleted product: {ProductId} - {ProductName}",
                    Product.ProductId,
                    Product.Name);

                // Close the modal after successful deletion
                _modalNavigationControl.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Failed to delete product: {ProductId} - {ProductName}",
                    Product.ProductId,
                    Product.Name);

                // TODO: Show error message to user
                // You might want to add an error message property or use a message service
                // ErrorMessage = "Failed to delete product. Please try again.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Cancels the deletion and closes the modal
        /// </summary>
        private void ExecuteCancelCommand()
        {
            _logger.LogInformation("❌ Product deletion cancelled by user");
            _modalNavigationControl.Close();
        }

        #endregion
    }
}
