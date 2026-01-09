using MvvmCross.ViewModels;

namespace Storix.Application.DTO.Products
{
    /// <summary>
    /// DTO for stock information at a specific location
    /// </summary>
    public class StockLocationDto:MvxNotifyPropertyChanged
    {
        private int _locationId;
        private string? _locationName;
        private int _currentStock;
        private int _availableStock;
        private int _reservedStock;
        private bool _isLowStock;

        public int LocationId
        {
            get => _locationId;
            set => SetProperty(ref _locationId, value);
        }

        public string? LocationName
        {
            get => _locationName;
            set => SetProperty(ref _locationName, value);
        }

        public int CurrentStock
        {
            get => _currentStock;
            set => SetProperty(ref _currentStock, value, () => { RaisePropertyChanged(() => IsInStock); });
        }

        public int AvailableStock
        {
            get => _availableStock;
            set => SetProperty(ref _availableStock, value);
        }

        public int ReservedStock
        {
            get => _reservedStock;
            set => SetProperty(ref _reservedStock, value);
        }

        public bool IsLowStock
        {
            get => _isLowStock;
            set => SetProperty(ref _isLowStock, value);
        }

        public bool IsInStock => CurrentStock > 0;
    }
}
