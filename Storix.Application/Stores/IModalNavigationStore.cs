using System;
using MvvmCross.ViewModels;

namespace Storix.Application.Stores
{
    public interface IModalNavigationStore
    {
        /// <summary>
        ///     Gets or sets the current modal view model.
        ///     Invokes the <see cref="CurrentModalViewModelChanged" /> event when set.
        /// </summary>
        MvxViewModel? CurrentModalViewModel { get; set; }

        /// <summary>
        ///     Indicates whether a modal view model is currently open.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        ///     Event triggered when the current modal view model changes.
        /// </summary>
        event Action? CurrentModalViewModelChanged;

        /// <summary>
        ///     Closes the current modal by setting the view model to null.
        /// </summary>
        void Close();
    }
}
