using MvvmCross.ViewModels;
using Storix.Core.Factory;
using Storix.Core.ViewModels.Locations;

namespace Storix.Core.ViewModels
{
    public class LocationPaneViewModel( IViewModelFactory viewModelFactory ):MvxViewModel
    {
        public LocationListViewModel? LocationListViewModel => CreateViewModel();

        public LocationListViewModel? CreateViewModel()
        {
            LocationListViewModel? viewModel = viewModelFactory.CreateViewModel<LocationListViewModel>();
            viewModel?.Prepare();
            viewModel?.Initialize();
            return viewModel;
        }
    }
}
