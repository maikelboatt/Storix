using MvvmCross.ViewModels;
using Storix.Core.Factory;
using Storix.Core.ViewModels.Customers;

namespace Storix.Core.ViewModels
{
    public class CustomerPaneViewModel( IViewModelFactory viewModelFactory ):MvxViewModel
    {
        public CustomerListViewModel? CustomerListViewModel => CreateViewModel();

        public CustomerListViewModel? CreateViewModel()
        {
            CustomerListViewModel? viewModel = viewModelFactory.CreateViewModel<CustomerListViewModel>();
            viewModel?.Prepare();
            viewModel?.Initialize();
            return viewModel;
        }
    }
}
