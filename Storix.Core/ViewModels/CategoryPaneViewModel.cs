using MvvmCross.ViewModels;
using Storix.Core.Factory;
using Storix.Core.ViewModels.Categories;

namespace Storix.Core.ViewModels
{
    public class CategoryPaneViewModel( IViewModelFactory viewModelFactory ):MvxViewModel
    {
        public CategoryListViewModel? CategoryListViewModel => CreateViewModel();

        private CategoryListViewModel? CreateViewModel()
        {
            CategoryListViewModel? viewModel = viewModelFactory.CreateViewModel<CategoryListViewModel>();
            viewModel?.Prepare();
            viewModel?.Initialize();
            return viewModel;
        }
    }
}
