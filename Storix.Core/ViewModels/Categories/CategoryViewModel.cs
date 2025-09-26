using System.ComponentModel;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Categories;
using Storix.Application.Services.Categories.Interfaces;
using Storix.Core.InputModel;

namespace Storix.Core.ViewModels.Categories
{
    public class CategoryViewModel:MvxViewModel<int>, ICategoryViewModel
    {
        private readonly ICategoryService _categoryService;

        private int _categoryId;
        private CategoryInputModel _categoryInputModel;
        private bool _isEditMode;
        private bool _isLoading;


        public CategoryViewModel( ICategoryService categoryService )
        {
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _isEditMode = false;

            // Initialize Commands
            SaveCommand = new MvxAsyncCommand(ExecuteSaveCommandAsync, () => CanSave);
            ResetCommand = new MvxCommand(ExecuteResetCommand, () => !IsLoading);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
        }

        public override void Prepare( int parameter )
        {
            _categoryId = parameter;
        }

        public override Task Initialize()
        {
            if (_categoryId > 0)
                // Load existing category for editing (Editing Mode)
                LoadCategoryById();
            else
                Input = new CategoryInputModel();
            SubscribeToInputModelEvents();

            return base.Initialize();
        }

        public override void ViewDestroy( bool viewFinishing = true )
        {
            UnsubscribeFromInputModelEvents();
            base.ViewDestroy(viewFinishing);
        }

        private void LoadCategoryById()
        {
            IsLoading = true;
            try
            {
                CategoryDto? category = _categoryService.GetCategoryById(_categoryId);
                if (category != null)
                {
                    SetInputModelFromCategory(category);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SetInputModelFromCategory( CategoryDto categoryDto )
        {
            Input = new CategoryInputModel(categoryDto.ToUpdateDto());
            IsEditMode = true;
            RaiseAllPropertiesChanged();
        }

        private void ResetForm()
        {
            Input = new CategoryInputModel();
            IsEditMode = false;
            RaiseAllPropertiesChanged();
        }

        #region Commands

        public IMvxCommand ResetCommand { get; }
        public IMvxAsyncCommand SaveCommand { get; }
        public IMvxCommand CancelCommand { get; }

        #endregion

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
                // Logic to close modal and notify success
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

        private void ExecuteCancelCommand()
        {
            // Close Modal
        }

        private void ExecuteResetCommand()
        {
            ResetForm();
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

        public string Title => IsEditMode ? "Edit Category" : "Create Category";
        public string SaveButtonText => IsEditMode ? "Update" : "Create";

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
            if (_categoryInputModel == null) return;
            _categoryInputModel.PropertyChanged += OnInputModelPropertyChanged;
            _categoryInputModel.ErrorsChanged += OnInputModelErrorsChanged;
        }

        private void UnsubscribeFromInputModelEvents()
        {
            if (_categoryInputModel == null) return;
            _categoryInputModel.PropertyChanged -= OnInputModelPropertyChanged;
            _categoryInputModel.ErrorsChanged -= OnInputModelErrorsChanged;
        }

        private void OnInputModelPropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);

            // Refresh commands
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void OnInputModelErrorsChanged( object sender, DataErrorsChangedEventArgs e )
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
