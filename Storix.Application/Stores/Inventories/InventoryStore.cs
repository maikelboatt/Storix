using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Inventories
{
    /// <summary>
    ///     In-memory cache for inventory records.
    ///     Provides fast lookup for frequently accessed inventory data.
    /// </summary>
    public class InventoryStore:IInventoryStore
    {
        private readonly Dictionary<int, Inventory> _inventory;
        private readonly Dictionary<int, List<int>> _productIndex;      // ProductId -> List of InventoryIds
        private readonly Dictionary<int, List<int>> _locationIndex;     // LocationId -> List of InventoryIds
        private readonly Dictionary<string, int> _productLocationIndex; // "ProductId-LocationId" -> InventoryId

        public InventoryStore( List<Inventory>? initialInventory = null )
        {
            _inventory = new Dictionary<int, Inventory>();
            _productIndex = new Dictionary<int, List<int>>();
            _locationIndex = new Dictionary<int, List<int>>();
            _productLocationIndex = new Dictionary<string, int>();

            if (initialInventory != null)
            {
                Initialize(initialInventory);
            }
        }

        public void Initialize( IEnumerable<Inventory> inventoryItems )
        {
            _inventory.Clear();
            _productIndex.Clear();
            _locationIndex.Clear();
            _productLocationIndex.Clear();

            foreach (Inventory item in inventoryItems)
            {
                AddToIndexes(item);
            }
        }

        public void Clear()
        {
            _inventory.Clear();
            _productIndex.Clear();
            _locationIndex.Clear();
            _productLocationIndex.Clear();
        }

        public event Action<Inventory>? InventoryAdded;
        public event Action<Inventory>? InventoryUpdated;
        public event Action<int>? InventoryDeleted;

        #region CRUD Operations

        public Inventory? Create( Inventory inventory )
        {
            if (inventory.InventoryId <= 0)
            {
                return null;
            }

            // Check if inventory already exists
            if (_inventory.ContainsKey(inventory.InventoryId))
            {
                return null;
            }

            // Check if product-location combination already exists
            string productLocationKey = GetProductLocationKey(inventory.ProductId, inventory.LocationId);
            if (_productLocationIndex.ContainsKey(productLocationKey))
            {
                return null;
            }

            AddToIndexes(inventory);
            InventoryAdded?.Invoke(inventory);
            return inventory;
        }

        public Inventory? Update( Inventory inventory )
        {
            if (!_inventory.TryGetValue(inventory.InventoryId, out Inventory? existingInventory))
            {
                return null; // Inventory not found in cache
            }

            // If product or location changed, update indexes
            if (existingInventory.ProductId != inventory.ProductId ||
                existingInventory.LocationId != inventory.LocationId)
            {
                RemoveFromIndexes(existingInventory);
                AddToIndexes(inventory);
            }
            else
            {
                // Just update the record
                _inventory[inventory.InventoryId] = inventory;
            }

            InventoryUpdated?.Invoke(inventory);
            return inventory;
        }

        public bool Delete( int inventoryId )
        {
            if (!_inventory.Remove(inventoryId, out Inventory? inventory))
                return false;

            RemoveFromIndexes(inventory);
            InventoryDeleted?.Invoke(inventoryId);
            return true;
        }

        #endregion

        #region Lookups

        public Inventory? GetById( int inventoryId ) => _inventory.TryGetValue(inventoryId, out Inventory? inventory)
            ? inventory
            : null;

        public Inventory? GetByProductAndLocation( int productId, int locationId )
        {
            string key = GetProductLocationKey(productId, locationId);

            if (_productLocationIndex.TryGetValue(key, out int inventoryId))
            {
                return _inventory.GetValueOrDefault(inventoryId);
            }

            return null;
        }

        public List<Inventory> GetByProductId( int productId )
        {
            if (!_productIndex.TryGetValue(productId, out List<int>? inventoryIds))
            {
                return new List<Inventory>();
            }

            return inventoryIds
                   .Select(id => _inventory.TryGetValue(id, out Inventory? inv)
                               ? inv
                               : null)
                   .Where(inv => inv != null)
                   .Cast<Inventory>()
                   .OrderBy(inv => inv.LocationId)
                   .ToList();
        }

        public List<Inventory> GetByLocationId( int locationId )
        {
            if (!_locationIndex.TryGetValue(locationId, out List<int>? inventoryIds))
            {
                return new List<Inventory>();
            }

            return inventoryIds
                   .Select(id => _inventory.TryGetValue(id, out Inventory? inv)
                               ? inv
                               : null)
                   .Where(inv => inv != null)
                   .Cast<Inventory>()
                   .OrderBy(inv => inv.ProductId)
                   .ToList();
        }

        public List<Inventory> GetAll()
        {
            return _inventory
                   .Values
                   .OrderBy(inv => inv.ProductId)
                   .ThenBy(inv => inv.LocationId)
                   .ToList();
        }

        public List<Inventory> GetLowStockItems( int threshold = 10 )
        {
            return _inventory
                   .Values
                   .Where(inv => inv.CurrentStock <= threshold)
                   .OrderBy(inv => inv.CurrentStock)
                   .ThenBy(inv => inv.ProductId)
                   .ToList();
        }

        public List<Inventory> GetOutOfStockItems()
        {
            return _inventory
                   .Values
                   .Where(inv => inv.CurrentStock == 0)
                   .OrderBy(inv => inv.ProductId)
                   .ToList();
        }

        public List<Inventory> Search(
            int? productId = null,
            int? locationId = null,
            int? minStock = null,
            int? maxStock = null )
        {
            IEnumerable<Inventory> query = _inventory.Values;

            if (productId.HasValue)
            {
                query = query.Where(inv => inv.ProductId == productId.Value);
            }

            if (locationId.HasValue)
            {
                query = query.Where(inv => inv.LocationId == locationId.Value);
            }

            if (minStock.HasValue)
            {
                query = query.Where(inv => inv.CurrentStock >= minStock.Value);
            }

            if (maxStock.HasValue)
            {
                query = query.Where(inv => inv.CurrentStock <= maxStock.Value);
            }

            return query
                   .OrderBy(inv => inv.ProductId)
                   .ThenBy(inv => inv.LocationId)
                   .ToList();
        }

        #endregion

        #region Validation

        public bool Exists( int inventoryId ) => _inventory.ContainsKey(inventoryId);

        public bool ExistsForProductAndLocation( int productId, int locationId )
        {
            string key = GetProductLocationKey(productId, locationId);
            return _productLocationIndex.ContainsKey(key);
        }

        #endregion

        #region Statistics

        public int GetCount() => _inventory.Count;

        public int GetTotalStockForProduct( int productId )
        {
            if (!_productIndex.TryGetValue(productId, out List<int>? inventoryIds))
            {
                return 0;
            }

            return inventoryIds
                   .Select(id => _inventory.TryGetValue(id, out Inventory? inv)
                               ? inv.CurrentStock
                               : 0)
                   .Sum();
        }

        public int GetAvailableStockForProduct( int productId )
        {
            if (!_productIndex.TryGetValue(productId, out List<int>? inventoryIds))
            {
                return 0;
            }

            return inventoryIds
                   .Select(id => _inventory.TryGetValue(id, out Inventory? inv)
                               ? inv.AvailableStock
                               : 0)
                   .Sum();
        }

        public int GetReservedStockForProduct( int productId )
        {
            if (!_productIndex.TryGetValue(productId, out List<int>? inventoryIds))
            {
                return 0;
            }

            return inventoryIds
                   .Select(id => _inventory.TryGetValue(id, out Inventory? inv)
                               ? inv.ReservedStock
                               : 0)
                   .Sum();
        }

        public Dictionary<int, int> GetStockByLocation()
        {
            return _inventory
                   .Values
                   .GroupBy(inv => inv.LocationId)
                   .ToDictionary(
                       g => g.Key,
                       g => g.Sum(inv => inv.CurrentStock)
                   );
        }

        public Dictionary<int, int> GetStockByProduct()
        {
            return _inventory
                   .Values
                   .GroupBy(inv => inv.ProductId)
                   .ToDictionary(
                       g => g.Key,
                       g => g.Sum(inv => inv.CurrentStock)
                   );
        }

        public int GetLowStockCount( int threshold = 10 )
        {
            return _inventory.Values.Count(inv => inv.CurrentStock <= threshold);
        }

        public int GetOutOfStockCount()
        {
            return _inventory.Values.Count(inv => inv.CurrentStock == 0);
        }

        #endregion

        #region Stock Level Queries

        public bool IsInStock( int productId, int locationId )
        {
            Inventory? inventory = GetByProductAndLocation(productId, locationId);
            return inventory?.IsInStock ?? false;
        }

        public bool HasAvailableStock( int productId, int locationId, int requiredQuantity )
        {
            Inventory? inventory = GetByProductAndLocation(productId, locationId);
            return inventory != null && inventory.AvailableStock >= requiredQuantity;
        }

        public int GetAvailableStockAtLocation( int productId, int locationId )
        {
            Inventory? inventory = GetByProductAndLocation(productId, locationId);
            return inventory?.AvailableStock ?? 0;
        }

        public int GetCurrentStockAtLocation( int productId, int locationId )
        {
            Inventory? inventory = GetByProductAndLocation(productId, locationId);
            return inventory?.CurrentStock ?? 0;
        }

        public int GetReservedStock( int productId, int locationId )
        {
            Inventory? inventory = GetByProductAndLocation(productId, locationId);
            return inventory?.ReservedStock ?? 0;
        }

        #endregion

        #region Private Helper Methods

        private void AddToIndexes( Inventory inventory )
        {
            // Add to main dictionary
            _inventory[inventory.InventoryId] = inventory;

            // Add to product index
            if (!_productIndex.ContainsKey(inventory.ProductId))
            {
                _productIndex[inventory.ProductId] = new List<int>();
            }
            _productIndex[inventory.ProductId]
                .Add(inventory.InventoryId);

            // Add to location index
            if (!_locationIndex.ContainsKey(inventory.LocationId))
            {
                _locationIndex[inventory.LocationId] = new List<int>();
            }
            _locationIndex[inventory.LocationId]
                .Add(inventory.InventoryId);

            // Add to product-location index
            string productLocationKey = GetProductLocationKey(inventory.ProductId, inventory.LocationId);
            _productLocationIndex[productLocationKey] = inventory.InventoryId;
        }

        private void RemoveFromIndexes( Inventory inventory )
        {
            // Remove from product index
            if (_productIndex.TryGetValue(inventory.ProductId, out List<int>? productInventoryIds))
            {
                productInventoryIds.Remove(inventory.InventoryId);
                if (productInventoryIds.Count == 0)
                {
                    _productIndex.Remove(inventory.ProductId);
                }
            }

            // Remove from location index
            if (_locationIndex.TryGetValue(inventory.LocationId, out List<int>? locationInventoryIds))
            {
                locationInventoryIds.Remove(inventory.InventoryId);
                if (locationInventoryIds.Count == 0)
                {
                    _locationIndex.Remove(inventory.LocationId);
                }
            }

            // Remove from product-location index
            string productLocationKey = GetProductLocationKey(inventory.ProductId, inventory.LocationId);
            _productLocationIndex.Remove(productLocationKey);
        }

        private static string GetProductLocationKey( int productId, int locationId ) => $"{productId}-{locationId}";

        #endregion
    }
}
