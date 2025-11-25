using MvvmCross.ViewModels;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Orders;

namespace Storix.Core.ViewModels.Orders
{
    public class OrderDeleteViewModelBase:MvxViewModel<int>
    {
        private int _orderId;

        public OrderDeleteViewModelBase(IOrderService orderService, IOrderStore orderStore)
        {
            
        }

        public override void Prepare( int parameter )
        {
            _orderId = parameter;
        }
    }
}
