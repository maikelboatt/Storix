using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Locations;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Application.Stores.Locations;
using Storix.Core.Control;
using Storix.Domain.Models;

namespace Storix.Core.ViewModels.Locations
{
    public class LocationListViewModel:MvxViewModel
    {
        private readonly ILocationService _locationService;
        private readonly ILocationStore _locationStore;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<LocationListViewModel> _logger;

        private MvxObservableCollection<LocationListItemViewModel> _locations = [];
        private List<LocationListItemViewModel> _allLocations = [];
        private string _searchText = string.Empty;
        private bool _isLoading;


        public LocationListViewModel( ILocationService locationService,
            ILocationStore locationStore,
            IModalNavigationControl modalNavigationControl,
            ILogger<LocationListViewModel> logger )
        {
            _locationService = locationService;
            _locationStore = locationStore;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Subscribe to write operation events from the store
            _locationStore.LocationAdded += OnLocationAdded;
            _locationStore.LocationUpdated += OnLocationUpdated;
            _locationStore.LocationDeleted += OnLocationDeleted;


            // Initialise commands
            OpenLocationFormCommand = new MvxCommand<int>(ExecuteLocationForm);
            OpenLocationDeleteCommand = new MvxCommand<int>(ExecuteLocationDelete);
        }

        #region ViewModel LifeCycle

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                await LoadLocations();
            }
            finally
            {
                IsLoading = false;
            }
            await base.Initialize();
        }

        public override void ViewDestroy( bool viewFinishing = true )
        {
            // Unsubscribe from store events to prevent memory leaks
            _locationStore.LocationAdded -= OnLocationAdded;
            _locationStore.LocationUpdated -= OnLocationUpdated;
            _locationStore.LocationDeleted -= OnLocationDeleted;

            base.ViewDestroy(viewFinishing);
        }

        #endregion

        private async Task LoadLocations()
        {
            DatabaseResult<IEnumerable<LocationDto>> result = await _locationService.GetAllActiveLocationsAsync();

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogError("Failed to load locations: {ErrorMessage}", result.ErrorMessage);
                Locations = [];
                _allLocations.Clear();
                return;
            }

            _allLocations = result
                            .Value
                            .Select(locationDto => new LocationListItemViewModel(locationDto))
                            .ToList();

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                Locations = new MvxObservableCollection<LocationListItemViewModel>(_allLocations);
            }
            else
            {
                string lowerSearchText = _searchText.ToLowerInvariant();
                List<LocationListItemViewModel> filtered = _allLocations
                                                           .Where(c => c
                                                                       .Name.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase) ||
                                                                       (c
                                                                        .Address?.ToLowerInvariant()
                                                                        .Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase)
                                                                        ?? false))
                                                           .ToList();
                Locations = new MvxObservableCollection<LocationListItemViewModel>(filtered);
            }
        }

        #region Properties

        public MvxObservableCollection<LocationListItemViewModel> Locations
        {
            get => _locations;
            set => SetProperty(ref _locations, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public IMvxCommand<int> OpenLocationFormCommand { get; }
        public IMvxCommand<int> OpenLocationDeleteCommand { get; }

        private void ExecuteLocationDelete( int locationId )
        {
            _modalNavigationControl.PopUp<LocationDeleteViewModel>(locationId);
        }

        private void ExecuteLocationForm( int locationId )
        {
            _modalNavigationControl.PopUp<LocationFormViewModel>(locationId);
        }

        #endregion

        #region Store Event Handlers

        private void OnLocationAdded( Location location )
        {
            try
            {
                LocationDto dto = location.ToDto();

                LocationListItemViewModel vm = new(dto);
                _allLocations.Add(vm);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to LocationAdded for {LocationId}", location.LocationId);
            }
        }

        private void OnLocationUpdated( Location location )
        {
            try
            {
                LocationDto dto = location.ToDto();

                LocationListItemViewModel? existingVm = _allLocations
                    .FirstOrDefault(l => l.LocationId == location.LocationId);
                if (existingVm == null)
                    return;

                LocationListItemViewModel updatedVm = new(dto);

                int index = _allLocations.IndexOf(existingVm);
                _allLocations[index] = updatedVm;

                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to LocationUpdated for {LocationId}", location.LocationId);
            }
        }

        private void OnLocationDeleted( int locationId )
        {
            try
            {
                _allLocations.RemoveAll(l => l.LocationId == locationId);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to LocationDeleted for {LocationId}", locationId);

            }
        }

        #endregion
    }
}
