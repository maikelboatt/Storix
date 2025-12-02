using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Services.Inventories.Interfaces;
using Storix.Application.Stores.Inventory;

namespace Storix.Application.Services.Inventories
{
    /// <summary>
    ///     Service for managing inventory cache operations.
    /// </summary>
    public class InventoryCacheService(
        IInventoryStore inventoryStore,
        IInventoryReadService readService,
        ILogger<InventoryCacheService> logger ):IInventoryCacheService
    {
        public Domain.Models.Inventory? GetInventoryByIdInCache( int inventoryId )
        {
            logger.LogDebug("Retrieving inventory {InventoryId} from cache", inventoryId);
            return inventoryStore.GetById(inventoryId);
        }

        public Domain.Models.Inventory? GetInventoryByProductAndLocationInCache( int productId, int locationId )
        {
            logger.LogDebug(
                "Retrieving inventory for product {ProductId} at location {LocationId} from cache",
                productId,
                locationId);
            return inventoryStore.GetByProductAndLocation(productId, locationId);
        }

        public List<Domain.Models.Inventory> GetInventoryByProductIdInCache( int productId )
        {
            logger.LogDebug("Retrieving inventory for product {ProductId} from cache", productId);
            return inventoryStore.GetByProductId(productId);
        }

        public List<Domain.Models.Inventory> GetInventoryByLocationIdInCache( int locationId )
        {
            logger.LogDebug("Retrieving inventory at location {LocationId} from cache", locationId);
            return inventoryStore.GetByLocationId(locationId);
        }

        public List<Domain.Models.Inventory> GetAllInventoryInCache()
        {
            logger.LogDebug("Retrieving all inventory from cache");
            return inventoryStore.GetAll();
        }

        public List<Domain.Models.Inventory> GetLowStockItemsInCache( int threshold = 10 )
        {
            logger.LogDebug("Retrieving low stock items (threshold: {Threshold}) from cache", threshold);
            return inventoryStore.GetLowStockItems(threshold);
        }

        public List<Domain.Models.Inventory> GetOutOfStockItemsInCache()
        {
            logger.LogDebug("Retrieving out of stock items from cache");
            return inventoryStore.GetOutOfStockItems();
        }

        public bool InventoryExistsInCache( int inventoryId ) => inventoryStore.Exists(inventoryId);

        public int GetInventoryCountInCache() => inventoryStore.GetCount();

        public int GetTotalStockForProductInCache( int productId ) => inventoryStore.GetTotalStockForProduct(productId);

        public void RefreshStoreCache()
        {
            logger.LogInformation("Initiating inventory store cache refresh");
            _ = Task.Run(async () =>
            {
                try
                {
                    DatabaseResult<IEnumerable<Domain.Models.Inventory>> result =
                        await readService.GetAllInventoryAsync();

                    if (result is { IsSuccess: true, Value: not null })
                    {
                        inventoryStore.Initialize(result.Value.ToList());
                        logger.LogInformation(
                            "Inventory store cache refreshed successfully with {Count} records",
                            result.Value.Count());
                    }
                    else
                    {
                        logger.LogWarning("Failed to refresh inventory store cache: {Error}", result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception occurred while refreshing inventory store cache");
                }
            });
        }
    }
}
