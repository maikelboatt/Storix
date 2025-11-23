using System;
using MvvmCross.ViewModels;
using Storix.Application.Stores;
using Storix.Core.Control;

namespace Storix.Infrastructure
{
    /// <summary>
    /// Controls modal navigation using a custom ViewModel factory
    /// </summary>
    public class ModalNavigationControl:IModalNavigationControl
    {
        private readonly ModalNavigationStore _modalNavigationStore;
        private readonly Func<Type, object, Task<MvxViewModel>> _viewModelFactory; // Changed to Task<MvxViewModel>

        public ModalNavigationControl(
            ModalNavigationStore modalNavigationStore,
            Func<Type, object, Task<MvxViewModel>> viewModelFactory ) // Changed parameter type
        {
            _modalNavigationStore = modalNavigationStore;
            _viewModelFactory = viewModelFactory;
        }

        public async void PopUp<TViewModel>( int? parameter = null ) where TViewModel : IMvxViewModel
        {
            try
            {
                // Await the factory
                MvxViewModel viewModel = await _viewModelFactory(typeof(TViewModel), parameter ?? 0);

                _modalNavigationStore.CurrentModalViewModel = viewModel;

                System.Diagnostics.Debug.WriteLine($"✅ Opened modal: {typeof(TViewModel).Name} with parameter: {parameter ?? 0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to open modal {typeof(TViewModel).Name}: {ex}");
                throw;
            }
        }

        public void Close()
        {
            _modalNavigationStore.Close();
        }
    }
}
