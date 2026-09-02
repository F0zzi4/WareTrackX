using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Contracts;
using MiniErp.Api.Data;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Services;

public sealed class OrderService(ErpDbContext db)
{
    public async Task<ServiceResult<SalesOrderDto>> CreateAsync(
        CreateSalesOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0)
        {
            return ServiceResult<SalesOrderDto>.Invalid("OrderLinesRequired", "At least one order line is required.");
        }

        var customer = await db.Customers.FindAsync([request.CustomerId], cancellationToken);
        if (customer is null)
        {
            return ServiceResult<SalesOrderDto>.NotFound("CustomerNotFound", "Customer was not found.");
        }

        if (customer.Status != CustomerStatus.Active)
        {
            return ServiceResult<SalesOrderDto>.Invalid("CustomerBlocked", "Customer must be active to receive new orders.");
        }

        var productIds = request.Lines.Select(line => line.ProductId).Distinct().ToArray();
        var products = await db.Products
            .Include(product => product.InventoryItems)
            .Where(product => productIds.Contains(product.Id) && product.IsActive)
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        if (products.Count != productIds.Length)
        {
            return ServiceResult<SalesOrderDto>.Invalid("ProductUnavailable", "One or more products are unavailable.");
        }

        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0)
            {
                return ServiceResult<SalesOrderDto>.Invalid("InvalidQuantity", "Line quantities must be greater than zero.");
            }

            var available = products[line.ProductId].InventoryItems.Sum(item => item.AvailableQuantity);
            if (available < line.Quantity)
            {
                return ServiceResult<SalesOrderDto>.Invalid("InsufficientStock", $"Product {products[line.ProductId].Sku} has insufficient stock.");
            }
        }

        var sequence = await db.SalesOrders.CountAsync(cancellationToken) + 1;
        var order = new SalesOrder
        {
            Number = $"SO-{DateTimeOffset.UtcNow:yyyy}-{sequence:0000}",
            Customer = customer,
            Status = OrderStatus.Confirmed,
            OrderDate = DateTimeOffset.UtcNow,
            RequiredDate = request.RequiredDate
        };

        foreach (var lineRequest in request.Lines)
        {
            var product = products[lineRequest.ProductId];
            var lineTotal = product.UnitPrice * lineRequest.Quantity;

            order.Lines.Add(new SalesOrderLine
            {
                SalesOrder = order,
                Product = product,
                Quantity = lineRequest.Quantity,
                UnitPrice = product.UnitPrice,
                LineTotal = lineTotal
            });

            ReserveStock(product, lineRequest.Quantity, order.Number);
        }

        order.Subtotal = order.Lines.Sum(line => line.LineTotal);
        order.Tax = decimal.Round(order.Subtotal * 0.08m, 2);
        order.Total = order.Subtotal + order.Tax;

        order.Invoice = new Invoice
        {
            Number = $"INV-{DateTimeOffset.UtcNow:yyyy}-{sequence:0000}",
            SalesOrder = order,
            Status = InvoiceStatus.Open,
            IssuedAt = DateTimeOffset.UtcNow,
            DueAt = DateTimeOffset.UtcNow.AddDays(30),
            Amount = order.Total
        };

        await db.SalesOrders.AddAsync(order, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var saved = await db.SalesOrders
            .Include(item => item.Customer)
            .Include(item => item.Lines)
            .ThenInclude(line => line.Product)
            .SingleAsync(item => item.Id == order.Id, cancellationToken);

        return ServiceResult<SalesOrderDto>.Ok(saved.ToDto());
    }

    private void ReserveStock(Product product, int quantity, string reference)
    {
        var remaining = quantity;

        foreach (var item in product.InventoryItems.OrderBy(item => item.AvailableQuantity))
        {
            if (remaining == 0)
            {
                break;
            }

            var reserved = Math.Min(item.AvailableQuantity, remaining);
            item.QuantityReserved += reserved;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            remaining -= reserved;

            db.StockMovements.Add(new StockMovement
            {
                Product = product,
                Type = StockMovementType.Reservation,
                Quantity = reserved,
                Reference = reference,
                Warehouse = item.Warehouse,
                OccurredAt = DateTimeOffset.UtcNow
            });
        }
    }
}

public sealed record ServiceResult<T>(
    T? Value,
    ApiError? Error,
    ServiceResultStatus Status)
{
    public static ServiceResult<T> Ok(T value) => new(value, null, ServiceResultStatus.Ok);

    public static ServiceResult<T> Invalid(string code, string message) =>
        new(default, new ApiError(code, message), ServiceResultStatus.Invalid);

    public static ServiceResult<T> NotFound(string code, string message) =>
        new(default, new ApiError(code, message), ServiceResultStatus.NotFound);
}

public enum ServiceResultStatus
{
    Ok,
    Invalid,
    NotFound
}
