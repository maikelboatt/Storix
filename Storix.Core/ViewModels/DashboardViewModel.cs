using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.SkiaSharpView.WPF;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using SkiaSharp;
using Storix.Application.Common;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.DTO.Products;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Services;
using Storix.Application.Services.Inventories.Interfaces;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Services.Products;
using Storix.Application.Services.Products.Interfaces;

namespace Storix.Core.ViewModels
{
    public class DashboardViewModel:MvxViewModel
    {
        private readonly ICacheInitializerService _cacheInitializerService;
        private readonly IProductService _productService;
        private readonly IOrderItemService _orderItemService;
        private readonly IProductCacheReadService _productCacheReadService;
        private readonly IInventoryManager _inventoryManager;
        private readonly IOrderCacheReadService _orderCacheReadService;
        private readonly ILogger<DashboardViewModel> _logger;

        private bool _isLoading;
        private ObservableCollection<TopProductDto> _topProducts;

        // Dashboard Statistics
        private string _totalRevenue = "$0.00";
        private string _revenueChange = "0%";
        private int _totalProducts;
        private string _productsChange = "0%";
        private int _draftOrders;
        private int _activeOrders;
        private int _fulfilledOrders;
        private int _completedOrders;
        private int _cancelledOrders;
        private string _ordersChange = "0%";
        private int _lowStockItems;
        private string _profitMargin = "0%";
        private string _profitMarginChange = "0%";
        private string _inventoryTurnover = "0x";
        private string _turnoverChange = "0x";

        // Revenue Chart Properties
        private ISeries[] _revenueChartSeries;
        private Axis[] _revenueChartXAxes;
        private Axis[] _revenueChartYAxes;
        private int _selectedTimePeriodIndex;

        #region Constructor

