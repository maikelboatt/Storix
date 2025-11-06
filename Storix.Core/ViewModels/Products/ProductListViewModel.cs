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
        private readonly IProductStore _productStore;
        private readonly ISupplierService _supplierService;
        private readonly ICategoryService _categoryService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<ProductListViewModel> _logger;
        private MvxObservableCollection<ProductListItemViewModel> _products;
        private string _searchText;
        private bool _isLoading;
        private bool _isCacheInitialized;
        private List<ProductListItemViewModel> _allProducts; // full cache


        public ProductListViewModel( IProductService productService,
            IProductStore productStore,
            ISupplierService supplierService,
            ICategoryService categoryService,
            IModalNavigationControl modalNavigationControl,
            ILogger<ProductListViewModel> logger )
        {
            _productService = productService;
            _productStore = productStore;
            _supplierService = supplierService;
            _categoryService = categoryService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

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
            // DatabaseResult<IEnumerable<ProductListDto>> result = await _productService.GetAllActiveProductsForListAsync();

            // if (result is { IsSuccess: true, Value: not null })
            //     Products = new MvxObservableCollection<ProductListItemViewModel>(
            //         result.Value.Select(dto => new ProductListItemViewModel(dto))
            //     );

            List<ProductListDto> result = _productStore.GetProductListDto();

            if (result.Count != 0)
            {
                _allProducts = result
                               .Select(dto => new ProductListItemViewModel(dto))
                               .ToList();
                ApplyFilter();
            }
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
                             !string.IsNullOrEmpty(p.Name) && p
                                                              .Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
                             !string.IsNullOrEmpty(p.CategoryName) && p
                                                                      .CategoryName.Contains(filter, StringComparison.InvariantCultureIgnoreCase)
                  )
                  .ToList();

            Products = new MvxObservableCollection<ProductListItemViewModel>(filtered);
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
                    ApplyFilter();
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
