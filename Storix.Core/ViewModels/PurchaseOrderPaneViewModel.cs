using MvvmCross.ViewModels;
using Storix.Core.Factory;
using Storix.Core.ViewModels.Orders.Purchase;

namespace Storix.Core.ViewModels
{
    public class PurchaseOrderPaneViewModel( IViewModelFactory viewModelFactory ):MvxViewModel
    {
        public PurchaseOrderListViewModel? PurchaseOrder => CreateDashboardViewModel();

        private PurchaseOrderListViewModel? CreateDashboardViewModel()
        {
            PurchaseOrderListViewModel? viewModel = viewModelFactory.CreateViewModel<PurchaseOrderListViewModel>();
            viewModel?.Prepare();
            viewModel?.Initialize();
            return viewModel;
        }
    }
}