        public DashboardViewModel(
            ICacheInitializerService cacheInitializerService,
            IProductService productService,
            IOrderItemService orderItemService,
            IProductCacheReadService productCacheReadService,
            IInventoryManager inventoryManager,
            IOrderCacheReadService orderCacheReadService,
            ILogger<DashboardViewModel> logger )
        {
            _cacheInitializerService = cacheInitializerService ?? throw new ArgumentNullException(nameof(cacheInitializerService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _orderItemService = orderItemService;
            _productCacheReadService = productCacheReadService ?? throw new ArgumentNullException(nameof(productCacheReadService));
            _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
            _orderCacheReadService = orderCacheReadService ?? throw new ArgumentNullException(nameof(orderCacheReadService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _topProducts = [];

            // Initialize chart axes
            RevenueChartXAxes =
            [
                new Axis
                {
                    Labels =
                    [
                    ],
                    LabelsRotation = 0,
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(226, 232, 240))
                    {
                        StrokeThickness = 1,
                        PathEffect = new DashEffect(
                        [
                            4f, 4f
                        ])
                    }
                }
            ];

            RevenueChartYAxes =
            [
                new Axis
                {
                    Labeler = value => $"${value / 1000:N0}K",
                    TextSize = 12,
                    MinLimit = 0,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(226, 232, 240))
                    {
                        StrokeThickness = 1,
                        PathEffect = new DashEffect(
                        [
                            4f, 4f
                        ])
                    }
                }
            ];


            SelectedTimePeriodIndex = 0; // Default to last 7 days
        }

        #endregion

        #region Properties

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<TopProductDto> TopProducts
        {
            get => _topProducts;
            set => SetProperty(ref _topProducts, value);
        }

        #region Dashboard Card Properties

        /// <summary>
        /// Total Revenue display value
        /// </summary>
        public string TotalRevenue
        {
            get => _totalRevenue;
            set => SetProperty(ref _totalRevenue, value);
        }

        /// <summary>
        /// Revenue change percentage
        /// </summary>
        public string RevenueChange
        {
            get => _revenueChange;
            set => SetProperty(ref _revenueChange, value);
        }

        /// <summary>
        /// Total number of products
        /// </summary>
        public int TotalProducts
        {
            get => _totalProducts;
            set => SetProperty(ref _totalProducts, value, () => { RaisePropertyChanged(() => TotalProductsDisplay); });
        }

        /// <summary>
        /// Total products formatted for display
        /// </summary>
        public string TotalProductsDisplay => TotalProducts.ToString("N0");

        /// <summary>
        /// Products change percentage
        /// </summary>
        public string ProductsChange
        {
            get => _productsChange;
            set => SetProperty(ref _productsChange, value);
        }

        /// <summary>
        /// Number of draft orders
        /// </summary>
        public int DraftOrders
        {
            get => _draftOrders;
            set => SetProperty(
                ref _draftOrders,
                value,
                () =>
                {
                    RaisePropertyChanged(() => DraftOrdersDisplay);
                    RaisePropertyChanged(() => TotalOrders);
                    RaisePropertyChanged(() => TotalOrdersDisplay);
                });
        }

        /// <summary>
        /// Draft orders formatted for display
        /// </summary>
        public string DraftOrdersDisplay => DraftOrders.ToString("N0");

        /// <summary>
        /// Number of active orders
        /// </summary>
        public int ActiveOrders
        {
            get => _activeOrders;
            set => SetProperty(
                ref _activeOrders,
                value,
                () =>
                {
                    RaisePropertyChanged(() => ActiveOrdersDisplay);
                    RaisePropertyChanged(() => TotalOrders);
                    RaisePropertyChanged(() => TotalOrdersDisplay);
                });
        }

        /// <summary>
        /// Active orders formatted for display
        /// </summary>
        public string ActiveOrdersDisplay => ActiveOrders.ToString("N0");

        /// <summary>
        /// Number of fulfilled orders
        /// </summary>
        public int FulfilledOrders
        {
            get => _fulfilledOrders;
            set => SetProperty(
                ref _fulfilledOrders,
                value,
                () =>
                {
                    RaisePropertyChanged(() => FulfilledOrdersDisplay);
                    RaisePropertyChanged(() => TotalOrders);
                    RaisePropertyChanged(() => TotalOrdersDisplay);
                });
        }

        /// <summary>
        /// Fulfilled orders formatted for display
        /// </summary>
        public string FulfilledOrdersDisplay => FulfilledOrders.ToString("N0");

        /// <summary>
        /// Number of completed orders
        /// </summary>
        public int CompletedOrders
        {
            get => _completedOrders;
            set => SetProperty(
                ref _completedOrders,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CompletedOrdersDisplay);
                    RaisePropertyChanged(() => TotalOrders);
                    RaisePropertyChanged(() => TotalOrdersDisplay);
                });
        }

        /// <summary>
        /// Completed orders formatted for display
        /// </summary>
        public string CompletedOrdersDisplay => CompletedOrders.ToString("N0");

