using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Categories;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Core.Control;
using Storix.Core.InputModel;

namespace Storix.Core.ViewModels.Categories
{
    public class CategoryFormViewModel:MvxViewModel<int>, ICategoryViewModel
    {
        private readonly ICategoryService _categoryService;
        private readonly ICategoryCacheReadService _categoryCacheReadService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<CategoryFormViewModel> _logger;
        private CategoryInputModel _categoryInputModel;
        private int _categoryId;
        private bool _isEditMode;
        private bool _isLoading;


        public CategoryFormViewModel( ICategoryService categoryService,
            ICategoryCacheReadService categoryCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<CategoryFormViewModel> logger )
        {
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _categoryCacheReadService = categoryCacheReadService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;
            _isEditMode = false;
            _categoryInputModel = new CategoryInputModel();

            // Initialize Commands
            SaveCommand = new MvxAsyncCommand(ExecuteSaveCommandAsync, () => CanSave);
            ResetCommand = new MvxCommand(ExecuteResetCommand, () => !IsLoading);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
            BrowseImageCommand = new MvxCommand(ExecuteBrowseImageCommand);
        }

        // Commands
        public IMvxCommand ResetCommand { get; }
        public IMvxAsyncCommand SaveCommand { get; }
        public IMvxCommand CancelCommand { get; }
        public IMvxCommand BrowseImageCommand { get; }


        #region LifeCycle methods

        public override void Prepare( int parameter )
        {
            _categoryId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                _logger.LogInformation(
                    "🔄 Initializing CategoryForm. CategoryId: {CategoryId}, Mode: {Create}",
                    _categoryId,
                    _categoryId > 0
                        ? "EDIT"
                        : "CREATE");

                // Load parent categories for dropdown
                LoadParentCategories();

                if (_categoryId > 0)
                {
                    // Editing Mode
                    LoadCategoryFromCache();
                }
                SubscribeToInputModelEvents();
            }
            finally
            {
                IsLoading = false;
            }

            await base.Initialize();
        }

        public override void ViewDestroy( bool viewFinishing = true )
        {
            UnsubscribeFromInputModelEvents();
            base.ViewDestroy(viewFinishing);
        }

        #endregion

        private void LoadParentCategories()
        {
            IEnumerable<CategoryDto> categories = _categoryCacheReadService.GetAllActiveCategoriesInCache();
            Input.ParentCategories.Clear();

            foreach (CategoryDto category in categories)
            {
                // Don't include the current category as a potential parent (for edit mode)
                if (category.CategoryId != _categoryId)
                {
                    Input.ParentCategories.Add(category);
                }
            }

            _logger.LogInformation("Loaded {ParentCategoriesCount} parent categories", Input.ParentCategories.Count);
        }

        private void LoadCategoryFromCache()
        {
            CategoryDto? category = _categoryCacheReadService.GetCategoryByIdInCache(_categoryId);
            if (category != null)
            {
                SetInputModelFromCategory(category);
            }
        }

        private void SetInputModelFromCategory( CategoryDto categoryDto )
        {

            UpdateCategoryDto updateDto = categoryDto.ToUpdateDto();

            // Store the current parent categories collection
            ObservableCollection<CategoryDto> parentCategories = Input.ParentCategories;

            // Create new input model with the DTO
            Input = new CategoryInputModel(updateDto)
            {
                ParentCategories = parentCategories
            };

            IsEditMode = true;
            RaiseAllPropertiesChanged();
        }

        private void ResetForm()
        {
            // Store the current parent categories collection
            ObservableCollection<CategoryDto> parentCategories = Input.ParentCategories;

            // Create fresh input model
            Input = new CategoryInputModel
            {
                ParentCategories = parentCategories
            };

            IsEditMode = false;
            RaiseAllPropertiesChanged();
        }


        #region Command Implementations

        private async Task ExecuteSaveCommandAsync()
        {
            if (!_categoryInputModel.Validate())
                return;

            IsLoading = true;

            try
            {
                if (IsEditMode)
                    await PerformUpdate();
                else
                    await PerformCreate();

                _modalNavigationControl.Close();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PerformUpdate()
        {
            UpdateCategoryDto updateDto = _categoryInputModel.ToUpdateDto();
            await _categoryService.UpdateCategoryAsync(updateDto);
        }

        private async Task PerformCreate()
        {
            CreateCategoryDto createDto = _categoryInputModel.ToCreateDto();
            await _categoryService.CreateCategoryAsync(createDto);
        }

        private void ExecuteCancelCommand() => _modalNavigationControl.Close();

        private void ExecuteResetCommand() => ResetForm();

        private void ExecuteBrowseImageCommand()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All Files|*.*",
                Title = "Select Category Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                Input.ImageUrl = openFileDialog.FileName;
            }
        }

        #endregion

        #region Properties

        public CategoryInputModel Input
        {
            get => _categoryInputModel;
            private set
            {
                if (_categoryInputModel != value)
                {
                    UnsubscribeFromInputModelEvents();
                    SetProperty(ref _categoryInputModel, value);
                    SubscribeToInputModelEvents();
                }
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value, () => RaisePropertyChanged(() => CanSave));
        }
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, () => ResetCommand.RaiseCanExecuteChanged());
        }

        public string Title => IsEditMode
            ? "Edit Category"
            : "Create Category";

        public string SaveButtonText => IsEditMode
            ? "Update"
            : "Create";

        // Validation state
        public bool IsValid => _categoryInputModel?.IsValid ?? false;
        public bool HasErrors => _categoryInputModel?.HasErrors ?? false;

        // Command availability
        public bool CanSave => IsValid && !IsLoading;
        public bool CanCancel => !IsLoading;

        #endregion

        #region Event Handling

        private void SubscribeToInputModelEvents()
        {
            if (_categoryInputModel == null!) return;
            _categoryInputModel!.PropertyChanged += OnInputModelPropertyChanged;
            _categoryInputModel!.ErrorsChanged += OnInputModelErrorsChanged;
        }

        private void UnsubscribeFromInputModelEvents()
        {
            if (_categoryInputModel == null!) return;
            _categoryInputModel!.PropertyChanged -= OnInputModelPropertyChanged;
            _categoryInputModel!.ErrorsChanged -= OnInputModelErrorsChanged;
        }

        private void OnInputModelPropertyChanged( object? sender, PropertyChangedEventArgs e )
        {
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);

            // Refresh commands
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void OnInputModelErrorsChanged( object? sender, DataErrorsChangedEventArgs e )
        {
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);

            // Refresh commands
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void RaiseAllPropertiesChanged()
        {
            RaisePropertyChanged(() => Title);
            RaisePropertyChanged(() => SaveButtonText);
            RaisePropertyChanged(() => IsEditMode);
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);
            RaisePropertyChanged(() => CanCancel);
        }

        #endregion
    }
}
