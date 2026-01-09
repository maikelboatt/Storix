using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.Products;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Services.Inventories.Interfaces;
using Storix.Application.Services.Print;
using Storix.Application.Services.Products.Interfaces;
using Storix.Core.Control;
using Storix.Core.ViewModels.Products;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Categories
{
    /// <summary>
    /// ViewModel for displaying comprehensive category details.
    /// Shows category information, statistics, subcategories, and products.
    /// </summary>
    public class CategoryDetailsViewModel:MvxViewModel<int>
    {
        private readonly ICategoryCacheReadService _categoryCacheReadService;
        private readonly IProductCacheReadService _productCacheReadService;
        private readonly IInventoryCacheReadService _inventoryCacheReadService;
        private readonly IPrintService _printService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<CategoryDetailsViewModel> _logger;

        private CategoryDto? _category;
        private bool _isLoading;
        private int _categoryId;
        private ObservableCollection<SubcategoryInfo> _subcategories;
        private ObservableCollection<ProductSummary> _recentProducts;

        #region Constructor

        public CategoryDetailsViewModel(
            ICategoryCacheReadService categoryCacheReadService,
            IProductCacheReadService productCacheReadService,
            IInventoryCacheReadService inventoryCacheReadService,
            IPrintService printService,
            IModalNavigationControl modalNavigationControl,
            ILogger<CategoryDetailsViewModel> logger )
        {
            _categoryCacheReadService = categoryCacheReadService ?? throw new ArgumentNullException(nameof(categoryCacheReadService));
            _productCacheReadService = productCacheReadService ?? throw new ArgumentNullException(nameof(productCacheReadService));
            _inventoryCacheReadService = inventoryCacheReadService ?? throw new ArgumentNullException(nameof(inventoryCacheReadService));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _modalNavigationControl = modalNavigationControl ?? throw new ArgumentNullException(nameof(modalNavigationControl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _subcategories = [];
            _recentProducts = [];

            // Initialize commands
            CloseCommand = new MvxCommand(ExecuteCloseCommand);
            EditCategoryCommand = new MvxCommand(ExecuteEditCategoryCommand, () => CanEditCategory);
            PrintDetailsCommand = new MvxCommand(ExecutePrintDetailsCommand, () => CanPrintDetails);
            ViewAllProductsCommand = new MvxCommand(ExecuteViewAllProductsCommand, () => CanViewAllProducts);
        }

        #endregion

        #region Commands

        public IMvxCommand CloseCommand { get; }
        public IMvxCommand EditCategoryCommand { get; }
        public IMvxCommand PrintDetailsCommand { get; }
        public IMvxCommand ViewAllProductsCommand { get; }

        #endregion

        #region Lifecycle Methods

        public override void Prepare( int parameter )
        {
            _categoryId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;

            try
            {
                await LoadCategoryDetailsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load category details for ID: {CategoryId}", _categoryId);
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
        /// The category being displayed
        /// </summary>
        public CategoryDto? Category
        {
            get => _category;
            private set => SetProperty(
                ref _category,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanEditCategory);
                    RaisePropertyChanged(() => CanPrintDetails);
                    RaisePropertyChanged(() => IsParentCategory);
                    EditCategoryCommand.RaiseCanExecuteChanged();
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
        /// Whether this is a parent category (has no parent itself)
        /// </summary>
        public bool IsParentCategory => Category?.ParentCategoryId == null;

        #endregion

        #region Parent Category Properties

        private string? _parentCategoryName;

        /// <summary>
        /// Name of the parent category (if this is a subcategory)
        /// </summary>
        public string? ParentCategoryName
        {
            get => _parentCategoryName;
            private set => SetProperty(ref _parentCategoryName, value);
        }

        #endregion

        #region Statistics Properties

        private int _totalProducts;
        private int _totalSubcategories;
        private decimal _totalCategoryValue;

        /// <summary>
        /// Total number of products in this category
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
        /// Total number of subcategories
        /// </summary>
        public int TotalSubcategories
        {
            get => _totalSubcategories;
            private set => SetProperty(ref _totalSubcategories, value, () => { RaisePropertyChanged(() => HasSubcategories); });
        }

        /// <summary>
        /// Total inventory value of products in this category
        /// </summary>
        public decimal TotalCategoryValue
        {
            get => _totalCategoryValue;
            private set => SetProperty(ref _totalCategoryValue, value);
        }

        /// <summary>
        /// Whether category has products
        /// </summary>
        public bool HasProducts => TotalProducts > 0;

        /// <summary>
        /// Whether category has subcategories
        /// </summary>
        public bool HasSubcategories => TotalSubcategories > 0;

        #endregion

        #region Collection Properties

        /// <summary>
        /// List of subcategories
        /// </summary>
        public ObservableCollection<SubcategoryInfo> Subcategories
        {
            get => _subcategories;
            private set => SetProperty(ref _subcategories, value);
        }

        /// <summary>
        /// Recent products in this category (limited to 5 for preview)
        /// </summary>
        public ObservableCollection<ProductSummary> RecentProducts
        {
            get => _recentProducts;
            private set => SetProperty(ref _recentProducts, value);
        }

        #endregion

        #region Command Can Execute Properties

        public bool CanEditCategory => Category != null && !IsLoading;
        public bool CanPrintDetails => Category != null && !IsLoading;
        public bool CanViewAllProducts => HasProducts && !IsLoading;

        #endregion

        #region Methods

        /// <summary>
        /// Loads complete category details including statistics and related data
        /// </summary>
        private async Task LoadCategoryDetailsAsync()
        {
            _logger.LogInformation("📁 Loading category details for ID: {CategoryId}", _categoryId);

            // Load category from cache
            CategoryDto? category = _categoryCacheReadService.GetCategoryByIdInCache(_categoryId);

            if (category == null)
            {
                _logger.LogWarning("⚠️ Category with ID {CategoryId} not found in cache", _categoryId);
                return;
            }

            Category = category;

            // Load parent category name if this is a subcategory
            LoadParentCategoryName();

            // Load subcategories
            LoadSubcategories();

            // Load products and statistics
            LoadProductsAndStatistics();

            _logger.LogInformation(
                "✅ Loaded category details: {CategoryName} (ID: {CategoryId})",
                Category.Name,
                Category.CategoryId);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads parent category name if this is a subcategory
        /// </summary>
        private void LoadParentCategoryName()
        {
            if (Category?.ParentCategoryId == null)
            {
                ParentCategoryName = null;
                return;
            }

            try
            {
                CategoryDto? parentCategory = _categoryCacheReadService.GetCategoryByIdInCache(Category.ParentCategoryId.Value);
                ParentCategoryName = parentCategory?.Name ?? "Unknown Parent";

                _logger.LogDebug(
                    "Loaded parent category: {ParentName} (ID: {ParentId})",
                    ParentCategoryName,
                    Category.ParentCategoryId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load parent category for {CategoryId}", _categoryId);
                ParentCategoryName = "Unknown Parent";
            }
        }

        /// <summary>
        /// Loads subcategories of this category
        /// </summary>
        private void LoadSubcategories()
        {
            if (Category == null) return;

            try
            {
                // Get all categories and filter for subcategories of this category
                IEnumerable<CategoryDto> allCategories = _categoryCacheReadService.GetAllActiveCategoriesInCache();
                List<SubcategoryInfo> subcategoryList = allCategories
                                                        .Where(c => c.ParentCategoryId == _categoryId)
                                                        .Select(c => new SubcategoryInfo
                                                        {
                                                            CategoryId = c.CategoryId,
                                                            Name = c.Name,
                                                            Description = c.Description,
                                                            ProductCount = GetProductCountForCategory(c.CategoryId)
                                                        })
                                                        .OrderBy(c => c.Name)
                                                        .ToList();

                Subcategories = new ObservableCollection<SubcategoryInfo>(subcategoryList);
                TotalSubcategories = subcategoryList.Count;

                _logger.LogDebug("Loaded {Count} subcategories", TotalSubcategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load subcategories for category {CategoryId}", _categoryId);
                Subcategories = [];
                TotalSubcategories = 0;
            }
        }

        /// <summary>
        /// Loads products and calculates statistics for this category
        /// </summary>
        private void LoadProductsAndStatistics()
        {
            if (Category == null) return;

            try
            {
                // Get all products in this category
                IEnumerable<ProductDto> allProducts = _productCacheReadService.GetActiveProductsFromCache();
                List<ProductDto> categoryProducts = allProducts
                                                    .Where(p => p.CategoryId == _categoryId)
                                                    .ToList();

                TotalProducts = categoryProducts.Count;

                // Calculate total category value
                decimal totalValue = 0;
                List<ProductSummary> productSummaries = new();

                foreach (ProductDto product in categoryProducts.Take(5)) // Only take first 5 for preview
                {
                    int stock = GetTotalStockForProduct(product.ProductId);
                    decimal productValue = stock * product.Cost;
                    totalValue += productValue;

                    productSummaries.Add(
                        new ProductSummary
                        {
                            ProductId = product.ProductId,
                            Name = product.Name,
                            SKU = product.SKU,
                            Price = product.Price,
                            Stock = stock
                        });
                }

                // Calculate total value for all products (not just first 5)
                foreach (ProductDto product in categoryProducts)
                {
                    int stock = GetTotalStockForProduct(product.ProductId);
                    TotalCategoryValue += stock * product.Cost;
                }

                RecentProducts = new ObservableCollection<ProductSummary>(productSummaries);

                _logger.LogDebug(
                    "Loaded {Count} products with total value ${Value:N2}",
                    TotalProducts,
                    TotalCategoryValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load products for category {CategoryId}", _categoryId);
                TotalProducts = 0;
                TotalCategoryValue = 0;
                RecentProducts = new ObservableCollection<ProductSummary>();
            }
        }

        /// <summary>
        /// Gets the number of products in a specific category
        /// </summary>
        private int GetProductCountForCategory( int categoryId )
        {
            try
            {
                IEnumerable<ProductDto> allProducts = _productCacheReadService.GetActiveProductsFromCache();
                return allProducts.Count(p => p.CategoryId == categoryId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get product count for category {CategoryId}", categoryId);
                return 0;
            }
        }

        /// <summary>
        /// Gets total stock for a product across all locations
        /// </summary>
        private int GetTotalStockForProduct( int productId )
        {
            try
            {
                return _inventoryCacheReadService.GetCurrentStockForProductInCache(productId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get stock for product {ProductId}", productId);
                return 0;
            }
        }

        #endregion

        #region Command Implementations

        /// <summary>
        /// Closes the category details dialog
        /// </summary>
        private void ExecuteCloseCommand()
        {
            _logger.LogInformation("Closing category details view");
            _modalNavigationControl.Close();
        }

        /// <summary>
        /// Opens the category edit dialogue
        /// </summary>
        private void ExecuteEditCategoryCommand()
        {
            if (Category == null) return;

            _logger.LogInformation("Opening edit dialog for category {CategoryId}", Category.CategoryId);

            _modalNavigationControl.PopUp<CategoryFormViewModel>(_categoryId);
        }

        /// <summary>
        /// Prints category details
        /// </summary>
        private void ExecutePrintDetailsCommand()
        {
            if (Category == null) return;

            _logger.LogInformation("Printing details for category {CategoryId}", Category.CategoryId);

            _printService.PrintCategoryDetails(
                Category,
                ParentCategoryName,
                Subcategories.ToList(),
                RecentProducts.ToList(),
                TotalProducts,
                TotalSubcategories,
                TotalCategoryValue
            );
        }

        /// <summary>
        /// Opens the products list filtered by this category
        /// </summary>
        private void ExecuteViewAllProductsCommand()
        {
            if (Category == null) return;

            _logger.LogInformation("Viewing all products for category {CategoryId}", Category.CategoryId);

            ProductListViewModelParameter param = new()
            {
                EntityId = _categoryId,
                FilterType = ProductFilterType.Category
            };

            _modalNavigationControl.PopUp<ProductListViewModel>(param);
        }

        #endregion
    }
}
