using MvvmCross;
using MvvmCross.ViewModels;

namespace Storix.Core.Factory
{
    public class ViewModelFactory:IViewModelFactory
    {
        public TViewModel? CreateViewModel<TViewModel>() where TViewModel : MvxViewModel => Mvx.IoCProvider?.Resolve<TViewModel>();

        public TViewModel? CreateViewModel<TViewModel, TParameter>( TParameter parameter ) where TViewModel : MvxViewModel
        {
            TViewModel? viewModel = Mvx.IoCProvider?.Resolve<TViewModel>();

            if (viewModel is IMvxViewModel<TParameter> parameterizedViewModel)
            {
                parameterizedViewModel.Prepare(parameter);
            }

            return viewModel;
        }
    }
}
