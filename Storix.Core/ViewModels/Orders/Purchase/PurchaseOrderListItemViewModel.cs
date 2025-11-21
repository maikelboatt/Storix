using MvvmCross.ViewModels;
using Storix.Application.DTO.Orders;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Orders.Purchase
{
    /// <summary>
    /// ViewModel for a single purchase order item in the list
    /// </summary>
    public class PurchaseOrderListItemViewModel:MvxNotifyPropertyChanged
    {
        private int _orderId;
        private OrderStatus _status;
        private string _supplierName = string.Empty;
        private DateTime _orderDate;
        private DateTime? _deliveryDate;
        private string? _notes;
        private int _createdBy;
        private decimal _totalAmount;
        private bool _isSelected;

        public PurchaseOrderListItemViewModel( PurchaseOrderListDto dto )
        {
            UpdateFromDto(dto);
        }

        public void UpdateFromDto( PurchaseOrderListDto dto )
        {
            OrderId = dto.OrderId;
            Status = dto.Status;
            SupplierName = dto.SupplierName;
            OrderDate = dto.OrderDate;
            DeliveryDate = dto.DeliveryDate;
            Notes = dto.Notes;
            CreatedBy = dto.CreatedBy;
            TotalAmount = dto.TotalAmount;
        }

        public int OrderId
        {
            get => _orderId;
            set => SetProperty(ref _orderId, value);
        }

        public OrderStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string SupplierName
        {
            get => _supplierName;
            set => SetProperty(ref _supplierName, value);
        }

        public DateTime OrderDate
        {
            get => _orderDate;
            set => SetProperty(ref _orderDate, value);
        }

        public DateTime? DeliveryDate
        {
            get => _deliveryDate;
            set => SetProperty(ref _deliveryDate, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public int CreatedBy
        {
            get => _createdBy;
            set => SetProperty(ref _createdBy, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string StatusDisplay => Status.ToString();
        public string TotalAmountDisplay => TotalAmount.ToString("C2");
        public string OrderDateDisplay => OrderDate.ToString("yyyy-MM-dd");
        public string DeliveryDateDisplay => DeliveryDate?.ToString("yyyy-MM-dd") ?? "N/A";
    }
}
