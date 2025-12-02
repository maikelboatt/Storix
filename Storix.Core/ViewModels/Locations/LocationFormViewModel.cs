using System.ComponentModel;
using Microsoft.Extensions.Logging;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Storix.Application.DTO.Locations;
using Storix.Application.Services.Locations.Interfaces;
using Storix.Core.Control;
using Storix.Core.InputModel;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Locations
{
    /// <summary>
    /// Contains shared logic for location creation and editing using InputModel pattern
    /// </summary>
    public class LocationFormViewModel:MvxViewModel<int>
    {
        private readonly ILocationService _locationService;
        private readonly ILocationCacheReadService _locationCacheReadService;
        private readonly IModalNavigationControl _modalNavigationControl;
        private readonly ILogger<LocationFormViewModel> _logger;

        private LocationInputModel _locationInputModel;
        private int _locationId;
        private bool _isEditMode;
        private bool _isLoading;

        public LocationFormViewModel( ILocationService locationService,
            ILocationCacheReadService locationCacheReadService,
            IModalNavigationControl modalNavigationControl,
            ILogger<LocationFormViewModel> logger )
        {
            _locationService = locationService;
            _locationCacheReadService = locationCacheReadService;
            _modalNavigationControl = modalNavigationControl;
            _logger = logger;
            _isEditMode = false;
            _locationInputModel = new LocationInputModel();

            // Initialize Commands
            SaveCommand = new MvxAsyncCommand(ExecuteSaveCommandAsync, () => CanSave);
            ResetCommand = new MvxCommand(ExecuteResetCommand, () => !IsLoading);
            CancelCommand = new MvxCommand(ExecuteCancelCommand, () => CanCancel);

        }

        #region Lifecycle methods

        public override void Prepare( int parameter )
        {
            _locationId = parameter;
        }

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                _logger.LogInformation(
                    "🔄 Initializing LocationForm. LocationId: {LocationId}, Mode: {Mode}",
                    _locationId,
                    _locationId > 0
                        ? "EDIT"
                        : "CREATE");

                if (_locationId > 0)
                {
                    // Editing Mode
                    LoadLocationFromCache();
                }
                SubscribeToInputModelEvents();
            }
            finally
            {
                IsLoading = false;
            }

            await base.Initialize();
        }


        public override void ViewDestroy( bool viewFinishing = true )
        {
            UnsubscribeFromInputModelEvents();
            base.ViewDestroy(viewFinishing);
        }

        #endregion

        private void LoadLocationFromCache()
        {
            LocationDto? location = _locationCacheReadService.GetLocationByIdInCache(_locationId);

            if (location is not null)
            {
                SetInputModelFromLocation(location);
            }
        }

        private void SetInputModelFromLocation( LocationDto locationDto )
        {
            UpdateLocationDto updateDto = locationDto.ToUpdateDto();

            // Populate the Input Model
            Input = new LocationInputModel(updateDto);

            IsEditMode = true;
            RaiseAllPropertiesChanged();
        }

        private void ResetForm()
        {
            // Create a fresh input model
            Input = new LocationInputModel();
            IsEditMode = false;
            RaiseAllPropertiesChanged();
        }

        #region Commands and Implementation

        public IMvxAsyncCommand SaveCommand { get; }
        public IMvxCommand ResetCommand { get; }
        public IMvxCommand CancelCommand { get; }

        private async Task ExecuteSaveCommandAsync()
        {
            if (!_locationInputModel.Validate())
                return;

            IsLoading = true;

            try
            {
                if (IsEditMode)
                    await PerformUpdateAsync();
                else
                    await PerformCreateAsync();

                _modalNavigationControl.Close();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PerformUpdateAsync()
        {
            UpdateLocationDto updateDto = _locationInputModel.ToUpdateDto();
            await _locationService.UpdateLocationAsync(updateDto);
        }

        private async Task PerformCreateAsync()
        {
            CreateLocationDto createDto = _locationInputModel.ToCreateDto();
            await _locationService.CreateLocationAsync(createDto);
        }

        private void ExecuteResetCommand() => ResetForm();

        private void ExecuteCancelCommand() => _modalNavigationControl.Close();

        #endregion

        #region Properties

        public LocationInputModel Input
        {
            get => _locationInputModel;
            private set
            {
                if (_locationInputModel != value)
                {
                    UnsubscribeFromInputModelEvents();
                    SetProperty(ref _locationInputModel, value);
                    SubscribeToInputModelEvents();
                }
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value, () => RaisePropertyChanged(() => CanSave));
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, () => ResetCommand.RaiseCanExecuteChanged());
        }

        // Location Types for dropdown
        public List<LocationType> LocationTypes { get; } = Enum
                                                           .GetValues(typeof(LocationType))
                                                           .Cast<LocationType>()
                                                           .ToList();

        public string Title => IsEditMode
            ? "Edit Location"
            : "Create Location";

        public string SaveButtonText => IsEditMode
            ? "Update"
            : "Create";

        // Validation state
        public bool IsValid => _locationInputModel?.IsValid ?? false;
        public bool HasErrors => _locationInputModel?.HasErrors ?? false;

        // Command availability
        public bool CanSave => IsValid && !IsLoading;
        public bool CanCancel => !IsLoading;

        #endregion

        #region Event Handling

        private void SubscribeToInputModelEvents()
        {
            if (_locationInputModel == null!) return;
            _locationInputModel!.PropertyChanged += OnInputModelPropertyChanged;
            _locationInputModel!.ErrorsChanged += OnInputModelErrorsChanged;
        }

        private void UnsubscribeFromInputModelEvents()
        {
            if (_locationInputModel == null!) return;
            _locationInputModel!.PropertyChanged -= OnInputModelPropertyChanged;
            _locationInputModel!.ErrorsChanged -= OnInputModelErrorsChanged;
        }

        private void OnInputModelPropertyChanged( object? sender, PropertyChangedEventArgs e )
        {
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);

            // Refresh commands
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void OnInputModelErrorsChanged( object? sender, DataErrorsChangedEventArgs e )
        {
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);

            // Refresh commands
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void RaiseAllPropertiesChanged()
        {
            RaisePropertyChanged(() => Title);
            RaisePropertyChanged(() => SaveButtonText);
            RaisePropertyChanged(() => IsEditMode);
            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
            RaisePropertyChanged(() => CanSave);
            RaisePropertyChanged(() => CanCancel);
        }

        #endregion
    }
}
