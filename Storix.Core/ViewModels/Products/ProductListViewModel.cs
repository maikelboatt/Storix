using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.Products;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Managers;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Services.Products;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Application.Stores.Products;
using Storix.Core.Control;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Core.ViewModels.Products
{
    public class ProductListViewModel:MvxViewModel<ProductListViewModelParameter>
    {
        private readonly IProductService _productService;
        private readonly IInventoryManager _inventoryManager;
        private readonly IProductStore _productStore;
        private readonly ICategoryCacheReadService _categoryCacheReadService;
        private readonly ISupplierCacheReadService _supplierCacheReadService;
        private readonly IProductCacheReadService _productCacheReadService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<ProductListViewModel> _logger;

        private MvxObservableCollection<ProductListItemViewModel> _products = [];
        private List<ProductListItemViewModel> _allProducts = [];
        private ProductListViewModelParameter? _filter;
        private string _searchText = string.Empty;
        private bool _isLoading;
        private string _filterTitle;
        private string _filterSubtitle;
        private bool _isFiltered;

        public ProductListViewModel(
            IProductService productService,
            IInventoryManager inventoryManager,
            IProductStore productStore,
            IProductCacheReadService productCacheReadService,
            ICategoryCacheReadService categoryCacheReadService,
            ISupplierCacheReadService supplierCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<ProductListViewModel> logger )
        {
            _productService = productService;
            _inventoryManager = inventoryManager;
            _productStore = productStore;
            _productCacheReadService = productCacheReadService;
            _categoryCacheReadService = categoryCacheReadService;
            _supplierCacheReadService = supplierCacheReadService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Subscribe to write operation events from the store
            _productStore.ProductAdded += OnProductAdded;
            _productStore.ProductUpdated += OnProductUpdated;
            _productStore.ProductDeleted += OnProductDeleted;
            _productStore.ProductStockChanged += OnProductStockChanged;

            // Initialize commands
            OpenProductFormCommand = new MvxCommand<int>(ExecuteProductForm);
            OpenProductDetailsCommand = new MvxCommand<int>(ExecuteProductDetails);
            OpenProductDeleteCommand = new MvxCommand<int>(ExecuteOpenProductDelete);
            CloseCommand = new MvxCommand(ExecuteCloseCommand);
        }

        public override void Prepare( ProductListViewModelParameter parameter )
        {
            _filter = parameter;

            _logger.LogInformation(
                "📦 Preparing ProductListViewModel with filter: {FilterType}, EntityId: {EntityId}",
                _filter.FilterType,
                _filter.EntityId);
        }

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                if (_filter is null)
                {
                    await LoadProducts();
                }
                else
                {
                    await LoadProductsAsync();
                }
            }
            finally
            {
                IsLoading = false;
            }

            await base.Initialize();
        }

        public override void ViewDestroy( bool viewFinishing = true )
        {
            // Unsubscribe from store events to prevent memory leaks
            _productStore.ProductAdded -= OnProductAdded;
            _productStore.ProductUpdated -= OnProductUpdated;
            _productStore.ProductDeleted -= OnProductDeleted;

            base.ViewDisappeared();
        }

        #region Data Loading and Filtering

        private async Task LoadProductsAsync()
        {
            List<ProductListItemViewModel>? productViewModels = null;
            string? filterTitle = null;
            string? filterSubtitle = null;

            // Load data on background thread
            await Task.Run(() =>
            {
                switch (_filter?.FilterType)
                {
                    case ProductFilterType.Category:
                        List<ProductListDto> categoryProducts = _productCacheReadService
                            .GetProductListByCategoryFromCache(_filter.EntityId);
                        productViewModels = categoryProducts
                                            .Select(dto => new ProductListItemViewModel(dto))
                                            .ToList();

                        CategoryDto? category = _categoryCacheReadService
                            .GetCategoryByIdInCache(_filter.EntityId);
                        filterTitle = $"Products in {category?.Name ?? "Category"}";
                        filterSubtitle = category?.Description ?? "Showing all products";
                        _logger.LogInformation(
                            "Loaded {Count} products for category {CategoryId}",
                            productViewModels.Count,
                            _filter.EntityId);
                        break;


                    case ProductFilterType.Supplier:
                        List<ProductListDto> supplierProducts = _productCacheReadService
                            .GetProductListBySupplierFromCache(_filter.EntityId);
                        productViewModels = supplierProducts
                                            .Select(dto => new ProductListItemViewModel(dto))
                                            .ToList();

                        SupplierDto? supplier = _supplierCacheReadService
                            .GetSupplierByIdInCache(_filter.EntityId);
                        filterTitle = $"Products from {supplier?.Name ?? "Supplier"}";
                        filterSubtitle = "Showing all products from this supplier";
                        _logger.LogInformation(
                            "Loaded {Count} products for supplier {SupplierId}",
                            productViewModels.Count,
                            _filter.EntityId);
                        break;

                    default:
                        List<ProductListDto> allProducts = _productCacheReadService
                            .GetProductListFromCache();

                        productViewModels = allProducts
                                            .Select(dto => new ProductListItemViewModel(dto))
                                            .ToList();
                        filterTitle = "All Products";
                        filterSubtitle = "Showing all products in the system";
                        _logger.LogInformation("Loaded {Count} total products", productViewModels.Count);
                        break;
                }

            });

            // Update UI properties on main thread
            await InvokeOnMainThreadAsync(() =>
            {
                if (productViewModels != null)
                {
                    _allProducts = productViewModels;
                    FilterTitle = filterTitle ?? string.Empty;
                    FilterSubtitle = filterSubtitle ?? string.Empty;

                    IsFiltered = _filter?.FilterType is ProductFilterType.Category or ProductFilterType.Supplier;

                    ApplyFilter();

                    _logger.LogInformation("Loaded {Count} products", _allProducts.Count);
                }
                else
                {
                    Products = [];
                    IsFiltered = false;
                }
            });
        }


        private async Task LoadProducts()
        {
            await Task.Run(() =>
            {
                List<ProductListDto> result = _productCacheReadService.GetProductListFromCache();

                if (result.Count == 0)
                {
                    _logger.LogInformation("No products found in store.");
                    Products = [];
                    _allProducts.Clear();
                    return;
                }

                _allProducts = result
                               .Select(dto => new ProductListItemViewModel(dto))
                               .ToList();

                ApplyFilter();
                _logger.LogInformation("Loaded {Count} products from store.", _allProducts.Count);
            });
        }

        private void ApplyFilter()
        {
            string filter = SearchText
                            ?.Trim()
                            .ToLowerInvariant() ?? string.Empty;

            List<ProductListItemViewModel> filtered = string.IsNullOrEmpty(filter)
                ? _allProducts
                : _allProducts
                  .Where(p =>
                             !string.IsNullOrEmpty(p.Name) &&
                             p.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                             !string.IsNullOrEmpty(p.CategoryName) &&
                             p.CategoryName.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                  .ToList();

            Products = new MvxObservableCollection<ProductListItemViewModel>(filtered);
        }

        #endregion

        #region Store Event Handlers

        private void OnProductAdded( Product product )
        {
            try
            {
                int totalStock = _inventoryManager.GetCurrentStockForProduct(product.ProductId);

                ProductListDto dto = product.ToListDto(
                    _productCacheReadService.GetCategoryNameFromCache(product.CategoryId),
                    _productCacheReadService.GetSupplierNameFromCache(product.SupplierId),
                    totalStock
                );

                ProductListItemViewModel vm = new(dto);
                _allProducts.Add(vm);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to ProductAdded for {ProductId}", product.ProductId);
            }
        }

        private void OnProductUpdated( Product product )
        {
            try
            {
                ProductListItemViewModel? existing = _allProducts.FirstOrDefault(p => p.ProductId == product.ProductId);
                if (existing == null)
                    return;
                int totalStock = _inventoryManager.GetCurrentStockForProduct(product.ProductId);

                ProductListDto dto = product.ToListDto(
                    _productCacheReadService.GetCategoryNameFromCache(product.CategoryId),
                    _productCacheReadService.GetSupplierNameFromCache(product.SupplierId),
                    totalStock
                );

                ProductListItemViewModel updatedVm = new(dto);

                int index = _allProducts.IndexOf(existing);
                _allProducts[index] = updatedVm;

                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to ProductUpdated for {ProductId}", product.ProductId);
            }
        }

        private void OnProductStockChanged( int productId, int newTotalStock )
        {
            _logger.LogInformation(
                "🔄 Stock update notification - Product: {ProductId}, New Stock: {Stock}",
                productId,
                newTotalStock);

            // Find the product in the list and update its stock
            ProductListItemViewModel? existing = _allProducts.FirstOrDefault(p => p.ProductId == productId);
            if (existing != null)
            {
                InvokeOnMainThread(() =>
                {
                    existing.CurrentStock = newTotalStock;

                    _logger.LogInformation(
                        "✅ UI Updated - Product '{Name}' now shows {Stock} units",
                        existing.Name,
                        newTotalStock);
                });
            }
        }


        private void OnProductDeleted( int productId )
        {
            try
            {
                _allProducts.RemoveAll(p => p.ProductId == productId);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to ProductDeleted for {ProductId}", productId);
            }
        }

        #endregion

        #region Properties

        public MvxObservableCollection<ProductListItemViewModel> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Title describing the filter (e.g., "Products in Electronics")
        /// </summary>
        public string FilterTitle
        {
            get => _filterTitle;
            private set => SetProperty(ref _filterTitle, value);
        }

        /// <summary>
        /// Subtitle with additional filter information
        /// </summary>
        public string FilterSubtitle
        {
            get => _filterSubtitle;
            private set => SetProperty(ref _filterSubtitle, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Indicates whether a filter is currently applied (Category or Supplier)
        /// Used to show/hide the filter header in the UI
        /// </summary>
        public bool IsFiltered
        {
            get => _isFiltered;
            set => SetProperty(ref _isFiltered, value);
        }

        public IEnumerable<ProductListItemViewModel> SelectedProducts => Products?.Where(p => p.IsSelected) ?? [];

        #endregion

        #region Commands

        private void ExecuteProductForm( int id ) => _modalNavigationControl.PopUp<ProductFormViewModel>(id);

        private void ExecuteProductDetails( int id ) => _modalNavigationControl.PopUp<ProductDetailsViewModel>(id);

        private void ExecuteOpenProductDelete( int productId ) => _modalNavigationControl.PopUp<ProductDeleteViewModel>(productId);

        private void ExecuteCloseCommand()
        {
            _logger.LogInformation("Closing filtered product list view");
            _modalNavigationControl.Close();
        }

        public IMvxCommand<int> OpenProductFormCommand { get; }
        public IMvxCommand<int> OpenProductDetailsCommand { get; }
        public IMvxCommand<int> OpenProductDeleteCommand { get; }

        public IMvxCommand CloseCommand { get; }


        public IMvxCommand SelectAllCommand => new MvxCommand(SelectAll);
        public IMvxCommand DeselectAllCommand => new MvxCommand(DeselectAll);
        public IMvxCommand DeleteSelectedCommand => new MvxCommand(async () => await DeleteSelected());

        private void SelectAll()
        {
            if (Products == null) return;
            foreach (ProductListItemViewModel? product in Products)
                product.IsSelected = true;
        }

        private void DeselectAll()
        {
            if (Products == null) return;
            foreach (ProductListItemViewModel? product in Products)
                product.IsSelected = false;
        }

        private async Task DeleteSelected()
        {
            List<ProductListItemViewModel> selected = SelectedProducts.ToList();
            if (selected.Count == 0) return;

            foreach (ProductListItemViewModel item in selected)
            {
                await _productService.SoftDeleteProductAsync(item.ProductId);
            }

            await LoadProductsAsync();
        }

        #endregion
    }
}
