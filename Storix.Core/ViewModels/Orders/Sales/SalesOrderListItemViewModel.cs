using MvvmCross.ViewModels;
using Storix.Application.DTO.Orders;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Orders.Sales
{
    public class SalesOrderListItemViewModel( SalesOrderListDto orderDto ):MvxNotifyPropertyChanged
    {
        private readonly SalesOrderListDto _orderListDto = orderDto ?? throw new ArgumentNullException(nameof(orderDto));
        private bool _isSelected;

        #region Sales Order Properties

        public int OrderId => _orderListDto.OrderId;
        public string CustomerName => _orderListDto.CustomerName;
        public DateTime OrderDate => _orderListDto.OrderDate;
        public OrderStatus Status => _orderListDto.Status;
        public decimal TotalAmount => _orderListDto.TotalAmount;
        public DateTime? DeliveryDate => _orderListDto.DeliveryDate;
        public string? Notes => _orderListDto.Notes;
        public int CreatedBy => _orderListDto.CreatedBy;

        #endregion

        #region UI_Specific Properties

        /// <summary>
        /// Indicates whether this product is currently selected in the UI.
        /// Used for checkbox binding in DataGrid.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the underlying Sales Order entity (useful for edit/delete operations).
        /// </summary>
        public SalesOrderListDto GetSalesOrder() => _orderListDto;

        /// <summary>
        /// Resets the selection state.
        /// </summary>
        public void ClearSelection()
        {
            IsSelected = false;
        }

        #endregion
    }
}
