using MvvmCross.ViewModels;

namespace Storix.Core.ViewModels.Orders.Fulfillment
{
    /// <summary>
    /// ViewModel for each item in the fulfillment list
    /// </summary>
    public class FulfillmentItemViewModel:MvxNotifyPropertyChanged
    {
        private int _quantityToFulfill;

        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int OrderedQuantity { get; set; }

        public int QuantityToFulfill
        {
            get => _quantityToFulfill;
            set
            {
                // Validate: Can't fulfill more than ordered
                if (value > OrderedQuantity)
                    value = OrderedQuantity;

                // Validate: Can't be negative
                if (value < 0)
                    value = 0;

                SetProperty(ref _quantityToFulfill, value);
                RaisePropertyChanged(() => LineTotal);
                RaisePropertyChanged(() => HasDiscrepancy);
            }
        }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal => QuantityToFulfill * UnitPrice;

        public bool HasDiscrepancy => QuantityToFulfill != OrderedQuantity;
    }
}
