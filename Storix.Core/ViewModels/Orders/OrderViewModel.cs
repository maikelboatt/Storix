using System.ComponentModel;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Orders;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Validator.Orders;
using Storix.Core.InputModel;

namespace Storix.Core.ViewModels.Orders
{
    public class OrderViewModel:MvxViewModel<int>, IOrderViewModel
    {
        private readonly IOrderService _orderService;
        private bool _isEditMode;
        private bool _isLoading;
        private int _orderId;
        private OrderInputModel _orderInputModel;


        public OrderViewModel( IOrderService orderService )
        {
            _orderService = orderService;
            _isEditMode = false;

            // Initialize Commands
            SaveCommand = new MvxAsyncCommand(ExecuteSaveCommandAsync, () => CanSave);
            ResetCommand = new MvxCommand(ExecuteResetCommand, () => !_isLoading);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
        }

        public override void Prepare( int parameter )
        {
            _orderId = parameter;
        }

        public override Task Initialize()
        {
            if (_orderId > 0)
                // Load existing order for editing (Editing Mode)
                LoadOrderById();
            else
                Input = new OrderInputModel();
            return base.Initialize();
        }

        public override void ViewDestroy( bool viewFinishing = true )
        {
            UnsubscribeFromInputModelEvents();
            base.ViewDestroy(viewFinishing);
        }

        private void LoadOrderById()
        {
            _isLoading = true;
            try
            {
                OrderDto? order = _orderService.GetOrderById(_orderId);
                if (order != null)
                {
                    SetInputModelFromOrder(order);
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void SetInputModelFromOrder( OrderDto order )
        {
            Input = new OrderInputModel(order.ToUpdateDto());
            _isEditMode = true;
            RaiseAllPropertiesChanged();
        }

        private void ResetForm()
        {
            Input = new OrderInputModel();
            _isEditMode = false;
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
            if (!_orderInputModel.Validate())
                return;

            IsLoading = true;

            try
            {
                if (IsEditMode)
                {
                    await PerformUpdate();
                }
                else
                {
                    await PerformCreate();
                }

                // Close modal and notify success
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PerformCreate()
        {
            CreateOrderDto createDto = _orderInputModel.ToCreateDto();
            await _orderService.CreateOrderAsync(createDto);
        }

        private async Task PerformUpdate()
        {
            UpdateOrderDto updateDto = _orderInputModel.ToUpdateDto();
            await _orderService.UpdateOrderAsync(updateDto);
        }

        private void ExecuteCancelCommand()
        {
            throw new NotImplementedException();
        }

        private void ExecuteResetCommand()
        {
            ResetForm();
        }

        #endregion

        #region Properties

        public OrderInputModel Input
        {
            get => _orderInputModel;
            private set
            {
                if (_orderInputModel == value) return;

                UnsubscribeFromInputModelEvents();
                _orderInputModel = value;
                SubscribeToInputModelEvents();
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            private set => SetProperty(ref _isEditMode, value, () => RaisePropertyChanged(() => CanSave));
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, ResetCommand.RaiseCanExecuteChanged);
        }

        public string Title => IsEditMode
            ? "Edit Category"
            : "Create Category";

        public string SaveButtonText => IsEditMode
            ? "Update"
            : "Create";

        // Validation state
        public bool IsValid => _orderInputModel?.IsValid ?? false;
        public bool HasErrors => _orderInputModel?.HasErrors ?? false;

        // Command availability
        public bool CanCancel => !IsLoading;

        public bool CanSave => IsValid && !IsLoading;

        #endregion

        #region Event Handling

        private void SubscribeToInputModelEvents()
        {
            if (_orderInputModel == null) return;
            _orderInputModel.PropertyChanged += OnInputModelPropertyChanged;
            _orderInputModel.ErrorsChanged += OnInputModelErrorsChanged;
        }

        private void UnsubscribeFromInputModelEvents()
        {
            if (_orderInputModel == null) return;
            _orderInputModel.PropertyChanged -= OnInputModelPropertyChanged;
            _orderInputModel.ErrorsChanged -= OnInputModelErrorsChanged;
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
            RaisePropertyChanged(() => _isEditMode);
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);
            RaisePropertyChanged(() => CanCancel);
        }

        #endregion
    }
}
