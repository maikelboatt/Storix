using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Orders
{
    public class OrderListViewModelParameter
    {
        public int EntityId { get; set; }


        public OrderFilterType FilterType { get; set; }
    }
}
