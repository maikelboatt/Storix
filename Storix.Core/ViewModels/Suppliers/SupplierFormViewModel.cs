using System.ComponentModel;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Core.Control;
using Storix.Core.InputModel;

namespace Storix.Core.ViewModels.Suppliers
{
    public class SupplierFormViewModel:MvxViewModel<int>
    {
        private readonly ISupplierService _supplierService;
        private readonly ISupplierCacheReadService _supplierCacheReadService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<SupplierFormViewModel> _logger;
        private SupplierInputModel _supplierInputModel;
        private int _supplierId;
        private bool _isEditMode;
        private bool _isLoading;

        public SupplierFormViewModel(
            ISupplierService supplierService,
            ISupplierCacheReadService supplierCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<SupplierFormViewModel> logger )
        {
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _supplierCacheReadService = supplierCacheReadService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;
            _isEditMode = false;
            _supplierInputModel = new SupplierInputModel();

            // Initialize Commands
            SaveCommand = new MvxAsyncCommand(ExecuteSaveCommandAsync, () => CanSave);
            ResetCommand = new MvxCommand(ExecuteResetCommand, () => !IsLoading);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
        }

        // Commands
        public IMvxCommand ResetCommand { get; }
        public IMvxAsyncCommand SaveCommand { get; }
        public IMvxCommand CancelCommand { get; }

        #region Lifecycle methods

        public override void Prepare( int parameter )
        {
            _supplierId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                _logger.LogInformation(
                    "🔄 Initializing SupplierForm. SupplierId: {SupplierId}, Mode: {Mode}",
                    _supplierId,
                    _supplierId > 0
                        ? "EDIT"
                        : "CREATE");

                if (_supplierId > 0)
                {
                    // Editing Mode
                    LoadSupplierFromCache();
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

        private void LoadSupplierFromCache()
        {
            SupplierDto? supplier = _supplierCacheReadService.GetSupplierByIdInCache(_supplierId);
            if (supplier != null)
            {
                SetInputModelFromSupplier(supplier);
            }
        }

        private void SetInputModelFromSupplier( SupplierDto supplierDto )
        {
            UpdateSupplierDto updateDto = supplierDto.ToUpdateDto();

            // Create new input model with the DTO
            Input = new SupplierInputModel(updateDto);

            IsEditMode = true;
            RaiseAllPropertiesChanged();
        }

        private void ResetForm()
        {
            // Create fresh input model
            Input = new SupplierInputModel();

            IsEditMode = false;
            RaiseAllPropertiesChanged();
        }

        #region Command Implementations

        private async Task ExecuteSaveCommandAsync()
        {
            if (!_supplierInputModel.Validate())
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
            UpdateSupplierDto updateDto = _supplierInputModel.ToUpdateDto();
            await _supplierService.UpdateSupplierAsync(updateDto);
        }

        private async Task PerformCreate()
        {
            CreateSupplierDto createDto = _supplierInputModel.ToCreateDto();
            await _supplierService.CreateSupplierAsync(createDto);
        }

        private void ExecuteCancelCommand() => _modalNavigationControl.Close();

        private void ExecuteResetCommand() => ResetForm();

        #endregion

        #region Properties

        public SupplierInputModel Input
        {
            get => _supplierInputModel;
            private set
            {
                if (_supplierInputModel != value)
                {
                    UnsubscribeFromInputModelEvents();
                    SetProperty(ref _supplierInputModel, value);
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
            ? "Edit Supplier"
            : "Create Supplier";

        public string SaveButtonText => IsEditMode
            ? "Update"
            : "Create";

        // Validation state
        public bool IsValid => _supplierInputModel?.IsValid ?? false;
        public bool HasErrors => _supplierInputModel?.HasErrors ?? false;

        // Command availability
        public bool CanSave => IsValid && !IsLoading;
        public bool CanCancel => !IsLoading;

        #endregion

        #region Event Handling

        private void SubscribeToInputModelEvents()
        {
            if (_supplierInputModel == null!) return;
            _supplierInputModel!.PropertyChanged += OnInputModelPropertyChanged;
            _supplierInputModel!.ErrorsChanged += OnInputModelErrorsChanged;
        }

        private void UnsubscribeFromInputModelEvents()
        {
            if (_supplierInputModel == null!) return;
            _supplierInputModel!.PropertyChanged -= OnInputModelPropertyChanged;
            _supplierInputModel!.ErrorsChanged -= OnInputModelErrorsChanged;
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
