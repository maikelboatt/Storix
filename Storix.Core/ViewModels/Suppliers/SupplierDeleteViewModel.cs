using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Core.Control;

namespace Storix.Core.ViewModels.Suppliers
{
    /// <summary>
    /// ViewModel for supplier deletion confirmation dialog.
    /// Displays full supplier details and handles the deletion operation.
    /// </summary>
    public class SupplierDeleteViewModel:MvxViewModel<int>
    {
        private readonly ISupplierService _supplierService;
        private readonly ISupplierCacheReadService _supplierCacheReadService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<SupplierDeleteViewModel> _logger;

        private SupplierDto? _supplier;
        private bool _isLoading;
        private int _supplierId;

        public SupplierDeleteViewModel(
            ISupplierService supplierService,
            ISupplierCacheReadService supplierCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<SupplierDeleteViewModel> logger )
        {
            _supplierService = supplierService;
            _supplierCacheReadService = supplierCacheReadService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Initialize commands
            DeleteCommand = new MvxAsyncCommand(ExecuteDeleteCommandAsync, () => CanDelete);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
        }

        private async Task ExecuteDeleteCommandAsync()
        {
            if (Supplier == null)
            {
                _logger.LogWarning("⚠️ Delete command executed but Supplier is null. Aborting deletion.");
                return;
            }

            IsLoading = true;

            try
            {
                _logger.LogInformation("🗑️ Deleting supplier:{SupplierId} - {SupplierName}", _supplierId, Supplier?.Name);

                await _supplierService.SoftDeleteSupplierAsync(_supplierId);
                _logger.LogInformation(
                    "✅ Successfully deleted supplier: {SupplierId} - {SupplierName}",
                    Supplier?.SupplierId,
                    Supplier?.Name);

                _modalNavigationControl.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Failed to delete supplier: {SupplierId} - {SupplierName}",
                    Supplier?.SupplierId,
                    Supplier?.Name);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteCancelCommand()
        {
            _logger.LogInformation("❌ Supplier deletion cancelled by user");
            _modalNavigationControl.Close();
        }

        #region LifeCycle Methods

        public override void Prepare( int parameter )
        {
            _supplierId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;

            try
            {
                await LoadSupplierAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading supplier details for deletion. SupplierId: {SupplierId}", _supplierId);
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
        /// The supplier to be deleted with all its details
        /// </summary>
        public SupplierDto? Supplier
        {
            get => _supplier;
            private set => SetProperty(
                ref _supplier,
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
        public bool CanDelete => Supplier != null && !IsLoading;

        /// <summary>
        /// Whether the cancel command can be executed
        /// </summary>
        public bool CanCancel => !IsLoading;

        public bool HasEmail => !string.IsNullOrWhiteSpace(Supplier?.Email);
        public bool HasPhone => !string.IsNullOrWhiteSpace(Supplier?.Phone);
        public bool HasAddress => !string.IsNullOrWhiteSpace(Supplier?.Address);

        #endregion

        #region Methods

        private async Task LoadSupplierAsync()
        {
            _logger.LogDebug("🧩 Loading supplier details for deletion. SupplierId: {SupplierId}", _supplierId);
            SupplierDto? supplier = _supplierCacheReadService.GetSupplierByIdInCache(_supplierId);

            if (supplier == null)
            {
                _logger.LogWarning("⚠️ Supplier with ID {SupplierId} not found in cache.", _supplierId);
            }

            Supplier = supplier;

            if (Supplier != null)
            {
                _logger.LogDebug("✅ Supplier details loaded successfully for SupplierId: {SupplierId}", _supplierId);
            }

            await Task.CompletedTask;
        }

        #endregion
    }
}
