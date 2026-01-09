using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Products;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Services.Print;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Core.Control;
using Storix.Core.ViewModels.Products;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Suppliers
{
    /// <summary>
    /// ViewModel for displaying comprehensive supplier details.
    /// Shows supplier information, statistics, and associated products.
    /// </summary>
    public class SupplierDetailsViewModel:MvxViewModel<int>
    {
        private readonly ISupplierCacheReadService _supplierCacheReadService;
        private readonly IProductCacheReadService _productCacheReadService;
        private readonly IPrintService _printService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<SupplierDetailsViewModel> _logger;

        private SupplierDto? _supplier;
        private bool _isLoading;
        private int _supplierId;
        private ObservableCollection<ProductSummary> _suppliedProducts;

        #region Constructor

        public SupplierDetailsViewModel(
            ISupplierCacheReadService supplierCacheReadService,
            IProductCacheReadService productCacheReadService,
            IPrintService printService,
            IModalNavigationControl modalNavigationControl,
            ILogger<SupplierDetailsViewModel> logger )
        {
            _supplierCacheReadService = supplierCacheReadService ?? throw new ArgumentNullException(nameof(supplierCacheReadService));
            _productCacheReadService = productCacheReadService ?? throw new ArgumentNullException(nameof(productCacheReadService));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _modalNavigationControl = modalNavigationControl ?? throw new ArgumentNullException(nameof(modalNavigationControl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _suppliedProducts = [];

            // Initialize commands
            CloseCommand = new MvxCommand(ExecuteCloseCommand);
            EditSupplierCommand = new MvxCommand(ExecuteEditSupplierCommand, () => CanEditSupplier);
            PrintDetailsCommand = new MvxCommand(ExecutePrintDetailsCommand, () => CanPrintDetails);
            ViewAllProductsCommand = new MvxCommand(ExecuteViewAllProductsCommand, () => CanViewAllProducts);
            SendEmailCommand = new MvxCommand(ExecuteSendEmailCommand, () => CanSendEmail);
            CallPhoneCommand = new MvxCommand(ExecuteCallPhoneCommand, () => CanCallPhone);
        }

        #endregion

        #region Commands

        public IMvxCommand CloseCommand { get; }
        public IMvxCommand EditSupplierCommand { get; }
        public IMvxCommand PrintDetailsCommand { get; }
        public IMvxCommand ViewAllProductsCommand { get; }
        public IMvxCommand SendEmailCommand { get; }
        public IMvxCommand CallPhoneCommand { get; }

        #endregion

        #region Lifecycle Methods

        public override void Prepare( int parameter )
        {
            _supplierId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;

            try
            {
                await LoadSupplierDetailsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load supplier details for ID: {SupplierId}", _supplierId);
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
        /// The supplier being displayed
        /// </summary>
        public SupplierDto? Supplier
        {
            get => _supplier;
            private set => SetProperty(
                ref _supplier,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanEditSupplier);
                    RaisePropertyChanged(() => CanPrintDetails);
                    RaisePropertyChanged(() => CanSendEmail);
                    RaisePropertyChanged(() => CanCallPhone);
                    EditSupplierCommand.RaiseCanExecuteChanged();
                    PrintDetailsCommand.RaiseCanExecuteChanged();
                    SendEmailCommand.RaiseCanExecuteChanged();
                    CallPhoneCommand.RaiseCanExecuteChanged();
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

        #endregion

        #region Statistics Properties

        private int _totalProducts;
        private int _activeOrders;
        private decimal _totalPurchaseValue;
        private DateTime? _lastOrderDate;

        /// <summary>
        /// Total number of products supplied by this supplier
        /// </summary>
        public int TotalProducts
        {
            get => _totalProducts;
            private set => SetProperty(
                ref _totalProducts,
                value,
                () =>
                {
                    RaisePropertyChanged(() => HasProducts);
                    RaisePropertyChanged(() => CanViewAllProducts);
                    ViewAllProductsCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Number of active purchase orders
        /// </summary>
        public int ActiveOrders
        {
            get => _activeOrders;
            private set => SetProperty(ref _activeOrders, value);
        }

        /// <summary>
        /// Total value of purchases from this supplier
        /// </summary>
        public decimal TotalPurchaseValue
        {
            get => _totalPurchaseValue;
            private set => SetProperty(ref _totalPurchaseValue, value);
        }

        /// <summary>
        /// Date of last purchase order
        /// </summary>
        public DateTime? LastOrderDate
        {
            get => _lastOrderDate;
            private set => SetProperty(ref _lastOrderDate, value, () => { RaisePropertyChanged(() => LastOrderDateDisplay); });
        }

        /// <summary>
        /// Formatted display of last order date
        /// </summary>
        public string LastOrderDateDisplay => LastOrderDate?.ToString("MMM dd, yyyy") ?? "No orders yet";

        /// <summary>
        /// Whether supplier has products
        /// </summary>
        public bool HasProducts => TotalProducts > 0;

        #endregion

        #region Collection Properties

        /// <summary>
        /// Products supplied by this supplier (limited to 5 for preview)
        /// </summary>
        public ObservableCollection<ProductSummary> SuppliedProducts
        {
            get => _suppliedProducts;
            private set => SetProperty(ref _suppliedProducts, value);
        }

        #endregion

        #region Command Can Execute Properties

        public bool CanEditSupplier => Supplier != null && !IsLoading;
        public bool CanPrintDetails => Supplier != null && !IsLoading;
        public bool CanViewAllProducts => HasProducts && !IsLoading;
        public bool CanSendEmail => Supplier != null && !string.IsNullOrWhiteSpace(Supplier.Email) && !IsLoading;
        public bool CanCallPhone => Supplier != null && !string.IsNullOrWhiteSpace(Supplier.Phone) && !IsLoading;

        #endregion

        #region Methods

        /// <summary>
        /// Loads complete supplier details including statistics and related data
        /// </summary>
        private async Task LoadSupplierDetailsAsync()
        {
            _logger.LogInformation("🏢 Loading supplier details for ID: {SupplierId}", _supplierId);

            // Load supplier from cache
            SupplierDto? supplier = _supplierCacheReadService.GetSupplierByIdInCache(_supplierId);

            if (supplier == null)
            {
                _logger.LogWarning("⚠️ Supplier with ID {SupplierId} not found in cache", _supplierId);
                return;
            }

            Supplier = supplier;

            // Load products and statistics
            LoadProductsAndStatistics();

            _logger.LogInformation(
                "✅ Loaded supplier details: {SupplierName} (ID: {SupplierId})",
                Supplier.Name,
                Supplier.SupplierId);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads products and calculates statistics for this supplier
        /// </summary>
        private void LoadProductsAndStatistics()
        {
            if (Supplier == null) return;

            try
            {
                // Get all products from this supplier
                IEnumerable<ProductDto> allProducts = _productCacheReadService.GetActiveProductsFromCache();
                List<ProductDto> supplierProducts = allProducts
                                                    .Where(p => p.SupplierId == _supplierId)
                                                    .ToList();

                TotalProducts = supplierProducts.Count;

                // Calculate statistics
                decimal totalValue = 0;
                List<ProductSummary> productSummaries = [];

                foreach (ProductDto product in supplierProducts.Take(5)) // Only take first 5 for preview
                {
                    decimal productValue = product.Cost;
                    totalValue += productValue;

                    productSummaries.Add(
                        new ProductSummary
                        {
                            ProductId = product.ProductId,
                            Name = product.Name,
                            SKU = product.SKU,
                            Price = product.Price,
                            Stock = 0 // Will be filled by inventory service if needed
                        });
                }

                // Calculate total purchase value for all products
                foreach (ProductDto product in supplierProducts)
                {
                    TotalPurchaseValue += product.Cost;
                }

                SuppliedProducts = new ObservableCollection<ProductSummary>(productSummaries);

                // These would typically come from purchase order data
                ActiveOrders = 0;     // Placeholder - should query purchase orders
                LastOrderDate = null; // Placeholder - should query purchase orders

                _logger.LogDebug(
                    "Loaded {Count} products with total value ${Value:N2}",
                    TotalProducts,
                    TotalPurchaseValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load products for supplier {SupplierId}", _supplierId);
                TotalProducts = 0;
                TotalPurchaseValue = 0;
                SuppliedProducts = [];
            }
        }

        #endregion

        #region Command Implementations

        /// <summary>
        /// Closes the supplier details dialog
        /// </summary>
        private void ExecuteCloseCommand()
        {
            _logger.LogInformation("Closing supplier details view");
            _modalNavigationControl.Close();
        }

        /// <summary>
        /// Opens the supplier edit dialogue
        /// </summary>
        private void ExecuteEditSupplierCommand()
        {
            if (Supplier == null) return;

            _logger.LogInformation("Opening edit dialog for supplier {SupplierId}", Supplier.SupplierId);

            _modalNavigationControl.PopUp<SupplierFormViewModel>(_supplierId);
        }

        /// <summary>
        /// Prints supplier details
        /// </summary>
        private void ExecutePrintDetailsCommand()
        {
            if (Supplier == null) return;

            _logger.LogInformation("Printing details for supplier {SupplierId}", Supplier.SupplierId);

            // _printService.PrintSupplierDetails(
            //     Supplier,
            //     SuppliedProducts.ToList(),
            //     TotalProducts,
            //     ActiveOrders,
            //     TotalPurchaseValue,
            //     LastOrderDate
            // );
        }

        /// <summary>
        /// Opens the products list filtered by this supplier
        /// </summary>
        private void ExecuteViewAllProductsCommand()
        {
            if (Supplier == null) return;

            _logger.LogInformation("Viewing all products for supplier {SupplierId}", Supplier.SupplierId);

            ProductListViewModelParameter param = new()
            {
                EntityId = _supplierId,
                FilterType = ProductFilterType.Supplier
            };

            _modalNavigationControl.PopUp<ProductListViewModel>(param);
        }

        /// <summary>
        /// Opens default email client with supplier email
        /// </summary>
        private void ExecuteSendEmailCommand()
        {
            if (Supplier == null || string.IsNullOrWhiteSpace(Supplier.Email)) return;

            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = $"mailto:{Supplier.Email}",
                        UseShellExecute = true
                    });
                _logger.LogInformation("Opening email client for {Email}", Supplier.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open email client");
            }
        }

        /// <summary>
        /// Opens phone dialer with supplier phone number
        /// </summary>
        private void ExecuteCallPhoneCommand()
        {
            if (Supplier == null || string.IsNullOrWhiteSpace(Supplier.Phone)) return;

            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = $"tel:{Supplier.Phone}",
                        UseShellExecute = true
                    });
                _logger.LogInformation("Opening phone dialer for {Phone}", Supplier.Phone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open phone dialer");
            }
        }

        #endregion
    }

    #region Helper Classes

    public class ProductSummary
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    #endregion
}
