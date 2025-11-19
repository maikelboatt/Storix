using MvvmCross.ViewModels;
using Storix.Core.Factory;
using Storix.Core.ViewModels.Orders.Sales;

namespace Storix.Core.ViewModels
{
    public class SalesOrderPaneViewModel( IViewModelFactory viewModelFactory ):MvxViewModel
    {
        public SalesOrderListViewModel? SalesOrder => CreateDashboardViewModel();

        private SalesOrderListViewModel? CreateDashboardViewModel()
        {
            SalesOrderListViewModel? viewModel = viewModelFactory.CreateViewModel<SalesOrderListViewModel>();
            viewModel?.Prepare();
            viewModel?.Initialize();
            return viewModel;
        }
    }
}
