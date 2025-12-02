using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Customers;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Core.Control;

namespace Storix.Core.ViewModels.Customers
{
    /// <summary>
    /// ViewModel for customer deletion confirmation dialog.
    /// Displays full customer details and handles the deletion operation.
    /// </summary>
    public class CustomerDeleteViewModel:MvxViewModel<int>
    {
        private readonly ICustomerService _customerService;
        private readonly ICustomerCacheReadService _customerCacheReadService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<CustomerDeleteViewModel> _logger;

        private CustomerDto? _customer;
        private bool _isLoading;
        private int _customerId;

        public CustomerDeleteViewModel(
            ICustomerService customerService,
            ICustomerCacheReadService customerCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<CustomerDeleteViewModel> logger )
        {
            _customerService = customerService;
            _customerCacheReadService = customerCacheReadService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Initialize commands
            DeleteCommand = new MvxAsyncCommand(ExecuteDeleteCommandAsync, () => CanDelete);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
        }

        private async Task ExecuteDeleteCommandAsync()
        {
            if (Customer == null)
            {
                _logger.LogWarning("⚠️ Delete command executed but Customer is null. Aborting deletion.");
                return;
            }

            IsLoading = true;

            try
            {
                _logger.LogInformation("🗑️ Deleting customer:{CustomerId} - {CustomerName}", _customerId, Customer?.Name);

                await _customerService.SoftDeleteCustomerAsync(_customerId);
                _logger.LogInformation(
                    "✅ Successfully deleted customer: {CustomerId} - {CustomerName}",
                    Customer?.CustomerId,
                    Customer?.Name);

                _modalNavigationControl.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Failed to delete customer: {CustomerId} - {CustomerName}",
                    Customer?.CustomerId,
                    Customer?.Name);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteCancelCommand()
        {
            _logger.LogInformation("❌ Customer deletion cancelled by user");
            _modalNavigationControl.Close();
        }

        #region LifeCycle Methods

        public override void Prepare( int parameter )
        {
            _customerId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;

            try
            {
                await LoadCustomerAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading customer details for deletion. CustomerId: {CustomerId}", _customerId);
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

        /// <summary>
        /// The customer to be deleted with all its details
        /// </summary>
        public CustomerDto? Customer
        {
            get => _customer;
            private set => SetProperty(
                ref _customer,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanDelete);
                    RaisePropertyChanged(() => HasEmail);
                    RaisePropertyChanged(() => HasPhone);
                    RaisePropertyChanged(() => HasAddress);
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
        public bool CanDelete => Customer != null && !IsLoading;

        /// <summary>
        /// Whether the cancel command can be executed
        /// </summary>
        public bool CanCancel => !IsLoading;

        public bool HasEmail => !string.IsNullOrWhiteSpace(Customer?.Email);
        public bool HasPhone => !string.IsNullOrWhiteSpace(Customer?.Phone);
        public bool HasAddress => !string.IsNullOrWhiteSpace(Customer?.Address);

        #endregion

        #region Methods

        private async Task LoadCustomerAsync()
        {
            _logger.LogDebug("🧩 Loading customer details for deletion. CustomerId: {CustomerId}", _customerId);
            CustomerDto? customer = _customerCacheReadService.GetCustomerByIdInCache(_customerId);

            if (customer == null)
            {
                _logger.LogWarning("⚠️ Customer with ID {CustomerId} not found in cache.", _customerId);
            }

            Customer = customer;

            if (Customer != null)
            {
                _logger.LogDebug("✅ Customer details loaded successfully for CustomerId: {CustomerId}", _customerId);
            }

            await Task.CompletedTask;
        }

        #endregion
    }
}
