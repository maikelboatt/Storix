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
using Storix.Core.Control;
using Storix.Domain.Models;

namespace Storix.Core.ViewModels.Inventories
{
    /// <summary>
    /// ViewModel for transferring stock between locations.
    /// </summary>
    public class StockTransferViewModel:MvxViewModel<int>
    {
        private readonly IProductCacheReadService _productCacheReadService;
        private readonly IInventoryCacheReadService _inventoryCacheReadService;
        private readonly IInventoryManager _inventoryManager;
        private readonly IPrintService _printService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<StockTransferViewModel> _logger;

        private int _productId;
        private ProductDto? _product;
        private bool _isLoading;
        private ObservableCollection<LocationDto> _availableFromLocations;
        private ObservableCollection<LocationDto> _availableToLocations;
        private LocationDto? _fromLocation;
        private LocationDto? _toLocation;
        private int _transferQuantity = 1;
        private string _transferNotes = string.Empty;
        private string? _validationError;

        #region Constructor

        public StockTransferViewModel(
            IProductCacheReadService productCacheReadService,
            IInventoryCacheReadService inventoryCacheReadService,
            IInventoryManager inventoryManager,
            IPrintService printService,
            IModalNavigationControl modalNavigationControl,
            ILogger<StockTransferViewModel> logger )
        {
            _productCacheReadService = productCacheReadService ?? throw new ArgumentNullException(nameof(productCacheReadService));
            _inventoryCacheReadService = inventoryCacheReadService ?? throw new ArgumentNullException(nameof(inventoryCacheReadService));
            _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _modalNavigationControl = modalNavigationControl ?? throw new ArgumentNullException(nameof(modalNavigationControl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _availableFromLocations = new ObservableCollection<LocationDto>();
            _availableToLocations = new ObservableCollection<LocationDto>();

            // Initialize commands
            SaveTransferCommand = new MvxAsyncCommand(ExecuteSaveTransferAsync, () => CanSaveTransfer);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
            IncreaseQuantityCommand = new MvxCommand(ExecuteIncreaseQuantity);
            DecreaseQuantityCommand = new MvxCommand(ExecuteDecreaseQuantity);
        }

        #endregion

        #region Commands

        public IMvxAsyncCommand SaveTransferCommand { get; }
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
                _logger.LogError(ex, "❌ Failed to initialize stock transfer for product {ProductId}", _productId);
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
                    RaisePropertyChanged(() => CanSaveTransfer);
                    RaisePropertyChanged(() => CanCancel);
                    SaveTransferCommand.RaiseCanExecuteChanged();
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
        /// Available locations to transfer FROM (locations with stock)
        /// </summary>
        public ObservableCollection<LocationDto> AvailableFromLocations
        {
            get => _availableFromLocations;
            private set => SetProperty(ref _availableFromLocations, value);
        }

        /// <summary>
        /// Available locations to transfer TO (all locations except FROM)
        /// </summary>
        public ObservableCollection<LocationDto> AvailableToLocations
        {
            get => _availableToLocations;
            private set => SetProperty(ref _availableToLocations, value);
        }

        /// <summary>
        /// Source location for transfer
        /// </summary>
        public LocationDto? FromLocation
        {
            get => _fromLocation;
            set => SetProperty(
                ref _fromLocation,
                value,
                () =>
                {
                    LoadFromLocationStock();
                    UpdateToLocations();
                    ValidateTransfer();
                    RaisePropertyChanged(() => CanSaveTransfer);
                    SaveTransferCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Destination location for transfer
        /// </summary>
        public LocationDto? ToLocation
        {
            get => _toLocation;
            set => SetProperty(
                ref _toLocation,
                value,
                () =>
                {
                    LoadToLocationStock();
                    ValidateTransfer();
                    RaisePropertyChanged(() => CanSaveTransfer);
                    SaveTransferCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Quantity to transfer
        /// </summary>
        public int TransferQuantity
        {
            get => _transferQuantity;
            set => SetProperty(
                ref _transferQuantity,
                value,
                () =>
                {
                    if (_transferQuantity < 0)
                        _transferQuantity = 0;

                    RaisePropertyChanged(() => ToLocationNewStock);
                    ValidateTransfer();
                    RaisePropertyChanged(() => CanSaveTransfer);
                    SaveTransferCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Transfer notes
        /// </summary>
        public string TransferNotes
        {
            get => _transferNotes;
            set => SetProperty(ref _transferNotes, value);
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
                    RaisePropertyChanged(() => CanSaveTransfer);
                    SaveTransferCommand.RaiseCanExecuteChanged();
                });
        }

        #endregion

        #region Stock Display Properties

        private int _fromLocationStock;
        private int _fromLocationAvailable;
        private int _toLocationStock;

        /// <summary>
        /// Current stock at FROM location
        /// </summary>
        public int FromLocationStock
        {
            get => _fromLocationStock;
            private set => SetProperty(ref _fromLocationStock, value);
        }

        /// <summary>
        /// Available stock at FROM location (not reserved)
        /// </summary>
        public int FromLocationAvailable
        {
            get => _fromLocationAvailable;
            private set => SetProperty(ref _fromLocationAvailable, value);
        }

        /// <summary>
        /// Current stock at TO location
        /// </summary>
        public int ToLocationStock
        {
            get => _toLocationStock;
            private set => SetProperty(ref _toLocationStock, value, () => { RaisePropertyChanged(() => ToLocationNewStock); });
        }

        /// <summary>
        /// Projected stock at TO location after transfer
        /// </summary>
        public int ToLocationNewStock => ToLocationStock + TransferQuantity;

        #endregion

        #region Command Can Execute Properties

        public bool CanSaveTransfer => !IsLoading &&
                                       FromLocation != null &&
                                       ToLocation != null &&
                                       TransferQuantity > 0 &&
                                       string.IsNullOrEmpty(ValidationError);

        public bool CanCancel => !IsLoading;

        #endregion

        #region Methods

        /// <summary>
        /// Loads product information and available locations
        /// </summary>
        private async Task LoadProductAndLocationsAsync()
        {
            _logger.LogInformation("📦 Loading product and locations for transfer (Product ID: {ProductId})", _productId);

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
            LoadFromLocations();

            _logger.LogInformation(
                "✅ Loaded product: {ProductName} with {LocationCount} locations",
                ProductName,
                AvailableFromLocations.Count);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads locations where this product has stock (FROM locations)
        /// </summary>
        private void LoadFromLocations()
        {
            try
            {
                List<Inventory> inventoryRecords = _inventoryCacheReadService.GetInventoryByProductIdInCache(_productId);

                // Only include locations with available stock
                List<LocationDto> locations = inventoryRecords
                                              .Where(inv => inv.AvailableStock > 0)
                                              .Select(inv => new LocationDto
                                              {
                                                  LocationId = inv.LocationId,
                                                  LocationName = GetLocationName(inv.LocationId),
                                                  InventoryId = inv.InventoryId
                                              })
                                              .OrderBy(l => l.LocationName)
                                              .ToList();

                AvailableFromLocations = new ObservableCollection<LocationDto>(locations);

                // Auto-select first location if available
                if (AvailableFromLocations.Any())
                {
                    FromLocation = AvailableFromLocations.First();
                }

                _logger.LogDebug("Loaded {Count} FROM locations for product {ProductId}", locations.Count, _productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load FROM locations for product {ProductId}", _productId);
                AvailableFromLocations = new ObservableCollection<LocationDto>();
            }
        }

        /// <summary>
        /// Updates TO locations (all locations except FROM)
        /// </summary>
        private void UpdateToLocations()
        {
            if (FromLocation == null)
            {
                AvailableToLocations = [];
                ToLocation = null;
                return;
            }

            try
            {
                List<Inventory> inventoryRecords = _inventoryCacheReadService.GetInventoryByProductIdInCache(_productId);

                // Include all locations except the FROM location
                List<LocationDto> locations = inventoryRecords
                                              .Where(inv => inv.LocationId != FromLocation.LocationId)
                                              .Select(inv => new LocationDto
                                              {
                                                  LocationId = inv.LocationId,
                                                  LocationName = GetLocationName(inv.LocationId),
                                                  InventoryId = inv.InventoryId
                                              })
                                              .OrderBy(l => l.LocationName)
                                              .ToList();

                AvailableToLocations = new ObservableCollection<LocationDto>(locations);

                // Clear TO location selection when FROM changes
                ToLocation = null;

                // Auto-select first TO location if available
                if (AvailableToLocations.Any())
                {
                    ToLocation = AvailableToLocations.First();
                }

                _logger.LogDebug("Updated {Count} TO locations", locations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update TO locations");
                AvailableToLocations = [];
                ToLocation = null;
            }
        }

        /// <summary>
        /// Loads stock for FROM location
        /// </summary>
        private void LoadFromLocationStock()
        {
            if (FromLocation == null)
            {
                FromLocationStock = 0;
                FromLocationAvailable = 0;
                return;
            }

            try
            {
                Inventory? inventory = _inventoryCacheReadService.GetInventoryByProductAndLocationInCache(
                    _productId,
                    FromLocation.LocationId);

                if (inventory != null)
                {
                    FromLocationStock = inventory.CurrentStock;
                    FromLocationAvailable = inventory.AvailableStock;

                    _logger.LogDebug(
                        "FROM location stock - Current: {Current}, Available: {Available}",
                        FromLocationStock,
                        FromLocationAvailable);
                }
                else
                {
                    FromLocationStock = 0;
                    FromLocationAvailable = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load FROM location stock");
                FromLocationStock = 0;
                FromLocationAvailable = 0;
            }
        }

        /// <summary>
        /// Loads stock for TO location
        /// </summary>
        private void LoadToLocationStock()
        {
            if (ToLocation == null)
            {
                ToLocationStock = 0;
                return;
            }

            try
            {
                Inventory? inventory = _inventoryCacheReadService.GetInventoryByProductAndLocationInCache(
                    _productId,
                    ToLocation.LocationId);

                ToLocationStock = inventory?.CurrentStock ?? 0;

                _logger.LogDebug("TO location current stock: {Stock}", ToLocationStock);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load TO location stock");
                ToLocationStock = 0;
            }
        }

        /// <summary>
        /// Validates the transfer before saving
        /// </summary>
        private void ValidateTransfer()
        {
            ValidationError = null;

            if (FromLocation == null)
            {
                ValidationError = "Please select a FROM location";
                return;
            }

            if (ToLocation == null)
            {
                ValidationError = "Please select a TO location";
                return;
            }

            if (FromLocation.LocationId == ToLocation.LocationId)
            {
                ValidationError = "FROM and TO locations must be different";
                return;
            }

            if (TransferQuantity <= 0)
            {
                ValidationError = "Quantity must be greater than zero";
                return;
            }

            if (TransferQuantity > FromLocationAvailable)
            {
                ValidationError = $"Cannot transfer {TransferQuantity}. Only {FromLocationAvailable} units available at source location.";
                return;
            }
        }

        /// <summary>
        /// Gets location name by ID
        /// </summary>
        private string GetLocationName( int locationId ) =>
            // TODO: Implement LocationStore lookup
            $"Location {locationId}";

        /// <summary>
        /// Gets the current user ID (placeholder - implement based on your auth system)
        /// </summary>
        private int GetCurrentUserId() =>
            // TODO: Get from authentication service or user context
            1; // Placeholder

        #endregion

        #region Command Implementations

        /// <summary>
        /// Increases the transfer quantity by 1
        /// </summary>
        private void ExecuteIncreaseQuantity()
        {
            if (TransferQuantity < FromLocationAvailable)
            {
                TransferQuantity++;
            }
        }

        /// <summary>
        /// Decreases the transfer quantity by 1
        /// </summary>
        private void ExecuteDecreaseQuantity()
        {
            if (TransferQuantity > 0)
            {
                TransferQuantity--;
            }
        }

        /// <summary>
        /// Saves the stock transfer
        /// </summary>
        private async Task ExecuteSaveTransferAsync()
        {
            if (FromLocation == null || ToLocation == null || _product == null)
            {
                _logger.LogWarning("⚠️ Cannot save transfer: Location or Product is null");
                return;
            }

            ValidateTransfer();

            if (!string.IsNullOrEmpty(ValidationError))
            {
                _logger.LogWarning("⚠️ Validation failed: {Error}", ValidationError);
                return;
            }

            // Confirm transfer with user
            MessageBoxResult result = MessageBox.Show(
                $"Transfer {TransferQuantity} unit(s) of {ProductName}\n" +
                $"FROM: {FromLocation.LocationName}\n" +
                $"TO: {ToLocation.LocationName}\n\n" +
                $"Continue?",
                "Confirm Transfer",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                _logger.LogInformation("Transfer cancelled by user");
                return;
            }

            IsLoading = true;

            try
            {
                _logger.LogInformation(
                    "🔄 Transferring {Quantity} units of Product {ProductId} from Location {FromId} to Location {ToId}",
                    TransferQuantity,
                    _productId,
                    FromLocation.LocationId,
                    ToLocation.LocationId);

                // Call inventory service to perform transfer
                DatabaseResult<InventoryMovement> transferResult = await _inventoryManager.TransferStockAsync(
                    _productId,
                    FromLocation.LocationId,
                    ToLocation.LocationId,
                    TransferQuantity,
                    TransferNotes,
                    GetCurrentUserId());

                if (!transferResult.IsSuccess)
                {
                    ValidationError = transferResult.ErrorMessage ?? "Failed to transfer stock.";
                    _logger.LogError("Stock transfer failed: {Error}", transferResult.ErrorMessage);
                    return;
                }

                _logger.LogInformation(
                    "✅ Stock transfer successful - Movement ID: {MovementId}",
                    transferResult.Value?.MovementId);

                MessageBox.Show(
                    $"Successfully transferred {TransferQuantity} unit(s)\n" +
                    $"FROM: {FromLocation.LocationName}\n" +
                    $"TO: {ToLocation.LocationName}",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Close the modal after successful transfer
                _modalNavigationControl.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Failed to transfer stock for Product: {ProductId}",
                    _productId);

                ValidationError = "Failed to transfer stock. Please try again.";
                MessageBox.Show(
                    "Failed to transfer stock. Please try again.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Cancels the transfer and closes the modal
        /// </summary>
        private void ExecuteCancelCommand()
        {
            _logger.LogInformation("❌ Stock transfer cancelled by user");
            _modalNavigationControl.Close();
        }

        #endregion
    }
}
