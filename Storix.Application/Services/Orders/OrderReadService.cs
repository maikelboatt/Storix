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
    ///     Service responsible for order read operations
    /// </summary>
    public class OrderReadService(
        IOrderRepository orderRepository,
        IOrderItemManager orderItemManager,
        IOrderStore orderStore,
        IDatabaseErrorHandlerService databaseErrorHandlerService,
        ILogger<OrderReadService> logger ):IOrderReadService
    {
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
                await databaseErrorHandlerService.HandleDatabaseOperationAsync(orderRepository.GetAllAsync, "Retrieving all orders");

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

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByTypeAsync( OrderType type )
        {
            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByTypeAsync(type),
                "$Retrieving orders by type {type}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {OrderCount} {Type} orders", result.Value.Count(), type);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders by type: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<SalesOrderListDto>>> GetSalesOrderListAsync()
        {
            DatabaseResult<IEnumerable<OrderDto>> result = await GetOrdersByTypeAsync(OrderType.Sale);

            if (result is { IsSuccess: true, Value: not null })
            {
                List<SalesOrderListDto> salesOrderListDtos = [];

                foreach (OrderDto dto in result.Value)
                {
                    // Await the async call to get total amount
                    DatabaseResult<decimal> totalResult = await orderItemManager.GetOrderTotalValueAsync(dto.OrderId);
                    decimal totalAmount = totalResult.IsSuccess
                        ? totalResult.Value
                        : 0m;

                    salesOrderListDtos.Add(
                        new SalesOrderListDto
                        {
                            OrderId = dto.OrderId,
                            CustomerName = orderStore.GetCustomerName(dto.CustomerId ?? 0),
                            OrderDate = dto.OrderDate,
                            Status = dto.Status,
                            TotalAmount = totalAmount,
                            DeliveryDate = dto.DeliveryDate,
                            Notes = dto.Notes,
                            CreatedBy = dto.CreatedBy
                        });
                }

                orderStore.InitializeSalesOrderList(salesOrderListDtos);

                logger.LogInformation(
                    "Successfully mapped {SalesOrderCount} sales orders to SalesOrderListDto",
                    salesOrderListDtos.Count);

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
                    // Await the async call to get total amount
                    DatabaseResult<decimal> totalResult = await orderItemManager.GetOrderTotalValueAsync(dto.OrderId);
                    decimal totalAmount = totalResult.IsSuccess
                        ? totalResult.Value
                        : 0m;

                    purchaseOrderListDtos.Add(
                        new PurchaseOrderListDto
                        {
                            OrderId = dto.OrderId,
                            SupplierName = orderStore.GetSupplierName(dto.SupplierId ?? 0),
                            OrderDate = dto.OrderDate,
                            Status = dto.Status,
                            TotalAmount = totalAmount,
                            DeliveryDate = dto.DeliveryDate,
                            Notes = dto.Notes,
                            CreatedBy = dto.CreatedBy
                        });
                }

                orderStore.InitializePurchaseOrderList(purchaseOrderListDtos);

                logger.LogInformation(
                    "Successfully mapped {SalesOrderCount} sales orders to PurchaseOrderListDto",
                    purchaseOrderListDtos.Count);

                return DatabaseResult<IEnumerable<PurchaseOrderListDto>>.Success(purchaseOrderListDtos);
            }

            logger.LogWarning("Failed to retrieve sales orders for list: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<PurchaseOrderListDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> GetOrdersByStatusAsync( OrderStatus status )
        {
            DatabaseResult<IEnumerable<Order>> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetByStatusAsync(status),
                "$Retrieving orders by status {status}");

            if (result is { IsSuccess: true, Value: not null })
            {
                logger.LogInformation("Successfully retrieved {OrderCount} {Status} orders", result.Value.Count(), status);
                return DatabaseResult<IEnumerable<OrderDto>>.Success(result.Value.ToDto());
            }

            logger.LogWarning("Failed to retrieve orders by status: {ErrorMessage}", result.ErrorMessage);
            return DatabaseResult<IEnumerable<OrderDto>>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

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
                logger.LogInformation(
                    "Successfully retrieved {OrderCount} orders for supplier {SupplierId}",
                    result.Value.Count(),
                    supplierId);
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
                logger.LogInformation(
                    "Successfully retrieved {OrderCount} orders for customer {CustomerId}",
                    result.Value.Count(),
                    customerId);
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

        public async Task<DatabaseResult<IEnumerable<OrderDto>>> SearchOrdersAsync( string? searchTerm = null,
            OrderType? type = null,
            OrderStatus? status = null,
            int? supplierId = null,
            int? customerId = null,
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

        public async Task<DatabaseResult<OrderStatisticsDto?>> GetOrderStatisticsAsync( DateTime startDate, DateTime endDate )
        {
            DatabaseResult<OrderStatisticsDto?> result = await databaseErrorHandlerService.HandleDatabaseOperationAsync(
                () => orderRepository.GetOrderStatisticsAsync(startDate, endDate),
                $"Getting order statistics from {startDate:d} to {endDate:d}");

            return result.IsSuccess
                ? DatabaseResult<OrderStatisticsDto?>.Success(result.Value)
                : DatabaseResult<OrderStatisticsDto?>.Failure(result.ErrorMessage!, result.ErrorCode);
        }
    }
}
