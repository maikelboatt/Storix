using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Categories;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Application.Stores.Categories;
using Storix.Core.Control;
using Storix.Core.ViewModels.Products;
using Storix.Domain.Models;

namespace Storix.Core.ViewModels.Categories
{
    public class CategoryListViewModel:MvxViewModel
    {
        private readonly ICategoryService _categoryService;
        private readonly ICategoryStore _categoryStore;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<CategoryListViewModel> _logger;

        private MvxObservableCollection<CategoryListItemViewModel> _categories = [];
        private List<CategoryListItemViewModel> _allCategories = [];
        private string _searchText = string.Empty;

        private bool _isLoading;

        public CategoryListViewModel( ICategoryService categoryService,
            ICategoryStore categoryStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<CategoryListViewModel> logger )
        {
            _categoryService = categoryService;
            _categoryStore = categoryStore;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Subscribe to write operation events from the store
            _categoryStore.CategoryAdded += OnCategoryAdded;
            _categoryStore.CategoryUpdated += OnCategoryUpdated;
            _categoryStore.CategoryDeleted += OnCategoryDeleted;

            // Initialize commands
            OpenCategoryFormCommand = new MvxCommand<int>(ExecuteCategoryForm);
            OpenCategoryDeleteCommand = new MvxCommand<int>(ExecuteCategoryDelete);

        }

        #region ViewModel LifeCycle

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                LoadCategories();
            }
            finally
            {
                IsLoading = false;
            }
            await base.Initialize();
        }

        public override void ViewDestroy( bool viewFinishing = true )
        {
            // Unsubscribe from store events to prevent memory leaks
            _categoryStore.CategoryAdded -= OnCategoryAdded;
            _categoryStore.CategoryUpdated -= OnCategoryUpdated;
            _categoryStore.CategoryDeleted -= OnCategoryDeleted;

            base.ViewDestroy(viewFinishing);
        }

        #endregion

        private void LoadCategories()
        {
            List<CategoryListDto> result = _categoryStore
                                           .GetCategoryListDtos()
                                           .ToList();

            if (result.Count == 0)
            {
                _logger.LogInformation("No categories found in the store.");
                Categories = [];
                _allCategories.Clear();
                return;
            }

            _allCategories = result
                             .Select(dto => new CategoryListItemViewModel(dto))
                             .ToList();

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                Categories = new MvxObservableCollection<CategoryListItemViewModel>(_allCategories);
            }
            else
            {
                string lowerSearchText = _searchText.ToLowerInvariant();
                List<CategoryListItemViewModel> filtered = _allCategories
                                                           .Where(c => c
                                                                       .Name.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase) ||
                                                                       (c
                                                                        .Description?.ToLowerInvariant()
                                                                        .Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase) ?? false))
                                                           .ToList();
                Categories = new MvxObservableCollection<CategoryListItemViewModel>(filtered);
            }
        }

        #region Properties

        public MvxObservableCollection<CategoryListItemViewModel> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
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

        private IEnumerable<CategoryListItemViewModel> SelectedCategories => Categories?.Where(c => c.IsSelected) ?? [];

        #endregion

        #region Store Event Handlers

        private void OnCategoryAdded( Category category )
        {
            int categoryId = category.CategoryId;
            try
            {
                CategoryListDto dto = category.ToListDto(_categoryStore.GetCategoryName(categoryId));
                CategoryListItemViewModel vm = new(dto);
                _allCategories.Add(vm);
                ApplyFilter();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error reacting to CategoryAdded for {CategoryId}", category.CategoryId);
            }
        }

        private void OnCategoryUpdated( Category category )
        {
            int categoryId = category.CategoryId;
            try
            {
                CategoryListItemViewModel? existingVm = _allCategories.FirstOrDefault(c => c.CategoryId == category.CategoryId);
                if (existingVm == null) return;

                // Update the existing ViewModel's properties
                CategoryListDto updatedDto = category.ToListDto(_categoryStore.GetCategoryName(categoryId));
                CategoryListItemViewModel updatedVm = new(updatedDto);

                int index = _allCategories.IndexOf(existingVm);
                _allCategories[index] = updatedVm;

                ApplyFilter();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error reacting to CategoryUpdated for {CategoryId}", category.CategoryId);

            }
        }

        private void OnCategoryDeleted( int categoryId )
        {
            try
            {
                _allCategories.RemoveAll(c => c.CategoryId == categoryId);
                ApplyFilter();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error reacting to CategoryDeleted for {CategoryId}", categoryId);

            }
        }

        #endregion

        #region Commands

        public IMvxCommand<int> OpenCategoryFormCommand { get; }
        public IMvxCommand<int> OpenCategoryDeleteCommand { get; }

        private void ExecuteCategoryDelete( int categoryId )
        {
            _modalNavigationControl.PopUp<ProductDeleteViewModel>(categoryId);
        }

        private void ExecuteCategoryForm( int categoryId )
        {
            _modalNavigationControl.PopUp<ProductFormViewModel>(categoryId);
        }

        #endregion
    }
}
