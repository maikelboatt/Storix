# Storix - Inventory & Order Management System

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows-blue?style=flat-square" alt="Platform">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square" alt=".NET">
  <img src="https://img.shields.io/badge/WPF-MVVM-0078D4?style=flat-square" alt="WPF">
  <img src="shields.io/badge/Database-SQL%20Server%202025-CC2927?style=flat-square" alt="SQL Server">
  <img src="https://img.shields.io/badge/ORM-Dapper-orange?style=flat-square" alt="Dapper">
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="License">
</p>

## 📋 Overview

**Storix** is a production-grade desktop inventory and order management system built for modern enterprises. Designed with scalability and performance in mind, it provides comprehensive tools for managing products, stock movements, suppliers, customers, and both sales and purchase orders through an intuitive Windows desktop interface.

The application leverages industry best practices including MVVM architecture, asynchronous programming, and efficient data access patterns to deliver a responsive and reliable user experience.

---

## ✨ Key Features

### 📦 Inventory Management
- **Product Catalog**: Comprehensive product registration with categories, SKUs, and detailed specifications
- **Stock Tracking**: Real-time inventory levels across multiple locations
- **Low Stock Alerts**: Automated notifications when items fall below threshold levels
- **Stock Adjustments**: Manual inventory corrections with full audit trail
- **Stock Transfers**: Inter-location inventory movements with tracking

### 🛒 Order Management
- **Sales Orders**: Complete sales order lifecycle from creation to fulfillment
- **Purchase Orders**: Supplier purchase order management with receiving workflows
- **Order Status Tracking**: Real-time order status updates and history
- **Multi-line Items**: Support for orders with multiple products and quantities

### 👥 Partner Management
- **Supplier Database**: Centralized supplier information with contact details
- **Customer Registry**: Customer profiles with transaction history
- **Contact Management**: Complete contact information and communication logs

### 📍 Location Management
- **Multi-location Support**: Manage inventory across warehouses, stores, and facilities
- **Location Hierarchies**: Organize locations by region or business unit
- **Location-specific Stock Levels**: Track inventory quantities per location

### 📊 Dashboard & Analytics
- **Real-time Metrics**: Live KPIs including total products, active orders, and revenue
- **Activity Feed**: Recent system activities and transactions
- **Quick Actions**: One-click access to common tasks
- **Performance Indicators**: Trend analysis with percentage changes

### 🔔 Notifications & Alerts
- **System Notifications**: Real-time alerts for critical events
- **Low Stock Warnings**: Proactive inventory alerts
- **Order Updates**: Status change notifications

---

## 🏗️ Architecture

### Technical Stack

| Layer | Technology |
|-------|-----------|
| **Framework** | .NET 8.0 / WPF |
| **Architecture** | MVVM (Model-View-ViewModel) |
| **Database** | SQL Server 2025 |
| **Data Access** | Dapper (Micro-ORM) |
| **UI Components** | Custom WPF Controls |
| **Async Operations** | Task-based Asynchronous Pattern (TAP) |

### N-Tier Architecture

