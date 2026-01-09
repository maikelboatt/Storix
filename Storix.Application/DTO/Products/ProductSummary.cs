using MvvmCross.ViewModels;

namespace Storix.Application.DTO.Products
{
    /// <summary>
    /// DTO for product summary information
    /// </summary>
    public class ProductSummary:MvxNotifyPropertyChanged
    {
        private int _productId;
        private string? _name;
        private string? _sku;
        private decimal _price;
        private int _stock;

        public int ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string? SKU
        {
            get => _sku;
            set => SetProperty(ref _sku, value);
        }

        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        public int Stock
        {
            get => _stock;
            set => SetProperty(ref _stock, value);
        }
    }
}
