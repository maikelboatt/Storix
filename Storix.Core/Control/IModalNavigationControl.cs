using MvvmCross.ViewModels;

namespace Storix.Core.Control
{
    public interface IModalNavigationControl
    {
        void PopUp<TViewModel>( int? parameter = null ) where TViewModel : IMvxViewModel;

        void Close();

        // void PopUp<T>( List<Animal> selectedAnimals ) where T : IMvxViewModel;

        // void PopUp<T>( InseminationDetailAnimalList inseminationDetailAnimalList ) where T : IMvxViewModel;

        // void PopUp<T>( ScheduledNotification scheduledNotifications ) where T : IMvxViewModel;
    }
}
