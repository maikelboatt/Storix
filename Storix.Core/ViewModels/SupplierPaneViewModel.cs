using MvvmCross.ViewModels;
using Storix.Core.Factory;
using Storix.Core.ViewModels.Suppliers;

namespace Storix.Core.ViewModels
{
    public class SupplierPaneViewModel( IViewModelFactory viewModelFactory ):MvxViewModel
    {
        public SupplierListViewModel? SupplierListViewModel => CreateViewModel();

        public SupplierListViewModel? CreateViewModel()
        {
            SupplierListViewModel? viewModel = viewModelFactory.CreateViewModel<SupplierListViewModel>();
            viewModel?.Prepare();
            viewModel?.Initialize();
            return viewModel;
        }
    }
}