Storix implements a clean N-tier architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────┐
│          Presentation Layer (WPF)               │
│  ┌──────────────┐  ┌──────────────────────┐    │
│  │    Views     │  │   ViewModels (MVVM)  │    │
│  └──────────────┘  └──────────────────────┘    │
└─────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────┐
│         Application Services Layer              │
│  ┌─────────────────────────────────────────┐   │
│  │  Business Logic & Validation Services   │   │
│  │  Event-driven Synchronization           │   │
│  │  In-memory State Management             │   │
│  └─────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────┐
│           Data Access Layer (DAL)               │
│  ┌─────────────────────────────────────────┐   │
│  │  Repositories & Data Mappers (Dapper)   │   │
│  │  Asynchronous Database Operations       │   │
│  └─────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────┐
│              SQL Server 2025                    │
└─────────────────────────────────────────────────┘
```

### Design Patterns

1. **MVVM (Model-View-ViewModel)**: Complete separation of UI and business logic
2. **Repository Pattern**: Abstracted data access with testable interfaces
3. **Unit of Work**: Transaction management across multiple repositories
4. **Observer Pattern**: Event-driven UI updates and inter-module communication
5. **Factory Pattern**: Object creation and dependency injection
6. **Strategy Pattern**: Pluggable business rule implementations

---

## 🚀 Performance Optimizations

### In-Memory State Store
- **Active Record Caching**: Frequently accessed records kept in memory
- **Reduced Database Round-trips**: Minimize network latency
- **Smart Cache Invalidation**: Automatic cache updates on data changes
- **Memory-efficient Collections**: Optimized data structures for large datasets

### Asynchronous Operations
- **Non-blocking UI**: All database operations use async/await patterns
- **Responsive Interface**: UI remains interactive during data operations
- **Parallel Processing**: Concurrent operations where applicable
- **Cancellation Support**: User-initiated operation cancellation

### Database Optimization
- **Dapper Micro-ORM**: Lightweight, high-performance data access
- **Parameterized Queries**: SQL injection protection and query plan caching
- **Indexed Queries**: Optimized database schema with strategic indexes
- **Batch Operations**: Bulk inserts and updates for efficiency

---

## 📁 Project Structure

```
Storix/
├── Storix.Presentation/           # WPF UI Layer
│   ├── Views/                     # XAML Views
│   ├── ViewModels/                # ViewModels (MVVM)
│   ├── Converters/                # Value Converters
│   ├── Controls/                  # Custom Controls
│   └── Resources/                 # Styles & Templates
│
├── Storix.Application/            # Business Logic Layer
│   ├── Services/                  # Application Services
│   ├── DTOs/                      # Data Transfer Objects
│   ├── Validators/                # Business Rule Validation
│   ├── Events/                    # Event Aggregation
│   └── StateManagement/           # In-memory State Store
│
├── Storix.DataAccess/             # Data Access Layer
│   ├── Repositories/              # Repository Implementations
│   ├── Interfaces/                # Repository Contracts
│   ├── Mappers/                   # Object-Relational Mapping
│   └── ConnectionFactory/         # Database Connection Management
│
├── Storix.Domain/                 # Domain Models
│   ├── Entities/                  # Business Entities
│   ├── ValueObjects/              # Value Objects
│   └── Enums/                     # Domain Enumerations
│
├── Storix.Infrastructure/         # Cross-cutting Concerns
│   ├── Logging/                   # Logging Infrastructure
│   ├── Configuration/             # App Configuration
│   └── Utilities/                 # Helper Classes
│
└── Storix.Database/               # Database Scripts
    ├── Schema/                    # Table Definitions
    ├── StoredProcedures/          # Stored Procedures
    ├── Migrations/                # Database Migrations
    └── SeedData/                  # Initial Data Scripts
