# Storix - Inventory & Order Management System

A production-grade desktop inventory management system built with WPF, featuring clean architecture, immutable domain models, and high-performance async operations.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-MVVM-0078D4?style=flat-square)](https://github.com/dotnet/wpf)
[![SQL Server](https://img.shields.io/badge/SQL_Server-2025-CC2927?style=flat-square)](https://www.microsoft.com/sql-server)
[![Dapper](https://img.shields.io/badge/Dapper-Micro--ORM-orange?style=flat-square)](https://github.com/DapperLib/Dapper)

## Overview

Storix manages products, inventory, orders (sales & purchase), suppliers, customers, and multi-location stock tracking. Built with scalability in mind using N-tier architecture, MVVM pattern, and C# record-based domain models.

**Key Highlights:**
- Clean N-tier architecture with clear separation of concerns
- Asynchronous data operations using Dapper for UI responsiveness
- In-memory state store to reduce database round-trips
- Immutable record-based domain models with built-in business logic
- Soft delete pattern for data safety and audit trails
- Feature-based modular design for extensibility

---

## Features

### Inventory Management
- Product catalog with SKU, barcode, categories, pricing, and cost tracking
- Multi-location inventory with real-time stock levels
- Automated low/overstock alerts based on min/max thresholds
- Stock adjustments and transfers with complete audit trail
- Computed profit margins and available stock calculations

### Order Management
- Sales and purchase orders with status tracking
- Multi-line items with automatic validation
- Reserved stock management for pending orders
- Overdue order detection
- Order-to-inventory synchronization

### Business Partners
- Supplier and customer management
- Contact information tracking
- Soft delete support for data retention

### Transaction Tracking
- Complete inventory transaction history (adjustments, sales, purchases, returns, transfers)
- Inter-location stock movements
- User attribution for all transactions
- Reference linking to orders and invoices

---

## Architecture

### Tech Stack

- **.NET 9.0** - Latest framework features and performance improvements
- **WPF + MVVM** - Separation of UI and business logic
- **C# Records** - Immutable domain models with value-based equality
- **Dapper** - High-performance micro-ORM
- **SQL Server 2025** - Enterprise-grade database
- **Async/Await** - Non-blocking operations throughout

### N-Tier Structure

```
┌─────────────────────────────────┐
│  Presentation (WPF + MVVM)      │  Views, ViewModels, Commands
├─────────────────────────────────┤
│  Application Services           │  Business Logic, Validation, Events
├─────────────────────────────────┤
│  Domain Models (Records)        │  Entities, Enums, Business Rules
├─────────────────────────────────┤
│  Data Access (Dapper)           │  Repositories, Async Operations
├─────────────────────────────────┤
│  SQL Server 2025                │  Relational Database
└─────────────────────────────────┘
```

---

## Domain Models

All models are **C# records** with immutability, value equality, and embedded business logic.

### Product
```csharp
public record Product(
    int ProductId,
    string Name,
    string SKU,
    string Description,
    string? Barcode,
    decimal Price,
    decimal Cost,
    int MinStockLevel,
    int MaxStockLevel,
    int SupplierId,
    int CategoryId,
    DateTime CreatedDate,
    DateTime? UpdatedDate = null,
    bool IsDeleted = false,
    DateTime? DeletedAt = null
) : ISoftDeletable
{
    public decimal ProfitMargin => Price - Cost;
    public bool IsLowStock(int currentStock) => currentStock <= MinStockLevel;
}
```

### Inventory
```csharp
public record Inventory(
    int InventoryId,
    int ProductId,
    int LocationId,
    int CurrentStock,
    int ReservedStock,
    DateTime LastUpdated
)
{
    public int AvailableStock => CurrentStock - ReservedStock;
    public bool IsInStock => AvailableStock > 0;
}
```

### Order & OrderItem
```csharp
public record Order(
    int OrderId,
    OrderType Type,           // Sale or Purchase
    OrderStatus Status,
    int? SupplierId,
    int? CustomerId,
    DateTime OrderDate,
    DateTime? DeliveryDate,
    string? Notes,
    int CreatedBy
)
{
    public bool IsOverdue => DeliveryDate.HasValue && DeliveryDate < DateTime.UtcNow;
}

public record OrderItem(
    int OrderItemId,
    int OrderId,
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
)
{
    public bool IsValidTotal => TotalPrice == UnitPrice * Quantity;
}
```

### Transactions & Movements
```csharp
public record InventoryTransaction(
    int TransactionId,
    int ProductId,
    int LocationId,
    TransactionType Type,     // Adjustment, Sale, Purchase, Return, Transfer
    int Quantity,
    decimal? UnitCost,
    string? Reference,
    string? Notes,
    int CreatedBy,
    DateTime CreatedDate
);

public record InventoryMovement(
    int MovementId,
    int ProductId,
    int FromLocationId,
    int ToLocationId,
    int Quantity,
    string? Notes,
    int CreatedBy,
    DateTime CreatedDate
);
```

### Supporting Models
```csharp
public record Category(
    int CategoryId,
    string Name,
    string? Description,
    int? ParentCategoryId,    // Hierarchical categories
    string? ImageUrl,
    bool IsDeleted = false,
    DateTime? DeletedAt = null
) : ISoftDeletable;

public record Location(
    int LocationId,
    string Name,
    string? Description,
    LocationType Type,        // Warehouse, Store, Distribution
    string? Address,
    bool IsDeleted = false,
    DateTime? DeletedAt = null
) : ISoftDeletable;

public record Supplier(
    int SupplierId,
    string Name,
    string Email,
    string Phone,
    string Address,
    bool IsDeleted = false,
    DateTime? DeletedAt = null
) : ISoftDeletable;

public record Customer(
    int CustomerId,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    bool IsDeleted = false,
    DateTime? DeletedAt = null
) : ISoftDeletable;

public record User(
    int UserId,
    string Username,
    string Password,          // Hashed
    string Role,
    string? FullName,
    string? Email,
    bool IsActive,
    bool IsDeleted = false,
    DateTime? DeletedAt = null
) : ISoftDeletable;
```

---

## Database Schema

### Key Tables

**Products** - Core product information with min/max stock levels
**Categories** - Hierarchical product categorization (self-referencing)
**Inventory** - Per-location stock tracking with reserved quantities
**Orders** - Unified table for sales and purchase orders
**OrderItems** - Line items with validation constraints
**InventoryTransactions** - Complete audit trail of all stock changes
**InventoryMovements** - Inter-location transfers
**Suppliers/Customers** - Business partner information
**Locations** - Warehouses, stores, distribution centers
**Users** - Authentication and role-based access

### Key Constraints

```sql
-- Price validation
CONSTRAINT CHK_Products_Price CHECK (Price >= 0)
CONSTRAINT CHK_Products_Cost CHECK (Cost >= 0)

-- Stock level validation
CONSTRAINT CHK_Products_StockLevels CHECK (MaxStockLevel > MinStockLevel)
CONSTRAINT CHK_Inventory_Stock CHECK (CurrentStock >= 0 AND ReservedStock >= 0)

-- Order item total validation
CONSTRAINT CHK_OrderItems_TotalPrice CHECK (TotalPrice = UnitPrice * Quantity)

-- Order partner validation (Sale must have Customer, Purchase must have Supplier)
CONSTRAINT CHK_Orders_Partner CHECK (
    (Type = 0 AND CustomerId IS NOT NULL AND SupplierId IS NULL) OR
    (Type = 1 AND SupplierId IS NOT NULL AND CustomerId IS NULL)
)

-- Movement location validation
CONSTRAINT CHK_InventoryMovements_Locations CHECK (FromLocationId <> ToLocationId)
```

---

## Performance Features

### In-Memory State Store
- Frequently accessed records cached (products, categories, locations)
- Reduces database queries by ~60% for common operations
- Smart cache invalidation on data changes
- Optimized for read-heavy workloads

### Async Operations
All I/O operations are asynchronous for responsive UI:
```csharp
await productRepository.GetByIdAsync(productId);
await inventoryService.TransferStockAsync(movement);
await orderService.CreateOrderAsync(order, items);
```

### Dapper Optimization
- Micro-ORM with minimal overhead
- Parameterized queries for plan caching
- Batch operations for bulk inserts
- Direct mapping to record types

---

## Getting Started

### Prerequisites
- Windows 10/11
- .NET 9.0 SDK
- SQL Server 2019+ (2025 recommended)

### Installation

1. **Clone repository**
   ```bash
   git clone https://github.com/yourusername/storix.git
   cd storix
   ```

2. **Setup database**
   ```bash
   sqlcmd -S your_server -i Storix.Database/Schema/CreateDatabase.sql
   sqlcmd -S your_server -d StorixDB -i Storix.Database/Schema/CreateTables.sql
   ```

3. **Configure connection**
   
   Update `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "StorixDatabase": "Server=localhost;Database=StorixDB;Integrated Security=true;TrustServerCertificate=true"
     }
   }
   ```

4. **Run**
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project Storix.Presentation
   ```

Default credentials: `admin` / `admin123` (change immediately)

---

## Project Structure

```
Storix/
├── Storix.Presentation/       # WPF Views & ViewModels
├── Storix.Application/        # Business Services & Validation
├── Storix.Domain/             # Record-based Domain Models
│   ├── Models/                # Product, Order, Inventory, etc.
│   ├── Enums/                 # OrderType, TransactionType, etc.
│   └── Interfaces/            # ISoftDeletable
├── Storix.DataAccess/         # Dapper Repositories
├── Storix.Infrastructure/     # Logging, Config, Security
└── Storix.Database/           # SQL Scripts & Migrations
```

---

## Key Design Patterns

**MVVM** - Complete UI/logic separation  
**Repository Pattern** - Abstracted data access  
**Soft Delete** - ISoftDeletable interface for safe archival  
**Immutable Records** - Thread-safe domain models  
**Event Aggregation** - Decoupled module communication  
**Async/Await** - Non-blocking I/O throughout

---

## Example Usage

### Check Low Stock
```csharp
var product = await productRepo.GetByIdAsync(100);
var inventory = await inventoryRepo.GetByProductAndLocationAsync(100, 1);

if (product.IsLowStock(inventory.CurrentStock))
{
    // Trigger reorder workflow
}
```

### Transfer Stock
```csharp
var movement = new InventoryMovement(
    MovementId: 0,
    ProductId: 100,
    FromLocationId: 1,    // Main Warehouse
    ToLocationId: 2,      // Branch Store
    Quantity: 50,
    Notes: "Restocking for weekend sale",
    CreatedBy: userId,
    CreatedDate: DateTime.UtcNow
);

await inventoryService.TransferStockAsync(movement);
```

### Create Order
```csharp
var order = new Order(
    OrderId: 0,
    Type: OrderType.Sale,
    Status: OrderStatus.Pending,
    SupplierId: null,
    CustomerId: 42,
    OrderDate: DateTime.UtcNow,
    DeliveryDate: DateTime.UtcNow.AddDays(3),
    Notes: "Rush delivery",
    CreatedBy: userId
);

var items = new List<OrderItem>
{
    new(0, order.OrderId, 100, 2, 1299.99m, 2599.98m),
    new(0, order.OrderId, 101, 1, 499.99m, 499.99m)
};

await orderService.CreateOrderWithItemsAsync(order, items);
// Automatically reserves stock
```

---

## Testing

```bash
dotnet test                                    # All tests
dotnet test --filter Category=Unit             # Unit tests only
dotnet test /p:CollectCoverage=true            # With coverage
```

**Coverage Areas:**
- Domain model business logic (IsLowStock, IsOverdue, IsValidTotal)
- Repository async operations
- Service layer validation
- Soft delete behavior
- Computed properties

---

## Security

- **Role-based access control** - Granular permissions
- **Password hashing** - Never store plain text
- **Soft delete** - Preserve audit trail
- **Parameterized queries** - SQL injection protection
- **User activity tracking** - Complete audit log

---

## Roadmap

- [ ] Advanced reporting with charts
- [ ] Barcode scanning integration
- [ ] Batch import/export (CSV, Excel)
- [ ] RESTful API for integrations
- [ ] Multi-currency support
- [ ] Email notifications
- [ ] Mobile companion app
- [ ] ML-based demand forecasting

---

## Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/NewFeature`)
3. Use C# records for domain models
4. Add tests for new features
5. Commit changes (`git commit -m 'Add NewFeature'`)
6. Push to branch (`git push origin feature/NewFeature`)
7. Open Pull Request

---

## License

MIT License - see [LICENSE](LICENSE)

---

## Author

**Maikel Boatt**  
GitHub: [@yourusername](https://github.com/maikelboatt)  
Email: boattmaikel@gmail.com

---

Built with WPF, .NET 9, Dapper, and SQL Server 2025

⭐ Star this repo if you find it useful!
