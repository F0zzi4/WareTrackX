using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Contracts;
using MiniErp.Api.Data;
using MiniErp.Api.Services;

namespace MiniErp.Api.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_reserves_inventory_and_creates_invoice()
    {
        await using var db = ErpTestFactory.CreateDbContext(nameof(CreateAsync_reserves_inventory_and_creates_invoice));
        await DataSeeder.SeedAsync(db);

        var customer = await db.Customers.SingleAsync(item => item.Name == "Northwind Manufacturing");
        var product = await db.Products.Include(item => item.InventoryItems).SingleAsync(item => item.Sku == "DYN-CON-010");
        var beforeReserved = product.InventoryItems.Sum(item => item.QuantityReserved);

        var result = await new OrderService(db).CreateAsync(new CreateSalesOrderRequest(
            customer.Id,
            DateTimeOffset.UtcNow.AddDays(14),
            [new CreateSalesOrderLineRequest(product.Id, 2)]));

        var afterReserved = await db.InventoryItems
            .Where(item => item.ProductId == product.Id)
            .SumAsync(item => item.QuantityReserved);

        Assert.Equal(ServiceResultStatus.Ok, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(beforeReserved + 2, afterReserved);
        Assert.Contains(await db.Invoices.ToListAsync(), invoice => invoice.SalesOrderId == result.Value.Id);
    }

    [Fact]
    public async Task CreateAsync_rejects_on_hold_customer()
    {
        await using var db = ErpTestFactory.CreateDbContext(nameof(CreateAsync_rejects_on_hold_customer));
        await DataSeeder.SeedAsync(db);

        var customer = await db.Customers.SingleAsync(item => item.Name == "Tailspin Health");
        var product = await db.Products.SingleAsync(item => item.Sku == "ERP-SVC-001");

        var result = await new OrderService(db).CreateAsync(new CreateSalesOrderRequest(
            customer.Id,
            DateTimeOffset.UtcNow.AddDays(14),
            [new CreateSalesOrderLineRequest(product.Id, 1)]));

        Assert.Equal(ServiceResultStatus.Invalid, result.Status);
        Assert.Equal("CustomerBlocked", result.Error?.Code);
    }
}
