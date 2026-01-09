using MvvmCross.ViewModels;
using Storix.Core.ViewModels.Orders;
using Storix.Core.ViewModels.Products;

namespace Storix.Core.Control
{
    /// <summary>
    /// Interface for controlling modal navigation
    /// </summary>
    public interface IModalNavigationControl
    {
        /// <summary>
        /// Opens a modal with ProductListViewModelParameter for filtering products
        /// </summary>
        /// <typeparam name="TViewModel">The type of ViewModel to open</typeparam>
        /// <param name="productListViewModelParameter">Parameter containing filter type and entity ID</param>
        /// <param name="parameter">Optional additional parameter (not used in this overload)</param>
        void PopUp<TViewModel>( ProductListViewModelParameter productListViewModelParameter, int? parameter = null )
            where TViewModel : IMvxViewModel;

        /// <summary>
        /// Opens a modal with OrderListViewModelParameter for filtering orders
        /// </summary>
        /// <typeparam name="TViewModel">The type of ViewModel to open</typeparam>
        /// <param name="orderListViewModelParameter">Parameter containing filter type and entity ID</param>
        /// <param name="parameter">Optional additional parameter (not used in this overload)</param>
        void PopUp<TViewModel>( OrderListViewModelParameter orderListViewModelParameter, int? parameter = null )
            where TViewModel : IMvxViewModel;

        /// <summary>
        /// Opens a modal with a simple integer parameter
        /// </summary>
        /// <typeparam name="TViewModel">The type of ViewModel to open</typeparam>
        /// <param name="parameter">Optional integer parameter</param>
        void PopUp<TViewModel>( int? parameter = null )
            where TViewModel : IMvxViewModel;

        /// <summary>
        /// Opens a modal and waits for a result to be returned
        /// </summary>
        /// <typeparam name="TViewModel">The type of ViewModel to open</typeparam>
        /// <typeparam name="TParameter">The type of parameter to pass</typeparam>
        /// <typeparam name="TResult">The type of result expected</typeparam>
        /// <param name="parameter">The parameter to pass to the ViewModel</param>
        /// <returns>The result from the modal, or null if cancelled</returns>
        Task<TResult?> PopUpWithResultAsync<TViewModel, TParameter, TResult>( TParameter parameter )
            where TViewModel : IMvxViewModel
            where TResult : class;

        /// <summary>
        /// Closes the current modal without a result
        /// </summary>
        void Close();

        /// <summary>
        /// Closes the current modal with a result
        /// </summary>
        /// <typeparam name="TResult">The type of result to return</typeparam>
        /// <param name="result">The result to return</param>
        void Close<TResult>( TResult result )
            where TResult : class;
    }
}
