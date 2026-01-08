using System.Collections.Generic;
using Storix.Application.DTO.Categories;
using Storix.Application.DTO.Products;

namespace Storix.Application.Services.Print.Interfaces
{
    public interface ICategoryPrintService
    {
        void PrintCategoryDetails( CategoryDto category,
            string? parentCategoryName,
            List<SubcategoryInfo> subcategories,
            List<ProductSummary> products,
            int totalProducts,
            int totalSubcategories,
            decimal totalCategoryValue );
    }
}
