using System.ComponentModel;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.OrderItems;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Validator.OrderItems;
using Storix.Core.InputModel;

namespace Storix.Core.ViewModels.OrderItems
{
    public class OrderItemViewModel:MvxViewModel<int>, IOrderItemViewModel
    {
        private readonly IOrderItemService _orderItemService;
        private bool _isEditMode;
        private bool _isLoading;
        private int _orderItemId;
        private OrderItemInputModel _orderItemInputModel;

        public OrderItemViewModel( IOrderItemService orderItemService )
        {
            _orderItemService = orderItemService;
            _isEditMode = false;

            // Initialize Commands
            SaveCommand = new MvxAsyncCommand(ExecuteSaveCommandAsync, () => CanSave);
            ResetCommand = new MvxCommand(ExecuteResetCommand, () => !_isLoading);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);
        }


        public override void Prepare( int parameter )
        {
            _orderItemId = parameter;
        }

        public override Task Initialize()
        {
            if (_orderItemId > 0)
                // Load existing order item for editing (Editing Mode)
                LoadOrderItemById();
            else
                Input = new OrderItemInputModel();
            return base.Initialize();
        }

        public override void ViewDestroy( bool viewFinishing = true )
        {
            UnsubscribeFromInputModelEvents();
            base.ViewDestroy(viewFinishing);
        }


        private void LoadOrderItemById()
        {
            IsLoading = true;
            try
            {
                OrderItemDto? orderItem = _orderItemService.GetOrderItemById(_orderItemId);
                if (orderItem != null)
                {
                    SetInputModelFromOrderItem(orderItem);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SetInputModelFromOrderItem( OrderItemDto orderItemDto )
        {
            IsEditMode = true;
            Input = new OrderItemInputModel(orderItemDto.ToUpdateDto());
            RaiseAllPropertiesChanged();
        }

        private void ResetForm()
        {
            Input = new OrderItemInputModel();
            _isEditMode = false;
            RaiseAllPropertiesChanged();
        }

        #region Commands

        public IMvxCommand ResetCommand { get; }
        public IMvxAsyncCommand SaveCommand { get; }
        public IMvxCommand CancelCommand { get; }

        #endregion

        #region Command Implementations

        private void ExecuteCancelCommand()
        {
            // Implement logic to close modal
            throw new NotImplementedException();
        }

        private void ExecuteResetCommand()
        {
            ResetForm();
        }

        private async Task ExecuteSaveCommandAsync()
        {
            if (!_orderItemInputModel.Validate()) return;
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
            UpdateOrderItemDto updateDto = _orderItemInputModel.ToUpdateDto();
            await _orderItemService.UpdateOrderItemAsync(updateDto);
        }

        private async Task PerformCreate()
        {
            CreateOrderItemDto createDto = _orderItemInputModel.ToCreateDto();
            await _orderItemService.CreateOrderItemAsync(createDto);
        }

        #endregion

        #region Properties

        public OrderItemInputModel Input
        {
            get => _orderItemInputModel;
            set
            {
                if (_orderItemInputModel == value) return;

                UnsubscribeFromInputModelEvents();
                SetProperty(ref _orderItemInputModel, value);
                SubscribeToInputModelEvents();
            }
        }

        public bool IsEditMode
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, () => RaisePropertyChanged(() => CanSave));
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, ResetCommand.RaiseCanExecuteChanged);
        }

        public string Title => IsEditMode
            ? "Edit Order Item"
            : "Create Order Item";

        public string SaveButtonText => IsEditMode
            ? "Update"
            : "Create";

        // Validation state
        public bool IsValid => _orderItemInputModel?.IsValid ?? false;
        public bool HasErrors => _orderItemInputModel?.HasErrors ?? true;

        // Command availability
        public bool CanSave => IsValid && !IsLoading;
        public bool CanCancel => !IsLoading;

        #endregion

        #region Error Handlers

        private void UnsubscribeFromInputModelEvents()
        {
            if (_orderItemInputModel != null!) return;

            _orderItemInputModel!.ErrorsChanged -= OnInputModelErrorsChanged;
            _orderItemInputModel.PropertyChanged -= OnInputModelPropertyChanged;
        }

        private void SubscribeToInputModelEvents()
        {
            if (_orderItemInputModel == null!) return;
            _orderItemInputModel.PropertyChanged += OnInputModelPropertyChanged;
            _orderItemInputModel.ErrorsChanged += OnInputModelErrorsChanged;
        }

        private void OnInputModelErrorsChanged( object? sender, DataErrorsChangedEventArgs e )
        {
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);

            SaveCommand.RaiseCanExecuteChanged();
        }

        private void OnInputModelPropertyChanged( object? sender, PropertyChangedEventArgs e )
        {
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);

            SaveCommand.RaiseCanExecuteChanged();
        }

        public void RaiseAllPropertiesChanged()
        {
            RaisePropertyChanged(() => IsEditMode);
            RaisePropertyChanged(() => IsLoading);
            RaisePropertyChanged(() => Title);
            RaisePropertyChanged(() => SaveButtonText);
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);
            RaisePropertyChanged(() => CanCancel);
        }

        #endregion
    }
}
