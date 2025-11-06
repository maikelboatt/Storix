using System;
using MvvmCross.ViewModels;

namespace Storix.Application.Stores
{
    /// <summary>
    ///     Manages the navigation for modal view models.
    /// </summary>
    public class ModalNavigationStore
    {
        private MvxViewModel? _currentModalViewModel;

        /// <summary>
        ///     Gets or sets the current modal view model.
        ///     Invokes the <see cref="CurrentModalViewModelChanged" /> event when set.
        /// </summary>
        public MvxViewModel? CurrentModalViewModel
        {
            get => _currentModalViewModel;
            set
            {
                _currentModalViewModel = value;
                CurrentModalViewModelChanged?.Invoke();
            }
        }

        /// <summary>
        ///     Indicates whether a modal view model is currently open.
        /// </summary>
        public bool IsOpen => CurrentModalViewModel != null;

        /// <summary>
        ///     Event triggered when the current modal view model changes.
        /// </summary>
        public event Action? CurrentModalViewModelChanged;

        /// <summary>
        ///     Closes the current modal by setting the view model to null.
        /// </summary>
        public void Close()
        {
            CurrentModalViewModel = null;
        }
    }
}
