using MvvmCross.ViewModels;
using Storix.Core.Factory;
using Storix.Core.ViewModels.Products;

namespace Storix.Core.ViewModels
{
    public class ProductPaneViewModel( IViewModelFactory viewModelFactory ):MvxViewModel
    {
        public ProductListViewModel ProductListViewModel => CreateViewModel();

        private ProductListViewModel CreateViewModel()
        {
            ProductListViewModel? viewModel = viewModelFactory.CreateViewModel<ProductListViewModel>();
            viewModel?.Prepare();
            viewModel?.Initialize();
            return viewModel!;
        }
    }
}
