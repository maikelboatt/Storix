using System.Collections.ObjectModel;
using MvvmCross.ViewModels;
using Storix.Application.Common;
using Storix.Application.DTO.Customers;
using Storix.Application.DTO.OrderItems;
using Storix.Application.DTO.Orders;
using Storix.Application.DTO.Products;
using Storix.Application.DTO.Suppliers;
using Storix.Application.Services;
using Storix.Application.Services.Customers.Interfaces;
using Storix.Application.Services.OrderItems.Interfaces;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Services.Products;
using Storix.Application.Services.Products.Interfaces;
using Storix.Application.Services.Suppliers.Interfaces;
using Storix.Domain.Enums;

namespace Storix.Core.ViewModels
{
    public class DashboardViewModel:MvxViewModel
    {
        private readonly ICacheInitializerService _cacheInitializerService;
        private readonly IProductService _productService;
        private ObservableCollection<TopProductDto> _topProducts;
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<TopProductDto> TopProducts
        {
            get => _topProducts;
            set => SetProperty(ref _topProducts, value);
        }

        public DashboardViewModel( ICacheInitializerService cacheInitializerService, IProductService productService )
        {
            _cacheInitializerService = cacheInitializerService;
            _productService = productService;
        }

        public override async Task Initialize()
        {
            IsLoading = true;
            try
            {
                await LoadTopProductsFromService();
                await _cacheInitializerService.InitializeCacheAsync();
            }
            finally
            {
                IsLoading = false;
            }
            await base.Initialize();
        }


        // Alternative: Load from service
        private async Task LoadTopProductsFromService()
        {

            DatabaseResult<IEnumerable<TopProductDto>> products = await _productService.GetTop5BestSellersAsync();

            if (products.Value != null)
                TopProducts = new ObservableCollection<TopProductDto>(
                    products.Value.Select(( p, index ) => new TopProductDto(
                                              p.ProductId,
                                              p.Rank,
                                              p.ProductName,
                                              p.SKU,
                                              p.TotalRevenue,
                                              p.UnitsSold)
                    )
                );
        }
    }
}
