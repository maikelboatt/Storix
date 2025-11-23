using MvvmCross.ViewModels;
using Storix.Application.DTO.Products;

namespace Storix.Core.ViewModels.Orders
{
    /// <summary>
    /// Represents a single row in the order items grid
    /// </summary>
    public class OrderItemRowViewModel:MvxNotifyPropertyChanged
    {
        private readonly List<ProductDto> _availableProducts;

        private int? _orderItemId;
        private int _productId;
        private string? _productName;
        private int _quantity;
        private decimal _unitPrice;

        public OrderItemRowViewModel( List<ProductDto> availableProducts )
        {
            _availableProducts = availableProducts;
        }

        // Event fired when product is selected
        public event EventHandler<int>? ProductSelected;

        public int? OrderItemId
        {
            get => _orderItemId;
            set => SetProperty(ref _orderItemId, value);
        }

        public int ProductId
        {
            get => _productId;
            set
            {
                if (SetProperty(ref _productId, value))
                {
                    // Update product name
                    ProductDto? product = _availableProducts.FirstOrDefault(p => p.ProductId == value);
                    if (product != null)
                    {
                        ProductName = product.Name;
                        // Notify that product was selected
                        ProductSelected?.Invoke(this, value);
                    }
                }
            }
        }

        public string? ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    RaisePropertyChanged(() => LineTotal);
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    RaisePropertyChanged(() => LineTotal);
                }
            }
        }

        public decimal LineTotal => Quantity * UnitPrice;
    }
}
