using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.Products;
using Storix.Application.DTO.Suppliers;
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
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly IProductStore _productStore;
        private readonly ILogger<ProductListViewModel> _logger;
        private MvxObservableCollection<ProductListItemViewModel> _products;
        private string _searchText;
        private bool _isLoading;
        private bool _isCacheInitialized;
        private List<ProductListItemViewModel> _allProducts; // full cache


        public ProductListViewModel( IProductService productService,
            IModalNavigationControl modalNavigationControl,
            IProductStore productStore,
            ILogger<ProductListViewModel> logger )
        {
            _productService = productService;
            _modalNavigationControl = modalNavigationControl;
            _productStore = productStore;
            _logger = logger;

            // Subscribe to events
            _productStore.ProductAdded += ProductStoreOnProductAdded;
            _productStore.ProductUpdated += ProductStoreOnProductUpdated;
            _productStore.ProductDeleted += ProductStoreOnProductDeleted;

            // Initialize commands
            OpenProductFormCommand = new MvxCommand<int>(ExecuteProductForm);
            OpenCreateSalesOrderCommand = new MvxCommand(ExecuteCreateSalesOrder);
            OpenCreatePurchaseOrderCommand = new MvxCommand(ExecuteCreatePurchaseOrder);
        }

        #region Event-Handlers

        private void ProductStoreOnProductAdded( Product product )
        {
            try
            {
                // Convert the added Product to ProductListDto
                ProductListDto dto = product.ToListDto(
                    product.CategoryId > 0
                        ? _productStore.GetCategoryName(product.CategoryId)
                        : null,
                    product.SupplierId > 0
                        ? _productStore.GetSupplierName(product.SupplierId)
                        : null,
                    3
                    // _productStore.GetCurrentStock(product.ProductId)
                );

                // Create view model and add to lists
                ProductListItemViewModel vm = new(dto);

                _allProducts.Add(vm);

                // Apply filter so it shows up if it matches current search
                ApplyFilterOptimized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ProductAdded event for product {ProductId}", product.ProductId);
            }
        }

        private void ProductStoreOnProductUpdated( Product product )
        {
            try
            {
                // Find existing item
                ProductListItemViewModel? existing = _allProducts.FirstOrDefault(p => p.ProductId == product.ProductId);
                if (existing is null)
                    return;

                // Replace with updated data
                ProductListDto dto = product.ToListDto(
                    product.CategoryId > 0
                        ? _productStore.GetCategoryName(product.CategoryId)
                        : null,
                    product.SupplierId > 0
                        ? _productStore.GetSupplierName(product.SupplierId)
                        : null,
                    5
                    // _productStore.GetCurrentStock(product.ProductId)
                );

                ProductListItemViewModel updatedVm = new(dto);

                // Update in the full list
                int index = _allProducts.IndexOf(existing);
                _allProducts[index] = updatedVm;

                // Refresh visible list
                ApplyFilterOptimized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ProductUpdated event for product {ProductId}", product.ProductId);
            }
        }

        private void ProductStoreOnProductDeleted( int productId )
        {
            try
            {
                // Remove from both caches
                _allProducts.RemoveAll(p => p.ProductId == productId);
                // Products.Remove();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ProductDeleted event for product {ProductId}", productId);
            }
        }

        #endregion


        #region ViewModel LifeCycle

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

        #endregion

        private void LoadProducts()
        {
            List<ProductListDto> result = _productStore.GetProductListDto();

            if (result == null || result.Count == 0)
            {
                Products = [];
                _logger.LogWarning("No products found from ProductStore.");
                return;
            }

            _allProducts = result
                           .Select(dto => new ProductListItemViewModel(dto))
                           .ToList();

            ApplyFilterOptimized();
        }

        private CancellationTokenSource _filterCancellation;
        private const int FilterDebounceMs = 300;


        private async void DebouncedFilter()
        {
            _filterCancellation?.Cancel();
            _filterCancellation = new CancellationTokenSource();

            try
            {
                await Task.Delay(FilterDebounceMs, _filterCancellation.Token);
                ApplyFilterOptimized();
            }
            catch (TaskCanceledException)
            {
                // Cancelled by newer input
            }
        }

        private void ApplyFilterOptimized()
        {
            string filter = SearchText?.Trim() ?? string.Empty;

            List<ProductListItemViewModel> filtered = string.IsNullOrEmpty(filter)
                ? _allProducts
                : _allProducts
                  .Where(p =>
                             !string.IsNullOrEmpty(p.Name) &&
                             p.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                             !string.IsNullOrEmpty(p.CategoryName) &&
                             p.CategoryName.Contains(filter, StringComparison.InvariantCultureIgnoreCase)
                  )
                  .ToList();

            // For small lists or first load, just replace
            if (Products.Count == 0)
            {
                Products = new MvxObservableCollection<ProductListItemViewModel>(filtered);
                return;
            }

            // Use HashSet for O(1) lookups
            HashSet<ProductListItemViewModel> filteredSet = new(filtered);

            // Remove items not in filtered list (backwards to avoid index issues)
            for (int i = Products.Count - 1; i >= 0; i--)
            {
                if (!filteredSet.Contains(Products[i]))
                {
                    Products.RemoveAt(i);
                }
            }

            // Add new items preserving order
            HashSet<ProductListItemViewModel> existingSet = new(Products);
            foreach (ProductListItemViewModel item in filtered.Where(f => !existingSet.Contains(f)))
            {
                Products.Add(item);
            }
        }


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
                    DebouncedFilter();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Get all currently selected products
        public IEnumerable<ProductListItemViewModel> SelectedProducts => Products?.Where(p => p.IsSelected) ?? [];

        #endregion

        #region Command Impelementations

        private void ExecuteProductForm( int id )
        {
            // Open the Product-Form dialog
            _modalNavigationControl.PopUp<ProductFormViewModel>(id);
        }

        private void ExecuteCreateSalesOrder()
        {
            _modalNavigationControl.PopUp<FormerViewModel>();
        }

        private void ExecuteCreatePurchaseOrder() => _modalNavigationControl.PopUp<ProductFormViewModel>(4);

        #endregion

        // Commands
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
            {
                product.IsSelected = true;
            }
        }

        private void DeselectAll()
        {
            if (Products == null) return;

            foreach (ProductListItemViewModel? product in Products)
            {
                product.IsSelected = false;
            }
        }

        private async Task DeleteSelected()
        {
            List<ProductListItemViewModel> selected = SelectedProducts.ToList();

            if (selected.Count == 0)
            {
                // Show message: "No products selected"
                return;
            }

            // Confirm deletion
            // ... your confirmation logic ...

            foreach (ProductListItemViewModel item in selected)
            {
                await _productService.SoftDeleteProductAsync(item.ProductId);
            }

            LoadProducts(); // Refresh list
        }
    }
}
