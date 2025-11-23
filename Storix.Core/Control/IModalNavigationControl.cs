using MvvmCross.ViewModels;

namespace Storix.Core.Control
{
    /// <summary>
    /// Interface for modal navigation control
    /// </summary>
    public interface IModalNavigationControl
    {
        /// <summary>
        /// Opens a modal with the specified ViewModel
        /// </summary>
        /// <typeparam name="TViewModel">The ViewModel type to display</typeparam>
        /// <param name="parameter">Optional parameter to pass to the ViewModel's Prepare method</param>
        void PopUp<TViewModel>( int? parameter = null ) where TViewModel : IMvxViewModel;

        /// <summary>
        /// Closes the current modal
        /// </summary>
        void Close();
    }
}
