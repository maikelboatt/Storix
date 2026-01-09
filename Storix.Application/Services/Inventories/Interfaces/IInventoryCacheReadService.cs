using System.Collections.Generic;

namespace Storix.Application.Services.Inventories.Interfaces
{
    public interface IInventoryCacheReadService
    {
        Domain.Models.Inventory? GetInventoryByIdInCache( int inventoryId );

        Domain.Models.Inventory? GetInventoryByProductAndLocationInCache( int productId, int locationId );

        List<Domain.Models.Inventory> GetInventoryByProductIdInCache( int productId );

        List<Domain.Models.Inventory> GetInventoryByLocationIdInCache( int locationId );

        List<Domain.Models.Inventory> GetAllInventoryInCache();

        List<Domain.Models.Inventory> GetLowStockItemsInCache( int threshold = 10 );

        List<Domain.Models.Inventory> GetOutOfStockItemsInCache();

        bool InventoryExistsInCache( int inventoryId );

        int GetInventoryCountInCache();

        Dictionary<int, int> GetAllProductStockLevelsInCache();

        int GetCurrentStockForProductInCache( int productId );

        public int GetProductStockAtLocationInCache( int productId, int locationId );

        int GetAvailableStockForProductInCache( int productId );

        int GetAvailableStockAtLocationInCache( int productId, int locationId );

        int GetReservedStockForProductInCache( int productId );

        Dictionary<int, int> GetStockByLocationInCache();

        void RefreshStoreCache();
    }
}
