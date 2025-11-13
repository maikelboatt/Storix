using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Categories;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Stores.Categories;
using Storix.Core.Control;

namespace Storix.Core.ViewModels.Categories
{
    /// <summary>
    /// ViewModel for category deletion confirmation dialog.
    /// Displays full category details and handles the deletion operation.
    /// </summary>
    public class CategoryDeleteViewModel:MvxViewModel<int>
    {
        private readonly ICategoryService _categoryService;
        private readonly ICategoryCacheReadService _categoryCacheReadService;
        private readonly ICategoryStore _categoryStore;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<CategoryDeleteViewModel> _logger;

        private CategoryDto? _category;
        private bool _isLoading;
        private int _categoryId;

        public CategoryDeleteViewModel( ICategoryService categoryService,
            ICategoryCacheReadService categoryCacheReadService,
            ICategoryStore categoryStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<CategoryDeleteViewModel> logger )
        {
            _categoryService = categoryService;
            _categoryCacheReadService = categoryCacheReadService;
            _categoryStore = categoryStore;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Initialize commands
            DeleteCommand = new MvxAsyncCommand(ExecuteDeleteCommandAsync, () => CanDelete);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
        }

        private async Task ExecuteDeleteCommandAsync()
        {
            if (Category == null)
            {
                _logger.LogWarning("⚠️ Delete command executed but Category is null. Aborting deletion.");
            }

            IsLoading = true;

            try
            {
                _logger.LogInformation("🗑️ Deleting category:{CategoryId} - {CategoryName}", _categoryId, Category?.Name);

                await _categoryService.SoftDeleteCategoryAsync(_categoryId);
                _logger.LogInformation(
                    "✅  Successfully deleted product: {CategoryId} - {CategoryName}",
                    Category?.CategoryId,
                    Category?.Name);

                _modalNavigationControl.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Failed to delete category: {CategoryId} - {CategoryName}",
                    Category?.CategoryId,
                    Category?.Name);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteCancelCommand()
        {
            _logger.LogInformation("❌ Category deletion cancelled by user");
            _modalNavigationControl.Close();
        }

        #region LifeCycle Methods

        public override void Prepare( int parameter )
        {
            _categoryId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;

            try
            {
                await LoadCategoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading category details for deletion. CategoryId: {CategoryId}", _categoryId);
                _modalNavigationControl.Close();
            }
            finally
            {
                IsLoading = false;
            }
            await base.Initialize();
        }

        #endregion

        #region Commands

        public IMvxCommand DeleteCommand { get; }
        public IMvxCommand CancelCommand { get; }

        #endregion


        #region Properties

        public string ParentCategoryName
        {
            get
            {
                if (Category == null || Category.ParentCategoryId == null)
                {
                    return "N/A";
                }

                string? parentName = _categoryStore.GetCategoryName(Category.ParentCategoryId.Value);
                return parentName ?? "N/A";
            }
        }

        /// <summary>
        /// The product to be deleted with all its details
        /// </summary>
        public CategoryDto? Category
        {
            get => _category;
            private set => SetProperty(
                ref _category,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanDelete);
                    DeleteCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Indicates whether a deletion operation is in progress
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(
                ref _isLoading,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanDelete);
                    RaisePropertyChanged(() => CanCancel);
                    DeleteCommand.RaiseCanExecuteChanged();
                    CancelCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Whether the delete command can be executed
        /// </summary>
        public bool CanDelete => Category != null && !IsLoading;

        /// <summary>
        /// Whether the cancel command can be executed
        /// </summary>
        public bool CanCancel => !IsLoading;

        public bool HasImageUrl => !string.IsNullOrWhiteSpace(Category?.ImageUrl);
        public bool IsImageLoading => false;

        #endregion

        #region Methods

        private async Task LoadCategoryAsync()
        {
            _logger.LogDebug("🧩 Loading category details for deletion. CategoryId: {CategoryId}", _categoryId);
            CategoryDto? category = _categoryCacheReadService.GetCategoryByIdInCache(_categoryId);

            if (category == null)
            {
                _logger.LogWarning("⚠️ Category with ID {CategoryId} not found in cache.", _categoryId);
            }

            Category = category;

            if (Category != null)
            {
                _logger.LogDebug("✅ Category details loaded successfully for CategoryId: {CategoryId}", _categoryId);
            }

            await Task.CompletedTask;
        }

        #endregion
    }
}