```

---

## 📊 Database Schema

### Core Tables

**Products**
```sql
- ProductId (PK)
- SKU (Unique)
- Name
- Description
- CategoryId (FK)
- UnitPrice
- ReorderLevel
- IsActive
- CreatedDate
- ModifiedDate
```

**Inventory**
```sql
- InventoryId (PK)
- ProductId (FK)
- LocationId (FK)
- QuantityOnHand
- QuantityReserved
- LastCountDate
```

**Orders** (Sales & Purchase)
```sql
- OrderId (PK)
- OrderNumber (Unique)
- OrderType (Sale/Purchase)
- CustomerId/SupplierId (FK)
- OrderDate
- Status
- TotalAmount
- CreatedBy
```

**OrderItems**
```sql
- OrderItemId (PK)
- OrderId (FK)
- ProductId (FK)
- Quantity
- UnitPrice
- LineTotal
```

---

## 🛠️ Getting Started

### Prerequisites

- **Operating System**: Windows 10/11 (64-bit)
- **.NET Runtime**: .NET 8.0 or higher
- **Database**: SQL Server 2019 or later (SQL Server 2025 recommended)
- **Memory**: Minimum 4 GB RAM (8 GB recommended)
- **Disk Space**: 500 MB for application + database storage

### Installation

1. **Clone the Repository**
   ```bash
   git clone https://github.com/yourusername/storix.git
   cd storix
   ```

2. **Database Setup**
   ```bash
   # Run database creation script
   sqlcmd -S your_server -i Storix.Database/Schema/CreateDatabase.sql
   
   # Apply migrations
   sqlcmd -S your_server -d StorixDB -i Storix.Database/Migrations/
   
   # Seed initial data (optional)
   sqlcmd -S your_server -d StorixDB -i Storix.Database/SeedData/
   ```

3. **Configuration**
   
   Update `appsettings.json` with your database connection:
   ```json
   {
     "ConnectionStrings": {
       "StorixDatabase": "Server=your_server;Database=StorixDB;Integrated Security=true;TrustServerCertificate=true"
     }
   }
   ```

4. **Build & Run**
   ```bash
   # Restore NuGet packages
   dotnet restore
   
   # Build the solution
   dotnet build --configuration Release
   
   # Run the application
   dotnet run --project Storix.Presentation
   ```

---

## 🎯 Usage

### First-Time Setup

1. **Initial Login**: Use default administrator credentials (change immediately)
   - Username: `admin`
   - Password: `admin123`

2. **Configure Locations**: Set up your warehouses and store locations
3. **Import Products**: Use CSV import or add products manually
4. **Set Up Suppliers**: Register your vendor information
5. **Configure Alerts**: Set reorder levels for automatic low-stock notifications

### Common Workflows

#### Creating a Sales Order
1. Navigate to **Orders → Sales Orders**
2. Click **Create Sale** quick action
3. Select customer and add products
4. Review and confirm order
5. Track fulfillment status

#### Stock Transfer
1. Go to **Quick Actions → Transfer Stock**
2. Select source and destination locations
3. Choose products and quantities
4. Confirm transfer
5. System automatically updates inventory levels

#### Generating Reports
1. Access **Reports** from sidebar
2. Select report type (Sales, Inventory, Purchase)
3. Set date range and filters
4. Export to PDF or Excel

---

## 🔐 Security Features

- **Role-based Access Control (RBAC)**: Granular permissions management
- **Audit Logging**: Complete transaction history tracking
- **Data Encryption**: Sensitive data encryption at rest
- **SQL Injection Protection**: Parameterized queries throughout
- **Session Management**: Secure session handling with timeout
- **Password Policies**: Enforced strong password requirements

---

## 🧪 Testing

```bash
# Run unit tests
dotnet test Storix.Tests.Unit

# Run integration tests
dotnet test Storix.Tests.Integration

# Generate code coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=./coverage
```

**Test Coverage**
- Unit Tests: Business logic and validation
- Integration Tests: Database operations and repositories
- UI Tests: ViewModel behavior and commands

---

## 📈 Future Enhancements

### Planned Features
- ✅ Advanced Reporting Module with customizable dashboards
- ✅ Financial Management integration (invoicing, payments)
- ✅ Barcode scanning support
- ✅ Multi-currency transactions
- ✅ Mobile companion app
- ✅ API for third-party integrations
- ✅ Advanced analytics and forecasting
- ✅ Multi-tenant support

### Extensibility
The modular architecture allows for easy extension:
- **Plugin System**: Add custom modules without modifying core code
- **Event Hooks**: Subscribe to business events for custom workflows
- **Custom Reports**: Build reports using template engine
- **API Integration**: RESTful API for external system integration

---

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Code Standards
- Follow C# coding conventions
- Write unit tests for new features
- Document public APIs with XML comments
- Keep commits atomic and well-described

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👨‍💻 Author

**Your Name**
- GitHub: [@yourusername](https://github.com/yourusername)
- LinkedIn: [Your Profile](https://linkedin.com/in/yourprofile)
- Email: your.email@example.com

---

## 🙏 Acknowledgments

- Built with [WPF](https://github.com/dotnet/wpf)
- Data access powered by [Dapper](https://github.com/DapperLib/Dapper)
- Icons from [Lucide Icons](https://lucide.dev/)
- UI inspiration from modern enterprise applications

---

## 📞 Support

For issues, questions, or feature requests:
- Open an [Issue](https://github.com/yourusername/storix/issues)
- Check the [Wiki](https://github.com/yourusername/storix/wiki)
- Join our [Discord Community](https://discord.gg/storix)

---

<p align="center">Made with ❤️ for inventory management professionals</p>
<p align="center">⭐ Star this repo if you find it helpful!</p>
