using MvvmCross.ViewModels;
using Storix.Application.Stores;

namespace Storix.Core.Control
{
    public class ModalNavigationControl(
        ModalNavigationStore modalNavigationStore,
        Func<Type, object, MvxViewModel> viewModelFactory )
        :IModalNavigationControl
    {
        public void PopUp<TViewModel>( int? parameter = null ) where TViewModel : IMvxViewModel
        {
            try
            {
                MvxViewModel viewModel = viewModelFactory(typeof(TViewModel), parameter ?? 0);
                modalNavigationStore.CurrentModalViewModel = viewModel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open modal: {ex}");
                throw;
            }
        }

        public void Close()
        {
            modalNavigationStore.Close();
        }
    }

    // public void PopUp<T>( List<Animal> selectedAnimals ) where T : IMvxViewModel
    // {
    //     modalNavigationStore.CurrentModalViewModel = viewModelFactory(typeof(T), selectedAnimals);
    // }
    //
    // public void PopUp<T>( InseminationDetailAnimalList inseminationDetailAnimalList ) where T : IMvxViewModel
    // {
    //     modalNavigationStore.CurrentModalViewModel = viewModelFactory(typeof(T), inseminationDetailAnimalList);
    // }
    //
    // public void PopUp<T>( IEnumerable<ScheduledNotification> scheduledNotifications ) where T : IMvxViewModel
    // {
    //     modalNavigationStore.CurrentModalViewModel = viewModelFactory(typeof(T), scheduledNotifications);
    // }
    //
    // public void PopUp<T>( ScheduledNotification scheduledNotifications ) where T : IMvxViewModel
    // {
    //     modalNavigationStore.CurrentModalViewModel = viewModelFactory(typeof(T), scheduledNotifications);
    // }
}
