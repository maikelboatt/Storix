using MvvmCross.ViewModels;
using Storix.Application.Stores;
using Storix.Core.ViewModels.Orders;
using Storix.Core.ViewModels.Products;

namespace Storix.Core.Control
{
    /// <summary>
    /// Controls modal navigation using a custom ViewModel factory
    /// </summary>
    public class ModalNavigationControl(
        ModalNavigationStore modalNavigationStore,
        Func<Type, object, Task<MvxViewModel>> viewModelFactory )
        :IModalNavigationControl
    {
        private TaskCompletionSource<object?>? _currentTaskCompletionSource;

        /// <summary>
        /// Opens a modal with ProductListViewModelParameter for filtering products by Category or Supplier
        /// </summary>
        public async void PopUp<TViewModel>( ProductListViewModelParameter productListViewModelParameter, int? parameter = null )
            where TViewModel : IMvxViewModel
        {
            try
            {
                // Use the ProductListViewModelParameter as the parameter for the factory
                MvxViewModel viewModel = await viewModelFactory(typeof(TViewModel), productListViewModelParameter);
                modalNavigationStore.CurrentModalViewModel = viewModel;

                System.Diagnostics.Debug.WriteLine(
                    $"✅ Opened modal: {typeof(TViewModel).Name} with filter: {productListViewModelParameter.FilterType}, EntityId: {productListViewModelParameter.EntityId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to open modal {typeof(TViewModel).Name}: {ex}");
            }
        }

        /// <summary>
        /// Opens a modal with OrderListViewModelParameter for filtering orders by Type, Location or CreatedBy
        /// </summary>
        public async void PopUp<TViewModel>( OrderListViewModelParameter orderListViewModelParameter, int? parameter = null ) where TViewModel : IMvxViewModel
        {
            try
            {
                // Use the OrderListViewModelParameter as the parameter for the factory
                MvxViewModel viewModel = await viewModelFactory(typeof(TViewModel), orderListViewModelParameter);
                modalNavigationStore.CurrentModalViewModel = viewModel;

                System.Diagnostics.Debug.WriteLine(
                    $"✅ Opened modal: {typeof(TViewModel).Name} with filter: {orderListViewModelParameter.FilterType}, EntityId: {orderListViewModelParameter.EntityId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to open modal {typeof(TViewModel).Name}: {ex}");
            }
        }

        /// <summary>
        /// Opens a modal with a simple integer parameter
        /// </summary>
        public async void PopUp<TViewModel>( int? parameter = null ) where TViewModel : IMvxViewModel
        {
            try
            {
                MvxViewModel viewModel = await viewModelFactory(typeof(TViewModel), parameter ?? 0);
                modalNavigationStore.CurrentModalViewModel = viewModel;

                System.Diagnostics.Debug.WriteLine($"✅ Opened modal: {typeof(TViewModel).Name} with parameter: {parameter ?? 0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to open modal {typeof(TViewModel).Name}: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Opens a modal and waits for a result to be returned
        /// </summary>
        public async Task<TResult?> PopUpWithResultAsync<TViewModel, TParameter, TResult>( TParameter parameter )
            where TViewModel : IMvxViewModel
            where TResult : class
        {
            try
            {
                // Create a TaskCompletionSource to wait for the result
                _currentTaskCompletionSource = new TaskCompletionSource<object?>();

                // Create and show the ViewModel
                MvxViewModel viewModel = await viewModelFactory(typeof(TViewModel), parameter ?? (object)0);
                modalNavigationStore.CurrentModalViewModel = viewModel;

                System.Diagnostics.Debug.WriteLine($"✅ Opened modal with result: {typeof(TViewModel).Name}");

                // Wait for the modal to close and return result
                object? result = await _currentTaskCompletionSource.Task;

                return result as TResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to open modal {typeof(TViewModel).Name}: {ex}");
                _currentTaskCompletionSource?.TrySetException(ex);
                throw;
            }
            finally
            {
                _currentTaskCompletionSource = null;
            }
        }

        /// <summary>
        /// Closes the current modal without a result
        /// </summary>
        public void Close()
        {
            modalNavigationStore.Close();

            _currentTaskCompletionSource?.TrySetResult(null);
        }

        /// <summary>
        /// Closes the current modal with a result
        /// </summary>
        public void Close<TResult>( TResult result ) where TResult : class
        {
            modalNavigationStore.Close();

            _currentTaskCompletionSource?.TrySetResult(result);

            System.Diagnostics.Debug.WriteLine($"✅ Modal closed with result: {result?.GetType().Name ?? "null"}");
        }
    }
}
