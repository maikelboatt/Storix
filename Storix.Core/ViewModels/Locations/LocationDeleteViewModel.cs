using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Locations;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Core.Control;

namespace Storix.Core.ViewModels.Locations
{
    /// <summary>
    /// Displays full location details and handles the deletion operation.
    /// </summary>
    public class LocationDeleteViewModel:MvxViewModel<int>
    {
        private readonly ILocationService _locationService;
        private readonly ILocationCacheReadService _locationCacheReadService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<LocationDeleteViewModel> _logger;

        private LocationDto? _location;
        private bool _isLoading;
        private int _locationId;

        public LocationDeleteViewModel( ILocationService locationService,
            ILocationCacheReadService locationCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<LocationDeleteViewModel> logger )
        {
            _locationService = locationService;
            _locationCacheReadService = locationCacheReadService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;

            // Initialize commands
            DeleteCommand = new MvxAsyncCommand(ExecuteDeleteCommandAsync, () => CanDelete);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);

        }

        #region Lifecycle Methods

        public override void Prepare( int parameter )
        {
            _locationId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;

            try
            {
                await LoadLocationAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load location with ID: {LocationId}", _locationId);
                // Optionally show error message to user
                _modalNavigationControl.Close();
            }
            finally
            {
                IsLoading = false;
            }

            await base.Initialize();
        }

        private async Task LoadLocationAsync()
        {
            _logger.LogInformation("🔄 Loading location details for LocationId: {LocationId}", _locationId);
            LocationDto? location = _locationCacheReadService.GetLocationByIdInCache(_locationId);

            if (location == null)
            {
                _logger.LogWarning("⚠️ Location with ID: {LocationId} not found in cache. Fetching from service.", _locationId);
            }

            Location = location;

            if (Location != null)
            {
                _logger.LogInformation(
                    "✅ Successfully loaded location: {LocationId} - {LocationName}",
                    Location.LocationId,
                    Location.Name);
            }

            await Task.CompletedTask;
        }

        #endregion

        #region Commands and Implementations

        public IMvxCommand DeleteCommand { get; }
        public IMvxCommand CancelCommand { get; }

        private async Task ExecuteDeleteCommandAsync()
        {
            if (Location == null)
            {
                _logger.LogWarning("⚠️ Delete command executed but Location is null. Aborting deletion.");
                return;
            }

            IsLoading = true;

            try
            {
                _logger.LogInformation("🗑️ Deleting location:{LocationId} - {LocationName}", _locationId, Location?.Name);

                await _locationService.SoftDeleteLocationAsync(_locationId);
                _logger.LogInformation(
                    "✅ Successfully deleted location: {LocationId} - {LocationName}",
                    Location?.LocationId,
                    Location?.Name);

                _modalNavigationControl.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Failed to delete location: {LocationId} - {LocationName}",
                    Location?.LocationId,
                    Location?.Name);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteCancelCommand() => _modalNavigationControl.Close();

        #endregion

        #region Properties

        /// <summary>
        /// The location to be deleted with all its details
        /// </summary>
        public LocationDto? Location
        {
            get => _location;
            private set => SetProperty(
                ref _location,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanDelete);
                    RaisePropertyChanged(() => HasDescription);
                    RaisePropertyChanged(() => HasType);
                    RaisePropertyChanged(() => HasAddress);
                    DeleteCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Indicates whether a deletion operation is in progress
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(
                ref _isLoading,
                value,
                () =>
                {
                    RaisePropertyChanged(() => CanDelete);
                    RaisePropertyChanged(() => CanCancel);
                    DeleteCommand.RaiseCanExecuteChanged();
                    CancelCommand.RaiseCanExecuteChanged();
                });
        }

        /// <summary>
        /// Whether the delete command can be executed
        /// </summary>
        public bool CanDelete => Location != null && !IsLoading;

        /// <summary>
        /// Whether the cancel command can be executed
        /// </summary>
        public bool CanCancel => !IsLoading;

        public bool HasDescription => !string.IsNullOrWhiteSpace(Location?.Description);
        public bool HasType => !string.IsNullOrWhiteSpace(Location?.Type.ToString());
        public bool HasAddress => !string.IsNullOrWhiteSpace(Location?.Address);

        #endregion
    }
}
