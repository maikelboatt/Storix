using Storix.Domain.Enums;

namespace Storix.Core.ViewModels.Products
{
    public class ProductListViewModelParameter
    {
        public int EntityId { get; set; }

        public ProductFilterType FilterType { get; set; }
    }
}
