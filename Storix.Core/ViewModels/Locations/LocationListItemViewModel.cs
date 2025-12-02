using MvvmCross.ViewModels;
using Storix.Application.DTO.Locations;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Locations
{
    public class LocationListItemViewModel( LocationDto location ):MvxNotifyPropertyChanged
    {
        private readonly LocationDto _location = location ?? throw new ArgumentNullException(nameof(location));

        private bool _isSelected;


        #region Location Properties

        public int LocationId => location.LocationId;
        public string Name => location.Name;
        public string? Description => location.Description;
        public LocationType Type => location.Type;
        public string? Address => location.Address;

        #endregion

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public LocationDto GetLocation() => _location;

        public void ClearSelection()
        {
            IsSelected = false;
        }
    }
}
