using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Products;
using Storix.Application.Services.Inventories.Interfaces;
using Storix.Application.Services.Print;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Stores.Categories;
using Storix.Application.Stores.Locations;
using Storix.Application.Stores.Suppliers;
using Storix.Core.Control;
using Storix.Core.ViewModels.Inventories;
using Storix.Domain.Models;

namespace Storix.Core.ViewModels.Products
{
    /// <summary>
    /// ViewModel for displaying comprehensive product details.
    /// Shows product information, stock levels, location breakdown, and related data.
    /// </summary>
    public class ProductDetailsViewModel:MvxViewModel<int>, IProductViewModel
    {
        private readonly IProductCacheReadService _productCacheReadService;
        private readonly IInventoryCacheReadService _inventoryCacheReadService;
        private readonly IPrintService _printService;
        private readonly ICategoryStore _categoryStore;
        private readonly ISupplierStore _supplierStore;
        private readonly ILocationStore _locationStore;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<ProductDetailsViewModel> _logger;

        private ProductDto? _product;
        private bool _isLoading;
        private int _productId;
        private ObservableCollection<StockLocationDto> _stockByLocation;

        #region Constructor

        public ProductDetailsViewModel(
            IProductCacheReadService productCacheReadService,
            IInventoryCacheReadService inventoryCacheReadService,
            IPrintService printService,
            ICategoryStore categoryStore,
            ISupplierStore supplierStore,
            ILocationStore locationStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<ProductDetailsViewModel> logger )
        {
            _productCacheReadService = productCacheReadService ?? throw new ArgumentNullException(nameof(productCacheReadService));
            _inventoryCacheReadService = inventoryCacheReadService ?? throw new ArgumentNullException(nameof(inventoryCacheReadService));
            _printService = printService;
            _categoryStore = categoryStore ?? throw new ArgumentNullException(nameof(categoryStore));
            _supplierStore = supplierStore ?? throw new ArgumentNullException(nameof(supplierStore));
            _locationStore = locationStore;
            _modalNavigationControl = modalNavigationControl ?? throw new ArgumentNullException(nameof(modalNavigationControl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _stockByLocation = [];

            // Initialize commands
            CloseCommand = new MvxCommand(ExecuteCloseCommand);
            EditProductCommand = new MvxCommand(ExecuteEditProductCommand, () => CanEditProduct);
            AdjustStockCommand = new MvxCommand(ExecuteAdjustStockCommand, () => CanAdjustStock);
            PrintDetailsCommand = new MvxCommand(ExecutePrintDetailsCommand, () => CanPrintDetails);
        }

        #endregion

        #region Commands

        public IMvxCommand CloseCommand { get; }
        public IMvxCommand EditProductCommand { get; }
        public IMvxCommand AdjustStockCommand { get; }
        public IMvxCommand PrintDetailsCommand { get; }

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
                await LoadProductDetailsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load product details for ID: {ProductId}", _productId);
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
        /// The product being displayed
        /// </summary>
        public ProductDto? Product
        {
            get => _product;
            private set => SetProperty(
                ref _product,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanEditProduct);
                    RaisePropertyChanged(() => CanAdjustStock);
                    RaisePropertyChanged(() => CanPrintDetails);
                    RaisePropertyChanged(() => ProfitMargin);
                    RaisePropertyChanged(() => TotalStockValue);
                    EditProductCommand.RaiseCanExecuteChanged();
                    AdjustStockCommand.RaiseCanExecuteChanged();
                    PrintDetailsCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Indicates whether data is being loaded
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Stock breakdown by location
        /// </summary>
        public ObservableCollection<StockLocationDto> StockByLocation
        {
            get => _stockByLocation;
            private set => SetProperty(ref _stockByLocation, value, () => { RaisePropertyChanged(() => HasStockLocations); });
        }

        /// <summary>
        /// Whether product has stock at any location
        /// </summary>
        public bool HasStockLocations => StockByLocation?.Any() ?? false;

        #endregion

        #region Stock Properties

        private int _totalStock;
        private int _availableStock;
        private int _reservedStock;

        /// <summary>
        /// Total stock across all locations
        /// </summary>
        public int TotalStock
        {
            get => _totalStock;
            private set => SetProperty(
                ref _totalStock,
                value,
                () =>
                {
                    RaisePropertyChanged(() => IsInStock);
                    RaisePropertyChanged(() => IsLowStock);
                    RaisePropertyChanged(() => TotalStockValue);
                });
        }

        /// <summary>
        /// Available stock across all locations
        /// </summary>
        public int AvailableStock
        {
            get => _availableStock;
            private set => SetProperty(ref _availableStock, value);
        }

        /// <summary>
        /// Reserved stock across all locations
        /// </summary>
        public int ReservedStock
        {
            get => _reservedStock;
            private set => SetProperty(ref _reservedStock, value);
        }

        /// <summary>
        /// Whether product is in stock
        /// </summary>
        public bool IsInStock => TotalStock > 0;

        /// <summary>
        /// Whether product stock is below minimum threshold
        /// </summary>
        public bool IsLowStock => Product != null && TotalStock > 0 && TotalStock <= Product.MinStockLevel;

        #endregion

        #region Financial Properties

        /// <summary>
        /// Profit margin per unit (Price - Cost)
        /// </summary>
        public decimal ProfitMargin => Product != null
            ? Product.Price - Product.Cost
            : 0m;

        /// <summary>
        /// Total inventory value (TotalStock * Cost)
        /// </summary>
        public decimal TotalStockValue => Product != null
            ? TotalStock * Product.Cost
            : 0m;

        #endregion

        #region Related Data Properties

        private string? _categoryName;
        private string? _supplierName;

        /// <summary>
        /// Name of the product's category
        /// </summary>
        public string? CategoryName
        {
            get => _categoryName;
            private set => SetProperty(ref _categoryName, value);
        }

        /// <summary>
        /// Name of the product's supplier
        /// </summary>
        public string? SupplierName
        {
            get => _supplierName;
            private set => SetProperty(ref _supplierName, value);
        }

        #endregion

        #region Command Can Execute Properties

        public bool CanEditProduct => Product != null && !IsLoading;
        public bool CanAdjustStock => Product != null && !IsLoading;
        public bool CanPrintDetails => Product != null && !IsLoading;

        #endregion

        #region Methods

        /// <summary>
        /// Loads complete product details including stock and related data
        /// </summary>
        private async Task LoadProductDetailsAsync()
        {
            _logger.LogInformation("📦 Loading product details for ID: {ProductId}", _productId);

            // Load product from cache
            ProductDto? product = _productCacheReadService.GetProductByIdFromCache(_productId);

            if (product == null)
            {
                _logger.LogWarning("⚠️ Product with ID {ProductId} not found in cache", _productId);
                return;
            }

            Product = product;

            // Load related data
            LoadCategoryAndSupplier();

            // Load stock information
            LoadStockInformation();

            // Load stock by location
            LoadStockByLocation();

            _logger.LogInformation(
                "✅ Loaded product details: {ProductName} (SKU: {SKU})",
                Product.Name,
                Product.SKU);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads category and supplier names
        /// </summary>
        private void LoadCategoryAndSupplier()
        {
            if (Product == null) return;

            try
            {
                CategoryName = _categoryStore.GetCategoryName(Product.CategoryId) ?? "Unknown Category";
                SupplierName = _supplierStore.GetSupplierName(Product.SupplierId) ?? "Unknown Supplier";

                _logger.LogDebug(
                    "Loaded related data - Category: {Category}, Supplier: {Supplier}",
                    CategoryName,
                    SupplierName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load category/supplier names for product {ProductId}", _productId);
                CategoryName = "Unknown Category";
                SupplierName = "Unknown Supplier";
            }
        }

        /// <summary>
        /// Loads aggregated stock information across all locations
        /// </summary>
        private void LoadStockInformation()
        {
            if (Product == null) return;

            try
            {
                List<Inventory> inventoryRecords = _inventoryCacheReadService.GetInventoryByProductIdInCache(_productId);

                TotalStock = inventoryRecords.Sum(i => i.CurrentStock);
                AvailableStock = inventoryRecords.Sum(i => i.AvailableStock);
                ReservedStock = inventoryRecords.Sum(i => i.ReservedStock);

                _logger.LogDebug(
                    "Stock summary - Total: {Total}, Available: {Available}, Reserved: {Reserved}",
                    TotalStock,
                    AvailableStock,
                    ReservedStock);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load stock information for product {ProductId}", _productId);
                TotalStock = 0;
                AvailableStock = 0;
                ReservedStock = 0;
            }
        }

        /// <summary>
        /// Loads stock breakdown by location
        /// </summary>
        private void LoadStockByLocation()
        {
            if (Product == null) return;

            try
            {
                List<Inventory> inventoryRecords = _inventoryCacheReadService.GetInventoryByProductIdInCache(_productId);

                List<StockLocationDto> locationStockList = inventoryRecords
                                                           .Select(inv => new StockLocationDto
                                                           {
                                                               LocationId = inv.LocationId,
                                                               LocationName = GetLocationName(inv.LocationId),
                                                               CurrentStock = inv.CurrentStock,
                                                               AvailableStock = inv.AvailableStock,
                                                               ReservedStock = inv.ReservedStock,
                                                               IsLowStock = Product != null && inv.CurrentStock > 0 && inv.CurrentStock <= Product.MinStockLevel
                                                           })
                                                           .OrderByDescending(l => l.CurrentStock)
                                                           .ToList();

                StockByLocation = new ObservableCollection<StockLocationDto>(locationStockList);

                _logger.LogDebug("Loaded stock for {Count} locations", StockByLocation.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load stock by location for product {ProductId}", _productId);
                StockByLocation = new ObservableCollection<StockLocationDto>();
            }
        }

        /// <summary>
        /// Gets location name by ID
        /// </summary>
        private string? GetLocationName( int locationId ) => _locationStore.GetLocationName(locationId);

        #endregion

        #region Command Implementations

        /// <summary>
        /// Closes the product details dialogue
        /// </summary>
        private void ExecuteCloseCommand()
        {
            _logger.LogInformation("Closing product details view");
            _modalNavigationControl.Close();
        }

        /// <summary>
        /// Opens the product edit dialogue
        /// </summary>
        private void ExecuteEditProductCommand()
        {
            if (Product == null) return;

            _logger.LogInformation("Opening edit dialog for product {ProductId}", Product.ProductId);

            _modalNavigationControl.PopUp<ProductFormViewModel>(_productId);
        }

        /// <summary>
        /// Opens the stock adjustment dialogue
        /// </summary>
        private void ExecuteAdjustStockCommand()
        {
            if (Product == null) return;

            _logger.LogInformation("Opening stock adjustment for product {ProductId}", Product.ProductId);

            _modalNavigationControl.PopUp<StockAdjustmentViewModel>(_productId);
        }

        /// <summary>
        /// Prints product details
        /// </summary>
        private void ExecutePrintDetailsCommand()
        {
            if (Product == null) return;

            _logger.LogInformation("Printing details for product {ProductId}", Product.ProductId);

            _printService.PrintProductDetails(
                Product,
                StockByLocation.ToList(),
                _categoryName!,
                _supplierName!,
                TotalStock,
                AvailableStock,
                ReservedStock);
        }

        #endregion
    }
}
