using MvvmCross.ViewModels;
using Storix.Core.Factory;

namespace Storix.Core.ViewModels
{
    public class AnalyticsPaneViewModel( IViewModelFactory viewModelFactory ):MvxViewModel
    {
        public DashboardViewModel? Dashboard => CreateDashboardViewModel();

        private DashboardViewModel? CreateDashboardViewModel()
        {
            DashboardViewModel? viewModel = viewModelFactory.CreateViewModel<DashboardViewModel>();
            viewModel?.Prepare();
            viewModel?.Initialize();
            return viewModel;
        }
    }
}
