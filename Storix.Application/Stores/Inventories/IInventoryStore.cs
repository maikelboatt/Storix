using System;
using System.Collections.Generic;

namespace Storix.Application.Stores.Inventories
{
    public interface IInventoryStore
    {
        void Initialize( IEnumerable<Domain.Models.Inventory> inventoryItems );

        void Clear();

        event Action<Domain.Models.Inventory>? InventoryAdded;
        event Action<Domain.Models.Inventory>? InventoryUpdated;
        event Action<int>? InventoryDeleted;

        Domain.Models.Inventory? Create( Domain.Models.Inventory inventory );

        Domain.Models.Inventory? Update( Domain.Models.Inventory inventory );

        bool Delete( int inventoryId );

        Domain.Models.Inventory? GetById( int inventoryId );

        Domain.Models.Inventory? GetByProductAndLocation( int productId, int locationId );

        List<Domain.Models.Inventory> GetByProductId( int productId );

        List<Domain.Models.Inventory> GetByLocationId( int locationId );

        List<Domain.Models.Inventory> GetAll();

        List<Domain.Models.Inventory> GetLowStockItems( int threshold = 10 );

        List<Domain.Models.Inventory> GetOutOfStockItems();

        List<Domain.Models.Inventory> Search(
            int? productId = null,
            int? locationId = null,
            int? minStock = null,
            int? maxStock = null );

        bool Exists( int inventoryId );

        bool ExistsForProductAndLocation( int productId, int locationId );

        int GetCount();

        int GetTotalStockForProduct( int productId );


        int GetAvailableStockForProduct( int productId );

        int GetReservedStockForProduct( int productId );

        Dictionary<int, int> GetStockByLocation();

        Dictionary<int, int> GetStockByProduct();

        int GetLowStockCount( int threshold = 10 );

        int GetOutOfStockCount();

        bool IsInStock( int productId, int locationId );

        bool HasAvailableStock( int productId, int locationId, int requiredQuantity );

        int GetAvailableStockAtLocation( int productId, int locationId );

        int GetCurrentStockAtLocation( int productId, int locationId );

        int GetReservedStock( int productId, int locationId );
    }
}
