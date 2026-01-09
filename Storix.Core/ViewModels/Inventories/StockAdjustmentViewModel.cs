using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Products;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services.Inventories.Interfaces;
using Storix.Application.Services.Print;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Stores.Locations;
using Storix.Core.Control;

namespace Storix.Core.ViewModels.Inventories
{
    /// <summary>
    /// ViewModel for adjusting stock levels for a product at a specific location.
    /// Supports both increase and decrease operations with reason tracking.
    /// </summary>
    public class StockAdjustmentViewModel:MvxViewModel<int>
    {
        private readonly IProductCacheReadService _productCacheReadService;
        private readonly IInventoryCacheReadService _inventoryCacheReadService;
        private readonly IInventoryManager _inventoryManager;
        private readonly IPrintService _printService;
        private readonly ILocationStore _locationStore;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<StockAdjustmentViewModel> _logger;

        private int _productId;
        private ProductDto? _product;
        private bool _isLoading;
        private ObservableCollection<LocationDto> _availableLocations;
        private LocationDto? _selectedLocation;
        private bool _isIncrease = true;
        private int _adjustmentQuantity = 1;
        private string _adjustmentReason = string.Empty;
        private string? _validationError;

        #region Constructor

        public StockAdjustmentViewModel(
            IProductCacheReadService productCacheReadService,
            IInventoryCacheReadService inventoryCacheReadService,
            IInventoryManager inventoryManager,
            IPrintService printService,
            ILocationStore locationStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<StockAdjustmentViewModel> logger )
        {
            _productCacheReadService = productCacheReadService ?? throw new ArgumentNullException(nameof(productCacheReadService));
            _inventoryCacheReadService = inventoryCacheReadService ?? throw new ArgumentNullException(nameof(inventoryCacheReadService));
            _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _locationStore = locationStore;
            _modalNavigationControl = modalNavigationControl ?? throw new ArgumentNullException(nameof(modalNavigationControl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _availableLocations = new ObservableCollection<LocationDto>();

            // Initialize commands
            SaveAdjustmentCommand = new MvxAsyncCommand(ExecuteSaveAdjustmentAsync, () => CanSaveAdjustment);
            TransferStockCommand = new MvxCommand(ExecuteTransferStock, () => CanSaveAdjustment);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
            IncreaseQuantityCommand = new MvxCommand(ExecuteIncreaseQuantity);
            DecreaseQuantityCommand = new MvxCommand(ExecuteDecreaseQuantity);
        }

        #endregion

        #region Commands

        public IMvxAsyncCommand SaveAdjustmentCommand { get; }
        public IMvxCommand TransferStockCommand { get; }
        public IMvxCommand CancelCommand { get; }
        public IMvxCommand IncreaseQuantityCommand { get; }
        public IMvxCommand DecreaseQuantityCommand { get; }

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
                await LoadProductAndLocationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize stock adjustment for product {ProductId}", _productId);
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
        /// Indicates whether data is being loaded or saved
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(
                ref _isLoading,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanSaveAdjustment);
                    RaisePropertyChanged(() => CanCancel);
                    SaveAdjustmentCommand.RaiseCanExecuteChanged();
                    CancelCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Product name for display
        /// </summary>
        public string ProductName => _product?.Name ?? "Unknown Product";

        /// <summary>
        /// Product SKU for display
        /// </summary>
        public string ProductSKU => _product?.SKU ?? "N/A";

        /// <summary>
        /// Available locations where stock can be adjusted
        /// </summary>
        public ObservableCollection<LocationDto> AvailableLocations
        {
            get => _availableLocations;
            private set => SetProperty(ref _availableLocations, value);
        }

        /// <summary>
        /// Currently selected location
        /// </summary>
        public LocationDto? SelectedLocation
        {
            get => _selectedLocation;
            set => SetProperty(
                ref _selectedLocation,
                value,
                () =>
                {
                    LoadCurrentStockForLocation();
                    ValidateAdjustment();
                    RaisePropertyChanged(() => CanSaveAdjustment);
                    SaveAdjustmentCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Whether the adjustment is an increase (true) or decrease (false)
        /// </summary>
        public bool IsIncrease
        {
            get => _isIncrease;
            set => SetProperty(
                ref _isIncrease,
                value,
                () =>
                {
                    RaisePropertyChanged(() => IsDecrease);
                    RaisePropertyChanged(() => NewStockLevel);
                    ValidateAdjustment();
                });
        }

        /// <summary>
        /// Whether the adjustment is a decrease
        /// </summary>
        public bool IsDecrease
        {
            get => !_isIncrease;
            set => IsIncrease = !value;
        }

        /// <summary>
        /// Quantity to adjust
        /// </summary>
        public int AdjustmentQuantity
        {
            get => _adjustmentQuantity;
            set => SetProperty(
                ref _adjustmentQuantity,
                value,
                () =>
                {
                    if (_adjustmentQuantity < 0)
                        _adjustmentQuantity = 0;

                    RaisePropertyChanged(() => NewStockLevel);
                    ValidateAdjustment();
                    RaisePropertyChanged(() => CanSaveAdjustment);
                    SaveAdjustmentCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Reason for the adjustment
        /// </summary>
        public string AdjustmentReason
        {
            get => _adjustmentReason;
            set => SetProperty(ref _adjustmentReason, value);
        }

        /// <summary>
        /// Validation error message
        /// </summary>
        public string? ValidationError
        {
            get => _validationError;
            private set => SetProperty(
                ref _validationError,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanSaveAdjustment);
                    SaveAdjustmentCommand.RaiseCanExecuteChanged();
                });
        }

        #endregion

        #region Stock Display Properties

        private int _currentStock;
        private int _availableStock;
        private int _reservedStock;

        /// <summary>
        /// Current stock at selected location
        /// </summary>
        public int CurrentStock
        {
            get => _currentStock;
            private set => SetProperty(ref _currentStock, value, () => { RaisePropertyChanged(() => NewStockLevel); });
        }

        /// <summary>
        /// Available stock at selected location
        /// </summary>
        public int AvailableStock
        {
            get => _availableStock;
            private set => SetProperty(ref _availableStock, value);
        }

        /// <summary>
        /// Reserved stock at selected location
        /// </summary>
        public int ReservedStock
        {
            get => _reservedStock;
            private set => SetProperty(ref _reservedStock, value);
        }

        /// <summary>
        /// Calculated new stock level after adjustment
        /// </summary>
        public int NewStockLevel
        {
            get
            {
                if (IsIncrease)
                    return CurrentStock + AdjustmentQuantity;
                else
                    return CurrentStock - AdjustmentQuantity;
            }
        }

        #endregion

        #region Command Can Execute Properties

        public bool CanSaveAdjustment => !IsLoading &&
                                         SelectedLocation != null &&
                                         AdjustmentQuantity > 0 &&
                                         string.IsNullOrEmpty(ValidationError);

        public bool CanCancel => !IsLoading;

        #endregion

        #region Methods

        /// <summary>
        /// Loads product information and available locations
        /// </summary>
        private async Task LoadProductAndLocationsAsync()
        {
            _logger.LogInformation("📦 Loading product and locations for adjustment (Product ID: {ProductId})", _productId);

            // Load product
            _product = _productCacheReadService.GetProductByIdFromCache(_productId);

            if (_product == null)
            {
                _logger.LogWarning("⚠️ Product {ProductId} not found in cache", _productId);
                return;
            }

            RaisePropertyChanged(() => ProductName);
            RaisePropertyChanged(() => ProductSKU);

            // Load locations where this product has inventory
            LoadAvailableLocations();

            _logger.LogInformation(
                "✅ Loaded product: {ProductName} with {LocationCount} locations",
                ProductName,
                AvailableLocations.Count);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads locations where this product has inventory records
        /// </summary>
        private void LoadAvailableLocations()
        {
            try
            {
                List<Domain.Models.Inventory> inventoryRecords = _inventoryCacheReadService.GetInventoryByProductIdInCache(_productId);

                List<LocationDto> locations = inventoryRecords
                                              .Select(inv => new LocationDto
                                              {
                                                  LocationId = inv.LocationId,
                                                  LocationName = GetLocationName(inv.LocationId),
                                                  InventoryId = inv.InventoryId
                                              })
                                              .OrderBy(l => l.LocationName)
                                              .ToList();

                AvailableLocations = new ObservableCollection<LocationDto>(locations);

                // Auto-select first location if available
                if (AvailableLocations.Any())
                {
                    SelectedLocation = AvailableLocations.First();
                }

                _logger.LogDebug("Loaded {Count} locations for product {ProductId}", locations.Count, _productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load locations for product {ProductId}", _productId);
                AvailableLocations = new ObservableCollection<LocationDto>();
            }
        }

        /// <summary>
        /// Loads current stock levels for the selected location
        /// </summary>
        private void LoadCurrentStockForLocation()
        {
            if (SelectedLocation == null)
            {
                CurrentStock = 0;
                AvailableStock = 0;
                ReservedStock = 0;
                return;
            }

            try
            {
                Domain.Models.Inventory? inventory = _inventoryCacheReadService.GetInventoryByProductAndLocationInCache(
                    _productId,
                    SelectedLocation.LocationId);

                if (inventory != null)
                {
                    CurrentStock = inventory.CurrentStock;
                    AvailableStock = inventory.AvailableStock;
                    ReservedStock = inventory.ReservedStock;

                    _logger.LogDebug(
                        "Loaded stock for location {LocationId} - Current: {Current}, Available: {Available}, Reserved: {Reserved}",
                        SelectedLocation.LocationId,
                        CurrentStock,
                        AvailableStock,
                        ReservedStock);
                }
                else
                {
                    _logger.LogWarning(
                        "No inventory found for product {ProductId} at location {LocationId}",
                        _productId,
                        SelectedLocation.LocationId);
                    CurrentStock = 0;
                    AvailableStock = 0;
                    ReservedStock = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load stock for location {LocationId}", SelectedLocation.LocationId);
                CurrentStock = 0;
                AvailableStock = 0;
                ReservedStock = 0;
            }
        }

        /// <summary>
        /// Validates the adjustment before saving
        /// </summary>
        private void ValidateAdjustment()
        {
            ValidationError = null;

            if (SelectedLocation == null)
            {
                ValidationError = "Please select a location";
                return;
            }

            if (AdjustmentQuantity <= 0)
            {
                ValidationError = "Quantity must be greater than zero";
                return;
            }

            if (IsDecrease && AdjustmentQuantity > CurrentStock)
            {
                ValidationError = $"Cannot decrease by {AdjustmentQuantity}. Current stock is only {CurrentStock}.";
                return;
            }

            if (IsDecrease && AdjustmentQuantity > AvailableStock)
            {
                ValidationError = $"Cannot decrease by {AdjustmentQuantity}. Only {AvailableStock} units are available (some are reserved).";
                return;
            }
        }

        /// <summary>
        /// Gets location name by ID
        /// </summary>
        private string GetLocationName( int locationId ) => _locationStore.GetLocationName(locationId) ??
                                                            $"Location {locationId}";

        #endregion

        #region Command Implementations

        /// <summary>
        /// Increases the adjustment quantity by 1
        /// </summary>
        private void ExecuteIncreaseQuantity()
        {
            AdjustmentQuantity++;
        }

        /// <summary>
        /// Decreases the adjustment quantity by 1
        /// </summary>
        private void ExecuteDecreaseQuantity()
        {
            if (AdjustmentQuantity > 0)
            {
                AdjustmentQuantity--;
            }
        }

        /// <summary>
        /// Opens the Transfer Stock modal
        /// </summary>
        private void ExecuteTransferStock() => _modalNavigationControl.PopUp<StockTransferViewModel>(parameter: _productId);

        /// <summary>
        /// Saves the stock adjustment
        /// </summary>
        private async Task ExecuteSaveAdjustmentAsync()
        {
            if (SelectedLocation == null || _product == null)
            {
                _logger.LogWarning("⚠️ Cannot save adjustment: Location or Product is null");
                return;
            }

            ValidateAdjustment();

            if (!string.IsNullOrEmpty(ValidationError))
            {
                _logger.LogWarning("⚠️ Validation failed: {Error}", ValidationError);
                return;
            }

            IsLoading = true;

            try
            {
                int quantityChange = IsIncrease
                    ? AdjustmentQuantity
                    : -AdjustmentQuantity;

                _logger.LogInformation(
                    "💾 Adjusting stock for Product: {ProductId} at Location: {LocationId} by {Quantity}",
                    _productId,
                    SelectedLocation.LocationId,
                    quantityChange);

                // Get inventory ID for this product-location combination
                Domain.Models.Inventory? inventory = _inventoryCacheReadService.GetInventoryByProductAndLocationInCache(
                    _productId,
                    SelectedLocation.LocationId);

                if (inventory == null)
                {
                    ValidationError = "Inventory record not found for this product and location.";
                    _logger.LogError(
                        "Inventory not found for Product {ProductId} at Location {LocationId}",
                        _productId,
                        SelectedLocation.LocationId);
                    return;
                }

                // Call inventory service to perform adjustment
                DatabaseResult result = await _inventoryManager.AdjustStockAsync(
                    inventory.InventoryId,
                    quantityChange,
                    AdjustmentReason,
                    GetCurrentUserId());

                if (!result.IsSuccess)
                {
                    ValidationError = result.ErrorMessage ?? "Failed to adjust stock.";
                    _logger.LogError("Stock adjustment failed: {Error}", result.ErrorMessage);
                    return;
                }

                _logger.LogInformation(
                    "✅ Stock adjustment successful - New stock: {NewStock}",
                    NewStockLevel);

                // Optional: Print receipt
                MessageBoxResult printResult = MessageBox.Show(
                    $"Stock adjusted successfully from {CurrentStock} to {NewStockLevel}.\n\nWould you like to print a receipt?",
                    "Success - Print Receipt?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (printResult == MessageBoxResult.Yes)
                {
                    _printService.PrintStockAdjustmentReceipt(
                        _productId,
                        ProductName,
                        ProductSKU,
                        SelectedLocation.LocationName ?? "Unknown",
                        CurrentStock,
                        NewStockLevel,
                        quantityChange,
                        AdjustmentReason ?? "No reason provided"
                    );
                }

                // Close the modal after successful adjustment
                _modalNavigationControl.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Failed to adjust stock for Product: {ProductId} at Location: {LocationId}",
                    _productId,
                    SelectedLocation.LocationId);

                ValidationError = "Failed to save adjustment. Please try again.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Gets the current user ID (placeholder - implement based on your auth system)
        /// </summary>
        private int GetCurrentUserId() =>
            // TODO: Get from authentication service or user context
            1; // Placeholder

        /// <summary>
        /// Cancels the adjustment and closes the modal
        /// </summary>
        private void ExecuteCancelCommand()
        {
            _logger.LogInformation("❌ Stock adjustment cancelled by user");
            _modalNavigationControl.Close();
        }

        #endregion
    }

    #region Supporting DTOs

    /// <summary>
    /// DTO for location information
    /// </summary>
    public class LocationDto:MvxNotifyPropertyChanged
    {
        private int _locationId;
        private string? _locationName;
        private int _inventoryId;

        public int LocationId
        {
            get => _locationId;
            set => SetProperty(ref _locationId, value);
        }

        public string? LocationName
        {
            get => _locationName;
            set => SetProperty(ref _locationName, value);
        }

        public int InventoryId
        {
            get => _inventoryId;
            set => SetProperty(ref _inventoryId, value);
        }
    }

    #endregion
}
