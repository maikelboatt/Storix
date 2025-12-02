using System.ComponentModel;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Customers;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Core.Control;
using Storix.Core.InputModel;

namespace Storix.Core.ViewModels.Customers
{
    public class CustomerFormViewModel:MvxViewModel<int>
    {
        private readonly ICustomerService _customerService;
        private readonly ICustomerCacheReadService _customerCacheReadService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<CustomerFormViewModel> _logger;
        private CustomerInputModel _customerInputModel;
        private int _customerId;
        private bool _isEditMode;
        private bool _isLoading;

        public CustomerFormViewModel(
            ICustomerService customerService,
            ICustomerCacheReadService customerCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<CustomerFormViewModel> logger )
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _customerCacheReadService = customerCacheReadService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;
            _isEditMode = false;
            _customerInputModel = new CustomerInputModel();

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
            _customerId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                _logger.LogInformation(
                    "🔄 Initializing CustomerForm. CustomerId: {CustomerId}, Mode: {Mode}",
                    _customerId,
                    _customerId > 0
                        ? "EDIT"
                        : "CREATE");

                if (_customerId > 0)
                {
                    // Editing Mode
                    LoadCustomerFromCache();
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

        private void LoadCustomerFromCache()
        {
            CustomerDto? customer = _customerCacheReadService.GetCustomerByIdInCache(_customerId);
            if (customer != null)
            {
                SetInputModelFromCustomer(customer);
            }
        }

        private void SetInputModelFromCustomer( CustomerDto customerDto )
        {
            UpdateCustomerDto updateDto = customerDto.ToUpdateDto();

            // Create new input model with the DTO
            Input = new CustomerInputModel(updateDto);

            IsEditMode = true;
            RaiseAllPropertiesChanged();
        }

        private void ResetForm()
        {
            // Create fresh input model
            Input = new CustomerInputModel();

            IsEditMode = false;
            RaiseAllPropertiesChanged();
        }

        #region Command Implementations

        private async Task ExecuteSaveCommandAsync()
        {
            if (!_customerInputModel.Validate())
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
            UpdateCustomerDto updateDto = _customerInputModel.ToUpdateDto();
            await _customerService.UpdateCustomerAsync(updateDto);
        }

        private async Task PerformCreate()
        {
            CreateCustomerDto createDto = _customerInputModel.ToCreateDto();
            await _customerService.CreateCustomerAsync(createDto);
        }

        private void ExecuteCancelCommand() => _modalNavigationControl.Close();

        private void ExecuteResetCommand() => ResetForm();

        #endregion

        #region Properties

        public CustomerInputModel Input
        {
            get => _customerInputModel;
            private set
            {
                if (_customerInputModel != value)
                {
                    UnsubscribeFromInputModelEvents();
                    SetProperty(ref _customerInputModel, value);
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
            ? "Edit Customer"
            : "Create Customer";

        public string SaveButtonText => IsEditMode
            ? "Update"
            : "Create";

        // Validation state
        public bool IsValid => _customerInputModel?.IsValid ?? false;
        public bool HasErrors => _customerInputModel?.HasErrors ?? false;

        // Command availability
        public bool CanSave => IsValid && !IsLoading;
        public bool CanCancel => !IsLoading;

        #endregion

        #region Event Handling

        private void SubscribeToInputModelEvents()
        {
            if (_customerInputModel == null!) return;
            _customerInputModel!.PropertyChanged += OnInputModelPropertyChanged;
            _customerInputModel!.ErrorsChanged += OnInputModelErrorsChanged;
        }

        private void UnsubscribeFromInputModelEvents()
        {
            if (_customerInputModel == null!) return;
            _customerInputModel!.PropertyChanged -= OnInputModelPropertyChanged;
            _customerInputModel!.ErrorsChanged -= OnInputModelErrorsChanged;
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
