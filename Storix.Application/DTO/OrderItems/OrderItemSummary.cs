using MvvmCross.ViewModels;

namespace Storix.Application.DTO.OrderItems
{
    /// <summary>
    /// DTO for order item information in print documents
    /// </summary>
    public class OrderItemSummary:MvxNotifyPropertyChanged
    {
        private int _lineNumber;
        private string? _productName;
        private string? _productSKU;
        private int _quantity;
        private decimal _unitPrice;
        private decimal _lineTotal;

        public int LineNumber
        {
            get => _lineNumber;
            set => SetProperty(ref _lineNumber, value);
        }

        public string? ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public string? ProductSKU
        {
            get => _productSKU;
            set => SetProperty(ref _productSKU, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value, () => { RaisePropertyChanged(() => LineTotal); });
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetProperty(ref _unitPrice, value, () => { RaisePropertyChanged(() => LineTotal); });
        }

        public decimal LineTotal => Quantity * UnitPrice;
    }
}
