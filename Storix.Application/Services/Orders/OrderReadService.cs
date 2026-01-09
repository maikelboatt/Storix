using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Storix.Application.Common;
using Storix.Application.Common.Errors;
using Storix.Application.DTO.Orders;
using Storix.Application.Enums;
using Storix.Application.Managers.Interfaces;
using Storix.Application.Repositories;
using Storix.Application.Services.Orders.Interfaces;
using Storix.Application.Stores.Orders;
using Storix.Domain.Enums;
using Storix.Domain.Models;

namespace Storix.Application.Services.Orders
{
    /// <summary>
    /// Service responsible for order read operations
    /// </summary>
    public class OrderReadService(
        IOrderRepository orderRepository,
        IOrderItemManager orderItemManager,
        IOrderStore orderStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<OrderReadService> logger ):IOrderReadService
    {
        #region Basic Read Operations

        public OrderDto? GetOrderById( int orderId )
        {
            if (orderId <= 0)
            {
                logger.LogWarning("Invalid order ID {OrderId} provided.", orderId);
                return null;
            }

            logger.LogInformation("Retrieving order with ID {OrderId} from store", orderId);
            return orderStore.GetById(orderId);
        }

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetAllOrdersAsync()
        {
            DatabaseResult<IEnumerable<Order>> result =
                await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                    orderRepository.GetAllAsync,
                    "Retrieving all orders");

            if (result is { IsSuccess: true, Value: not null })
            {
                IEnumerable<OrderDto> orderDtos = result.Value.ToDto();
                orderStore.Initialize(result.Value.ToList());
                logger.LogInformation("Successfully loaded {OrderCount} orders", result.Value.Count());
                return DatabaseResult<IEnumerable<OrderDto>>.Success(orderDtos);
            }

            logger.LogWarning("Failed to retrieve all orders: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region Query by Type and Status

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByTypeAsync( OrderType type )
        {
            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByTypeAsync(type),
                $"Retrieving orders by type {type}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {OrderCount} {Type} orders", result.Value.Count(), type);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders by type: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByStatusAsync( OrderStatus status )
        {
            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByStatusAsync(status),
                $"Retrieving orders by status {status}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {OrderCount} {Status} orders", result.Value.Count(), status);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders by status: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region Sales and Purchase Order Lists

        public async Task<DatabaseResult<IEnumerable<SalesOrderListDto>>> GetSalesOrderListAsync()
        {
            DatabaseResult<IEnumerable<OrderDto>> result = await GetOrdersByTypeAsync(OrderType.Sale);

            if (result is { IsSuccess: true, Value: not null })
            {
                List<SalesOrderListDto> salesOrderListDtos = [];

                foreach (OrderDto dto in result.Value)
                {
                    DatabaseResult<decimal> totalResult = await orderItemManager.GetOrderTotalValueAsync(dto.OrderId);
                    decimal totalAmount = totalResult.IsSuccess
                        ? totalResult.Value
                        : 0m;

                    salesOrderListDtos.Add(
                        new SalesOrderListDto
                        {
                            OrderId = dto.OrderId,
                            CustomerName = orderStore.GetCustomerName(dto.CustomerId ?? 0),
                            LocationName = orderStore.GetLocationName(dto.LocationId),
                            OrderDate = dto.OrderDate,
                            Status = dto.Status,
                            TotalAmount = totalAmount,
                            DeliveryDate = dto.DeliveryDate,
                            Notes = dto.Notes,
                            CreatedBy = dto.CreatedBy
                        });
                }

                orderStore.InitializeSalesOrderList(salesOrderListDtos);
                logger.LogInformation("Successfully mapped {SalesOrderCount} sales orders to SalesOrderListDto", salesOrderListDtos.Count);
                return DatabaseResult<IEnumerable<SalesOrderListDto>>.Success(salesOrderListDtos);
            }

            logger.LogWarning("Failed to retrieve sales orders for list: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<SalesOrderListDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<PurchaseOrderListDto>>> GetPurchaseOrderListAsync()
        {
            DatabaseResult<IEnumerable<OrderDto>> result = await GetOrdersByTypeAsync(OrderType.Purchase);

            if (result is { IsSuccess: true, Value: not null })
            {
                List<PurchaseOrderListDto> purchaseOrderListDtos = [];

                foreach (OrderDto dto in result.Value)
                {
                    DatabaseResult<decimal> totalResult = await orderItemManager.GetOrderTotalValueAsync(dto.OrderId);
                    decimal totalAmount = totalResult.IsSuccess
                        ? totalResult.Value
                        : 0m;

                    purchaseOrderListDtos.Add(
                        new PurchaseOrderListDto
                        {
                            OrderId = dto.OrderId,
                            SupplierName = orderStore.GetSupplierName(dto.SupplierId ?? 0),
                            LocationName = orderStore.GetLocationName(dto.LocationId),
                            OrderDate = dto.OrderDate,
                            Status = dto.Status,
                            TotalAmount = totalAmount,
                            DeliveryDate = dto.DeliveryDate,
                            Notes = dto.Notes,
                            CreatedBy = dto.CreatedBy
                        });
                }

                orderStore.InitializePurchaseOrderList(purchaseOrderListDtos);
                logger.LogInformation("Successfully mapped {PurchaseOrderCount} purchase orders to PurchaseOrderListDto", purchaseOrderListDtos.Count);
                return DatabaseResult<IEnumerable<PurchaseOrderListDto>>.Success(purchaseOrderListDtos);
            }

            logger.LogWarning("Failed to retrieve purchase orders for list: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<PurchaseOrderListDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region Query by Supplier, Customer, and Date

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersBySupplierAsync( int supplierId )
        {
            if (supplierId <= 0)
            {
                logger.LogWarning("Invalid supplier ID {SupplierId} provided", supplierId);
                return DatabaseResult<IEnumerable<OrderDto>>.Failure(
                    "Supplier ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetBySupplierAsync(supplierId),
                $"Retrieving orders for supplier {supplierId}");

            if (result.IsSuccess && result.Value != null)
            {
                logger.LogInformation("Successfully retrieved {OrderCount} orders for supplier {SupplierId}", result.Value.Count(), supplierId);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders for supplier: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByCustomerAsync( int customerId )
        {
            if (customerId <= 0)
            {
                logger.LogWarning("Invalid customer ID {CustomerId} provided", customerId);
                return DatabaseResult<IEnumerable<OrderDto>>.Failure(
                    "Customer ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByCustomerAsync(customerId),
                $"Retrieving orders for customer {customerId}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {OrderCount} orders for customer {CustomerId}", result.Value.Count(), customerId);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders for customer: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByDateRangeAsync( DateTime startDate, DateTime endDate )
        {
            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByDateRangeAsync(startDate, endDate),
                $"Retrieving orders from {startDate:d} to {endDate:d}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {OrderCount} orders in date range", result.Value.Count());
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders by date range: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region ✅ NEW: Location-Based Queries

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<IEnumerable<OrderDto>>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByLocationAsync(locationId),
                $"Retrieving orders for location {locationId}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {OrderCount} orders for location {LocationId}", result.Value.Count(), locationId);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders for location: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationAndStatusAsync( int locationId, OrderStatus status )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<IEnumerable<OrderDto>>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByLocationIdAndStatusAsync(locationId, status),
                $"Retrieving {status} orders for location {locationId}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {OrderCount} {Status} orders for location {LocationId}",
                    result.Value.Count(),
                    status,
                    locationId);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders for location and status: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationAndTypeAsync( int locationId, OrderType type )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<IEnumerable<OrderDto>>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByLocationIdAndTypeAsync(locationId, type),
                $"Retrieving {type} orders for location {locationId}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {OrderCount} {Type} orders for location {LocationId}",
                    result.Value.Count(),
                    type,
                    locationId);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders for location and type: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetActiveOrdersByLocationAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<IEnumerable<OrderDto>>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetActiveOrdersByLocationAsync(locationId),
                $"Retrieving active orders for location {locationId}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {OrderCount} active orders for location {LocationId}",
                    result.Value.Count(),
                    locationId);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve active orders for location: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationAndDateRangeAsync(
            int locationId,
            DateTime startDate,
            DateTime endDate )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<IEnumerable<OrderDto>>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByLocationAndDateRangeAsync(locationId, startDate, endDate),
                $"Retrieving orders for location {locationId} from {startDate:d} to {endDate:d}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {OrderCount} orders for location {LocationId} in date range",
                    result.Value.Count(),
                    locationId);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders for location and date range: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOverdueOrdersByLocationAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<IEnumerable<OrderDto>>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetOverdueOrdersByLocationAsync(locationId),
                $"Retrieving overdue orders for location {locationId}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {OrderCount} overdue orders for location {LocationId}",
                    result.Value.Count(),
                    locationId);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve overdue orders for location: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<SalesOrderListDto>>> GetSalesOrdersByLocationAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<IEnumerable<SalesOrderListDto>>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<OrderDto>> ordersResult = await GetOrdersByLocationAndTypeAsync(locationId, OrderType.Sale);

            if (ordersResult is { IsSuccess: true, Value: not null })
            {
                List<SalesOrderListDto> salesOrderListDtos = [];

                foreach (OrderDto dto in ordersResult.Value)
                {
                    DatabaseResult<decimal> totalResult = await orderItemManager.GetOrderTotalValueAsync(dto.OrderId);
                    decimal totalAmount = totalResult.IsSuccess
                        ? totalResult.Value
                        : 0m;

                    salesOrderListDtos.Add(
                        new SalesOrderListDto
                        {
                            OrderId = dto.OrderId,
                            CustomerName = orderStore.GetCustomerName(dto.CustomerId ?? 0),
                            LocationName = orderStore.GetLocationName(dto.LocationId),
                            OrderDate = dto.OrderDate,
                            Status = dto.Status,
                            TotalAmount = totalAmount,
                            DeliveryDate = dto.DeliveryDate,
                            Notes = dto.Notes,
                            CreatedBy = dto.CreatedBy
                        });
                }

                logger.LogInformation(
                    "Successfully mapped {SalesOrderCount} sales orders for location {LocationId}",
                    salesOrderListDtos.Count,
                    locationId);
                return DatabaseResult<IEnumerable<SalesOrderListDto>>.Success(salesOrderListDtos);
            }

            logger.LogWarning("Failed to retrieve sales orders for location: {ErrorMessage}", ordersResult.ErrorMessage);
            return DatabaseResult<IEnumerable<SalesOrderListDto>>.Failure(ordersResult.ErrorMessage!, ordersResult.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<PurchaseOrderListDto>>> GetPurchaseOrdersByLocationAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<IEnumerable<PurchaseOrderListDto>>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<OrderDto>> ordersResult = await GetOrdersByLocationAndTypeAsync(locationId, OrderType.Purchase);

            if (ordersResult is { IsSuccess: true, Value: not null })
            {
                List<PurchaseOrderListDto> purchaseOrderListDtos = [];

                foreach (OrderDto dto in ordersResult.Value)
                {
                    DatabaseResult<decimal> totalResult = await orderItemManager.GetOrderTotalValueAsync(dto.OrderId);
                    decimal totalAmount = totalResult.IsSuccess
                        ? totalResult.Value
                        : 0m;

                    purchaseOrderListDtos.Add(
                        new PurchaseOrderListDto
                        {
                            OrderId = dto.OrderId,
                            SupplierName = orderStore.GetSupplierName(dto.SupplierId ?? 0),
                            LocationName = orderStore.GetLocationName(dto.LocationId),
                            OrderDate = dto.OrderDate,
                            Status = dto.Status,
                            TotalAmount = totalAmount,
                            DeliveryDate = dto.DeliveryDate,
                            Notes = dto.Notes,
                            CreatedBy = dto.CreatedBy
                        });
                }

                logger.LogInformation(
                    "Successfully mapped {PurchaseOrderCount} purchase orders for location {LocationId}",
                    purchaseOrderListDtos.Count,
                    locationId);
                return DatabaseResult<IEnumerable<PurchaseOrderListDto>>.Success(purchaseOrderListDtos);
            }

            logger.LogWarning("Failed to retrieve purchase orders for location: {ErrorMessage}", ordersResult.ErrorMessage);
            return DatabaseResult<IEnumerable<PurchaseOrderListDto>>.Failure(ordersResult.ErrorMessage!, ordersResult.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByLocationIdsAsync( IEnumerable<int> locationIds )
        {
            if (locationIds == null || !locationIds.Any())
            {
                logger.LogWarning("No location IDs provided");
                return DatabaseResult<IEnumerable<OrderDto>>.Failure(
                    "At least one location ID must be provided.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByLocationIdsAsync(locationIds),
                $"Retrieving orders for {locationIds.Count()} locations");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation(
                    "Successfully retrieved {OrderCount} orders for {LocationCount} locations",
                    result.Value.Count(),
                    locationIds.Count());
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders for multiple locations: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region Overdue Orders

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOverdueOrdersAsync()
        {
            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                orderRepository.GetOverdueOrdersAsync,
                "Retrieving overdue orders");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {OverdueOrderCount} overdue orders", result.Value.Count());
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve overdue orders: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region Query by Creator

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrderByCreatedByAsync( int createdBy )
        {
            if (createdBy <= 0)
            {
                logger.LogWarning("Invalid order user ID {CreatedBy} provided", createdBy);
                return DatabaseResult<IEnumerable<OrderDto>>.Failure("Invalid user Id", DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByCreatedByAsync(createdBy),
                "Retrieving orders for created by");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {OrderCount} orders for created by {CreatedBy}", result.Value.Count(), createdBy);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders for created by: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region Search and Pagination

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> SearchOrdersAsync(
            string? searchTerm = null,
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
            int locationId = 0,
            DateTime? startDate = null,
            DateTime? endDate = null )
        {
            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.SearchAsync(
                    searchTerm,
                    type,
                    status,
                    supplierId,
                    customerId,
                    locationId,
                    startDate,
                    endDate),
                "Searching orders");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Search returned {OrderCount} orders", result.Value.Count());
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to search orders: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersPagedAsync( int pageNumber, int pageSize )
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                string errorMsg = pageNumber <= 0
                    ? "Page number must be positive"
                    : "Page size must be positive";
                logger.LogWarning("Invalid pagination parameters: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
                return DatabaseResult<IEnumerable<OrderDto>>.Failure(errorMsg, DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetPagedAsync(pageNumber, pageSize),
                $"Getting orders page {pageNumber} with size {pageSize}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved page {PageNumber} of orders {OrderCount} items", pageNumber, result.Value.Count());
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders page: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region Count Operations

        public async Task<DatabaseResult<int>> GetTotalOrderCountsAsync()
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                orderRepository.GetTotalCountAsync,
                "Getting total order counts");

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetOrderCountsByTypeAsync( OrderType type )
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetCountByTypeAsync(type),
                "Getting order count by type");

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetOrderCountByStatusAsync( OrderStatus status )
        {
            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetCountByStatusAsync(status),
                "Getting order count of status");

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<int>> GetOrderCountByLocationAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<int>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<int> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetOrderCountByLocationAsync(locationId),
                $"Getting order count for location {locationId}");

            return result.IsSuccess
                ? DatabaseResult<int>.Success(result.Value)
                : DatabaseResult<int>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<Dictionary<int, int>>> GetOrderCountsByLocationAsync()
        {
            DatabaseResult<Dictionary<int, int>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                orderRepository.GetOrderCountsByLocationAsync,
                "Getting order counts for all locations");

            return result.IsSuccess
                ? DatabaseResult<Dictionary<int, int>>.Success(result.Value)
                : DatabaseResult<Dictionary<int, int>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion

        #region Statistics

        public async Task<DatabaseResult<OrderStatisticsDto?>> GetOrderStatisticsAsync( DateTime startDate, DateTime endDate )
        {
            DatabaseResult<OrderStatisticsDto?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetOrderStatisticsAsync(startDate, endDate),
                $"Getting order statistics from {startDate:d} to {endDate:d}");

            return result.IsSuccess
                ? DatabaseResult<OrderStatisticsDto?>.Success(result.Value)
                : DatabaseResult<OrderStatisticsDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<decimal>> GetTotalRevenueByLocationAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<decimal>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<decimal> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetTotalRevenueByLocationAsync(locationId),
                $"Getting total revenue for location {locationId}");

            return result.IsSuccess
                ? DatabaseResult<decimal>.Success(result.Value)
                : DatabaseResult<decimal>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<Dictionary<OrderStatus, int>>> GetOrderStatusCountByLocationAsync( int locationId )
        {
            if (locationId <= 0)
            {
                logger.LogWarning("Invalid location ID {LocationId} provided", locationId);
                return DatabaseResult<Dictionary<OrderStatus, int>>.Failure(
                    "Location ID must be a positive integer.",
                    DatabaseErrorCode.InvalidInput);
            }

            DatabaseResult<Dictionary<OrderStatus, int>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetOrderStatusCountByLocationAsync(locationId),
                $"Getting order status distribution for location {locationId}");

            return result.IsSuccess
                ? DatabaseResult<Dictionary<OrderStatus, int>>.Success(result.Value)
                : DatabaseResult<Dictionary<OrderStatus, int>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        #endregion
    }
}