        /// <summary>
        /// Number of cancelled orders
        /// </summary>
        public int CancelledOrders
        {
            get => _cancelledOrders;
            set => SetProperty(
                ref _cancelledOrders,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CancelledOrdersDisplay);
                    RaisePropertyChanged(() => TotalOrders);
                    RaisePropertyChanged(() => TotalOrdersDisplay);
                });
        }

        /// <summary>
        /// Cancelled orders formatted for display
        /// </summary>
        public string CancelledOrdersDisplay => CancelledOrders.ToString("N0");

        /// <summary>
        /// Total number of orders (all statuses)
        /// </summary>
        public int TotalOrders => DraftOrders + ActiveOrders + FulfilledOrders + CompletedOrders + CancelledOrders;

        /// <summary>
        /// Total orders formatted for display
        /// </summary>
        public string TotalOrdersDisplay => TotalOrders.ToString("N0");

        /// <summary>
        /// Orders change percentage
        /// </summary>
        public string OrdersChange
        {
            get => _ordersChange;
            set => SetProperty(ref _ordersChange, value);
        }

        /// <summary>
        /// Number of low stock items
        /// </summary>
        public int LowStockItems
        {
            get => _lowStockItems;
            set => SetProperty(ref _lowStockItems, value, () => { RaisePropertyChanged(() => LowStockItemsDisplay); });
        }

        /// <summary>
        /// Low stock items formatted for display
        /// </summary>
        public string LowStockItemsDisplay => LowStockItems.ToString("N0");

        /// <summary>
        /// Profit margin percentage
        /// </summary>
        public string ProfitMargin
        {
            get => _profitMargin;
            set => SetProperty(ref _profitMargin, value);
        }

        /// <summary>
        /// Profit margin change
        /// </summary>
        public string ProfitMarginChange
        {
            get => _profitMarginChange;
            set => SetProperty(ref _profitMarginChange, value);
        }

        /// <summary>
        /// Inventory turnover rate
        /// </summary>
        public string InventoryTurnover
        {
            get => _inventoryTurnover;
            set => SetProperty(ref _inventoryTurnover, value);
        }

        /// <summary>
        /// Inventory turnover change
        /// </summary>
        public string TurnoverChange
        {
            get => _turnoverChange;
            set => SetProperty(ref _turnoverChange, value);
        }

        #endregion

        #region Revenue Chart Properties

        /// <summary>
        /// Chart series for revenue trend
        /// </summary>
        public ISeries[] RevenueChartSeries
        {
            get => _revenueChartSeries;
            set => SetProperty(ref _revenueChartSeries, value);
        }

        /// <summary>
        /// X-axes configuration
        /// </summary>
        public Axis[] RevenueChartXAxes
        {
            get => _revenueChartXAxes;
            set => SetProperty(ref _revenueChartXAxes, value);
        }

        /// <summary>
        /// Y-axes configuration
        /// </summary>
        public Axis[] RevenueChartYAxes
        {
            get => _revenueChartYAxes;
            set => SetProperty(ref _revenueChartYAxes, value);
        }

        /// <summary>
        /// Selected time period index (0=7days, 1=30days, 2=3months, 3=year)
        /// </summary>
        public int SelectedTimePeriodIndex
        {
            get => _selectedTimePeriodIndex;
            set
            {
                if (SetProperty(ref _selectedTimePeriodIndex, value))
                {
                    _ = LoadRevenueChartDataAsync();
                }
            }
        }

        #endregion

        #endregion

        #region Lifecycle Methods

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                _logger.LogInformation("📊 Initializing Dashboard...");

                // Initialize cache first
                await _cacheInitializerService.InitializeCacheAsync();

                // Load all dashboard data
                await LoadDashboardDataAsync();

                await LoadRevenueChartDataAsync(); // Load chart data

                _logger.LogInformation("✅ Dashboard initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize dashboard");
            }
            finally
            {
                IsLoading = false;
            }

            await base.Initialize();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads all dashboard data
        /// </summary>
        private async Task LoadDashboardDataAsync()
        {
            // Load data in parallel for better performance
            await Task.WhenAll(
                LoadTopProductsAsync(),
                LoadProductStatisticsAsync(),
                LoadOrderStatisticsAsync(),
                LoadInventoryStatisticsAsync(),
                LoadFinancialStatisticsAsync()
            );
        }


        /// <summary>
        /// Loads top 5 best-selling products
        /// </summary>
        private async Task LoadTopProductsAsync()
        {
            try
            {
                _logger.LogDebug("Loading top products...");

                DatabaseResult<IEnumerable<TopProductDto>> result = await _productService.GetTop5BestSellersAsync();

                if (result is { IsSuccess: true, Value: not null })
                {
                    TopProducts = new ObservableCollection<TopProductDto>(
                        result.Value.Select(( p, index ) => new TopProductDto(
                                                p.ProductId,
                                                p.Rank,
                                                p.ProductName,
                                                p.SKU,
                                                p.TotalRevenue,
                                                p.UnitsSold)
                        )
                    );

                    _logger.LogDebug("Loaded {Count} top products", TopProducts.Count);
                }
                else
                {
                    _logger.LogWarning("Failed to load top products: {Error}", result.ErrorMessage);
                    TopProducts = [];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading top products");
                TopProducts = [];
            }
        }

        /// <summary>
        /// Loads product-related statistics
        /// </summary>
        private async Task LoadProductStatisticsAsync()
        {
            try
            {
                _logger.LogDebug("Loading product statistics...");

                // Get all products from cache
                IEnumerable<ProductDto> allProducts = _productCacheReadService.GetActiveProductsFromCache();
                TotalProducts = allProducts.Count();

                // Calculate change (mock - you can implement actual comparison logic)
                ProductsChange = "↑ 12%";

                _logger.LogDebug("Total products: {Count}", TotalProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product statistics");
                TotalProducts = 0;
                ProductsChange = "0%";
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads order-related statistics
        /// </summary>
        private async Task LoadOrderStatisticsAsync()
        {
            try
            {
                _logger.LogDebug("Loading order statistics...");

                // Count draft orders
                DraftOrders = _orderCacheReadService
                              .GetDraftOrdersInCache()
                              .Count();

                // Count active orders
                ActiveOrders = _orderCacheReadService
                               .GetActiveOrdersInCache()
                               .Count();

                // Count fulfilled orders
                FulfilledOrders = _orderCacheReadService
                                  .GetFulfilledOrdersInCache()
                                  .Count();

                // Count completed orders
                CompletedOrders = _orderCacheReadService
                                  .GetCompletedOrdersInCache()
                                  .Count();

                // Count cancelled orders
                CancelledOrders = _orderCacheReadService
                                  .GetCancelledOrdersInCache()
                                  .Count();

                // Calculate change (mock - you can implement actual comparison logic)
                OrdersChange = "↑ 8%";

                _logger.LogDebug(
                    "Order statistics - Draft: {Draft}, Active: {Active}, Fulfilled: {Fulfilled}, Completed: {Completed}, Cancelled: {Cancelled}, Total: {Total}",
                    DraftOrders,
                    ActiveOrders,
                    FulfilledOrders,
                    CompletedOrders,
                    CancelledOrders,
                    TotalOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order statistics");
                DraftOrders = 0;
                ActiveOrders = 0;
                FulfilledOrders = 0;
                CompletedOrders = 0;
                CancelledOrders = 0;
                OrdersChange = "0%";
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads inventory-related statistics
        /// </summary>
        private async Task LoadInventoryStatisticsAsync()
        {
            try
            {
                _logger.LogDebug("Loading inventory statistics...");

                // Get all products from cache
                List<ProductDto> allProducts = _productCacheReadService
                                               .GetActiveProductsFromCache()
                                               .ToList();

                _logger.LogDebug("Total products for inventory check: {Count}", allProducts.Count);

                // Count low stock items
                int lowStockCount = 0;

                foreach (ProductDto product in allProducts)
                {
                    try
                    {
                        int currentStock = _inventoryManager.GetCurrentStockForProduct(product.ProductId);

                        // Check if stock is low (below minimum but not zero)
                        if (currentStock <= 0 || currentStock > product.MinStockLevel) continue;

                        lowStockCount++;
                        _logger.LogDebug(
                            "Low stock: {ProductName} - Current: {Current}, Min: {Min}",
                            product.Name,
                            currentStock,
                            product.MinStockLevel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error checking stock for product {ProductId}", product.ProductId);
                    }
                }

                LowStockItems = lowStockCount;

                _logger.LogDebug("Low stock items: {Count}", LowStockItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory statistics");
                LowStockItems = 0;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads financial statistics (revenue, profit margin, turnover)
        /// </summary>
        private async Task LoadFinancialStatisticsAsync()
        {
            try
            {
                _logger.LogDebug("Loading financial statistics...");

                List<ProductDto> allProducts = _productCacheReadService
                                               .GetActiveProductsFromCache()
                                               .ToList();

                List<OrderDto> allOrders = _orderCacheReadService
                                           .GetAllOrdersInCache()
                                           .ToList();

                _logger.LogDebug(
                    "Calculating financial stats for {ProductCount} products and {OrderCount} orders",
                    allProducts.Count,
                    allOrders.Count);

                // Calculate total revenue from completed orders
                decimal totalRevenue = await CalculateTotalRevenueAsync(allOrders);
                _logger.LogInformation("Calculated total revenue: ${Revenue}", totalRevenue);

                TotalRevenue = totalRevenue switch
                {
                    // Format revenue
                    >= 1000000 => $"${totalRevenue / 1000000:N2}M",
                    >= 1000    => $"${totalRevenue / 1000:N1}K",
                    _          => $"${totalRevenue:N2}"
                };

                RevenueChange = "↑ 15%";

                // Calculate profit margin
                decimal profitMargin = CalculateProfitMargin(allProducts);
                ProfitMargin = $"{profitMargin:N1}%";
                ProfitMarginChange = "↑ 3.2%";

                // Calculate inventory turnover
                double turnover = CalculateInventoryTurnover(allProducts, totalRevenue);
                InventoryTurnover = $"{turnover:N1}x";
                TurnoverChange = "↑ 0.5x";

                _logger.LogInformation(
                    "✅ Financial stats - Revenue: {Revenue}, Profit: {Profit}%, Turnover: {Turnover}x",
                    TotalRevenue,
                    ProfitMargin,
                    InventoryTurnover);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading financial statistics");
                TotalRevenue = "$0.00";
                RevenueChange = "0%";
                ProfitMargin = "0%";
                ProfitMarginChange = "0%";
                InventoryTurnover = "0x";
                TurnoverChange = "0x";
            }

            await Task.CompletedTask;
        }

        // TODO: Update the current stock immediately after an order is fulfilled.

        /// <summary>
        /// Calculates total revenue from completed and fulfilled orders
        /// </summary>
        private async Task<decimal> CalculateTotalRevenueAsync( List<OrderDto> orders )
        {
            try
            {
                // Get completed and fulfilled orders
                List<OrderDto> revenueOrders = orders
                                               .Where(o => o.Status == Domain.Enums.OrderStatus.Completed ||
                                                           o.Status == Domain.Enums.OrderStatus.Fulfilled)
                                               .ToList();

                _logger.LogDebug(
                    "Calculating revenue from {Count} completed/fulfilled orders",
                    revenueOrders.Count);

                decimal totalRevenue = 0;

                // Calculate actual revenue from order items
                foreach (OrderDto order in revenueOrders)
                {
                    try
                    {
                        // Get actual order total from OrderItemManager
                        DatabaseResult<IEnumerable<OrderItemDto>> orderItemsResult = await _orderItemService.GetOrderItemsByOrderIdAsync(order.OrderId);

                        if (orderItemsResult.IsSuccess && orderItemsResult.Value != null)
                        {
                            decimal orderTotal = orderItemsResult.Value.Sum(item => item.Quantity * item.UnitPrice);
                            totalRevenue += orderTotal;

                            _logger.LogDebug(
                                "Order {OrderId}: ${Total}",
                                order.OrderId,
                                orderTotal);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error calculating total for order {OrderId}", order.OrderId);
                    }
                }

                _logger.LogInformation("Total revenue from {Count} orders: ${Revenue}", revenueOrders.Count, totalRevenue);

                return totalRevenue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CalculateTotalRevenueAsync");
                return 0;
            }
        }

        /// <summary>
        /// Calculates average profit margin across all products
        /// </summary>
        private decimal CalculateProfitMargin( List<ProductDto> products )
        {
            if (products.Count == 0)
            {
                _logger.LogWarning("No products available for profit margin calculation");
                return 0;
            }

            decimal totalMargin = 0;
            int validProducts = 0;

            foreach (ProductDto product in products)
            {
                if (product.Price > 0 && product.Cost >= 0)
                {
                    decimal margin = (product.Price - product.Cost) / product.Price * 100;
                    totalMargin += margin;
                    validProducts++;
                }
            }

            decimal averageMargin = validProducts > 0
                ? totalMargin / validProducts
                : 0;

            _logger.LogDebug(
                "Calculated profit margin: {Margin}% from {Count} products",
                averageMargin,
                validProducts);

            return averageMargin;
        }


        /// <summary>
        /// Calculates inventory turnover rate
        /// </summary>
        private double CalculateInventoryTurnover( List<ProductDto> products, decimal revenue )
        {
            try
            {
                // Calculate total inventory value
                decimal totalInventoryValue = 0;

                foreach (ProductDto product in products)
                {
                    int stock = _inventoryManager.GetCurrentStockForProduct(product.ProductId);
                    totalInventoryValue += stock * product.Cost;
                }

                _logger.LogDebug("Total inventory value: ${Value}", totalInventoryValue);

                if (totalInventoryValue == 0)
                {
                    _logger.LogWarning("Total inventory value is 0, cannot calculate turnover");
                    return 0;
                }

                // Turnover = Cost of Goods Sold / Average Inventory
                // Using revenue * 0.7 as COGS estimate (70% of revenue)
                decimal cogs = revenue * 0.7m;
                double turnover = (double)(cogs / totalInventoryValue);

                _logger.LogDebug(
                    "Inventory turnover: {Turnover}x (COGS: ${COGS}, Inventory: ${Inventory})",
                    turnover,
                    cogs,
                    totalInventoryValue);

                return turnover;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating inventory turnover");
                return 0;
            }
        }

        #region Load Revenue Chart Data

        /// <summary>
        /// Loads revenue chart data based on selected time period
        /// </summary>
        private async Task LoadRevenueChartDataAsync()
        {
            try
            {
                _logger.LogDebug("Loading revenue chart data for period index: {Index}", SelectedTimePeriodIndex);

                (string[] Labels, double[] Values) revenueData = await GetRevenueDataForPeriodAsync(SelectedTimePeriodIndex);

                // Update X-axis labels
                RevenueChartXAxes = new[]
                {
                    new Axis
                    {
                        Labels = revenueData.Labels,
                        LabelsRotation = 0,
                        TextSize = 12,
                        SeparatorsPaint = new SolidColorPaint(new SKColor(226, 232, 240))
                        {
                            StrokeThickness = 1,
                            PathEffect = new DashEffect(
                            [
                                4f, 4f
                            ])
                        }
                    }
                };

                // Create line series
                RevenueChartSeries = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = revenueData.Values,
                        Name = "Revenue",
                        Fill = null,                                            // Transparent fill
                        Stroke = new SolidColorPaint(new SKColor(59, 130, 246)) // Blue
                        {
                            StrokeThickness = 3
                        },
                        GeometrySize = 10,
                        GeometryStroke = new SolidColorPaint(new SKColor(59, 130, 246))
                        {
                            StrokeThickness = 3
                        },
                        GeometryFill = new SolidColorPaint(SKColors.White),
                        LineSmoothness = 0.7
                    }
                };

                _logger.LogDebug("Revenue chart loaded with {Count} data points", revenueData.Values.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading revenue chart data");
            }
        }

        /// <summary>
        /// Gets revenue data for the selected time period
        /// </summary>
        private async Task<(string[] Labels, double[] Values)> GetRevenueDataForPeriodAsync( int periodIndex )
        {
            DateTime endDate = DateTime.Now.Date;
            DateTime startDate;
            string[] labels;
            double[] values;

            switch (periodIndex)
            {
                case 0: // Last 7 days
                    startDate = endDate.AddDays(-6);
                    labels = new string[7];
                    values = new double[7];

                    for (int i = 0; i < 7; i++)
                    {
                        DateTime date = startDate.AddDays(i);
                        labels[i] = date.ToString("ddd"); // Mon, Tue, Wed...
                        values[i] = (double)await GetRevenueForDateAsync(date);
                    }
                    break;

                case 1: // Last 30 days (grouped by weeks)
                    startDate = endDate.AddDays(-27);
                    labels = new[]
                    {
                        "Week 1",
                        "Week 2",
                        "Week 3",
                        "Week 4"
                    };
                    values = new double[4];

                    for (int week = 0; week < 4; week++)
                    {
                        DateTime weekStart = startDate.AddDays(week * 7);
                        DateTime weekEnd = weekStart.AddDays(6);
                        values[week] = (double)await GetRevenueForDateRangeAsync(weekStart, weekEnd);
                    }
                    break;

                case 2: // Last 3 months
                    labels = new string[3];
                    values = new double[3];

                    for (int i = 0; i < 3; i++)
                    {
                        DateTime monthDate = endDate.AddMonths(-(2 - i));
                        DateTime monthStart = new(monthDate.Year, monthDate.Month, 1);
                        DateTime monthEnd = monthStart
                                            .AddMonths(1)
                                            .AddDays(-1);

                        labels[i] = monthStart.ToString("MMM"); // Jan, Feb, Mar...
                        values[i] = (double)await GetRevenueForDateRangeAsync(monthStart, monthEnd);
                    }
                    break;

                case 3: // Last year (12 months)
                    labels = new string[12];
                    values = new double[12];

                    for (int i = 0; i < 12; i++)
                    {
                        DateTime monthDate = endDate.AddMonths(-(11 - i));
                        DateTime monthStart = new(monthDate.Year, monthDate.Month, 1);
                        DateTime monthEnd = monthStart
                                            .AddMonths(1)
                                            .AddDays(-1);

                        labels[i] = monthStart.ToString("MMM");
                        values[i] = (double)await GetRevenueForDateRangeAsync(monthStart, monthEnd);
                    }
                    break;

                default:
                    labels = new[]
                    {
                        "No Data"
                    };
                    values = new[]
                    {
                        0.0
                    };
                    break;
            }

            return (labels, values);
        }

        /// <summary>
        /// Gets revenue for a specific date
        /// </summary>
        private async Task<decimal> GetRevenueForDateAsync( DateTime date )
        {
            try
            {
                List<OrderDto> orders = _orderCacheReadService
                                        .GetCompletedOrdersInCache()
                                        .Concat(_orderCacheReadService.GetFulfilledOrdersInCache())
                                        .Where(o => o.OrderDate.Date == date.Date)
                                        .ToList();

                return await CalculateOrdersRevenueAsync(orders);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting revenue for date {Date}", date);
                return 0;
            }
        }

        /// <summary>
        /// Gets revenue for a date range
        /// </summary>
        private async Task<decimal> GetRevenueForDateRangeAsync( DateTime startDate, DateTime endDate )
        {
            try
            {
                List<OrderDto> orders = _orderCacheReadService
                                        .GetCompletedOrdersInCache()
                                        .Concat(_orderCacheReadService.GetFulfilledOrdersInCache())
                                        .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date)
                                        .ToList();

                return await CalculateOrdersRevenueAsync(orders);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error getting revenue for range {Start} to {End}",
                    startDate,
                    endDate);
                return 0;
            }
        }

        /// <summary>
        /// Calculates total revenue from a list of orders
        /// </summary>
        private async Task<decimal> CalculateOrdersRevenueAsync( List<OrderDto> orders )
        {
            decimal totalRevenue = 0;

            foreach (OrderDto order in orders)
            {
                try
                {
                    DatabaseResult<IEnumerable<OrderItemDto>> orderItemsResult = await _orderItemService.GetOrderItemsByOrderIdAsync(order.OrderId);

                    if (orderItemsResult.IsSuccess && orderItemsResult.Value != null)
                    {
                        decimal orderTotal = orderItemsResult.Value.Sum(item => item.Quantity * item.UnitPrice);
                        totalRevenue += orderTotal;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error calculating total for order {OrderId}", order.OrderId);
                }
            }

            return totalRevenue;
        }

        #endregion

        #endregion
    }
}
