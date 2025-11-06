using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Products;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Services.Products;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Application.Stores.Products;
using Storix.Core.Control;
using Storix.Domain.Models;

namespace Storix.Core.ViewModels.Products
{
    public class ProductListViewModel:MvxViewModel
    {
        private readonly IProductService _productService;
        private readonly IProductStore _productStore;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<ProductListViewModel> _logger;

        private MvxObservableCollection<ProductListItemViewModel> _products = new();
        private List<ProductListItemViewModel> _allProducts = new();
        private string _searchText = string.Empty;
        private bool _isLoading;

        public ProductListViewModel(
            IProductService productService,
            IProductStore productStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<ProductListViewModel> logger )
        {
            _productService = productService;
            _productStore = productStore;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Subscribe to write operation events from the store
            _productStore.ProductAdded += OnProductAdded;
            _productStore.ProductUpdated += OnProductUpdated;
            _productStore.ProductDeleted += OnProductDeleted;

            // Initialize commands
            OpenProductFormCommand = new MvxCommand<int>(ExecuteProductForm);
            OpenCreateSalesOrderCommand = new MvxCommand(ExecuteCreateSalesOrder);
            OpenCreatePurchaseOrderCommand = new MvxCommand(ExecuteCreatePurchaseOrder);
        }

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                LoadProducts();
            }
            finally
            {
                IsLoading = false;
            }

            await base.Initialize();
        }

        private void LoadProducts()
        {
            List<ProductListDto> result = _productStore.GetProductListDto();

            if (result == null || result.Count == 0)
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
        }

        #region === Store Event Handlers ===

        private void OnProductAdded( Product product )
        {
            try
            {
                ProductListDto dto = product.ToListDto(
                    _productStore.GetCategoryName(product.CategoryId),
                    _productStore.GetSupplierName(product.SupplierId),
                    // product.AvailableStock // or CurrentStock, whichever you prefer
                    50 // Placeholder for AvailableStock
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

                ProductListDto dto = product.ToListDto(
                    _productStore.GetCategoryName(product.CategoryId),
                    _productStore.GetSupplierName(product.SupplierId),
                    // product.AvailableStock
                    7 // Placeholder for AvailableStock
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

        #region === Properties ===

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

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public IEnumerable<ProductListItemViewModel> SelectedProducts => Products?.Where(p => p.IsSelected) ?? [];

        #endregion

        #region === Commands ===

        private void ExecuteProductForm( int id )
        {
            _modalNavigationControl.PopUp<ProductFormViewModel>(id);
        }

        private void ExecuteCreateSalesOrder()
        {
            _modalNavigationControl.PopUp<FormerViewModel>();
        }

        private void ExecuteCreatePurchaseOrder() => _modalNavigationControl.PopUp<ProductFormViewModel>(4);

        public IMvxCommand<int> OpenProductFormCommand { get; }
        public IMvxCommand OpenCreateSalesOrderCommand { get; }
        public IMvxCommand OpenCreatePurchaseOrderCommand { get; }

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

            LoadProducts();
        }

        #endregion
    }
}
