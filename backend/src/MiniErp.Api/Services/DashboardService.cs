using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Contracts;
using MiniErp.Api.Data;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Services;

public sealed class DashboardService(ErpDbContext db)
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await db.Invoices
            .Include(invoice => invoice.SalesOrder)
            .ThenInclude(order => order.Customer)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var orders = await db.SalesOrders
            .Include(order => order.Customer)
            .Include(order => order.Lines)
            .ThenInclude(line => line.Product)
            .AsNoTracking()
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync(cancellationToken);

        var inventory = await db.InventoryItems
            .Include(item => item.Product)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var revenue = invoices
            .Where(invoice => invoice.Status is InvoiceStatus.Open or InvoiceStatus.Paid)
            .Sum(invoice => invoice.Amount);

        var receivables = invoices
            .Where(invoice => invoice.Status is InvoiceStatus.Open or InvoiceStatus.Overdue)
            .Sum(invoice => invoice.Amount);

        var lowStock = inventory.Count(item => item.AvailableQuantity <= item.Product.ReorderPoint);
        var openOrders = orders.Count(order => order.Status is OrderStatus.Draft or OrderStatus.Confirmed);

        var monthlyRevenue = invoices
            .GroupBy(invoice => invoice.IssuedAt.ToString("MMM"))
            .Select(group => new RevenuePointDto(
                group.Key,
                group.Where(invoice => invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Open).Sum(invoice => invoice.Amount),
                group.Where(invoice => invoice.Status is InvoiceStatus.Open or InvoiceStatus.Overdue).Sum(invoice => invoice.Amount)))
            .ToArray();

        var categoryRevenue = orders
            .SelectMany(order => order.Lines)
            .GroupBy(line => line.Product.Category)
            .Select(group => new CategoryRevenueDto(group.Key, group.Sum(line => line.LineTotal)))
            .OrderByDescending(item => item.Revenue)
            .ToArray();

        var risks = new[]
        {
            new RiskItemDto("Open receivables", receivables > 100000 ? 32 : 18, "amber"),
            new RiskItemDto("Low stock", Math.Min(30, lowStock * 6), "rose"),
            new RiskItemDto("Customers on hold", await db.Customers.CountAsync(customer => customer.Status == CustomerStatus.OnHold, cancellationToken) * 10, "blue"),
            new RiskItemDto("Confirmed orders", openOrders * 5, "green")
        };

        var kpis = new[]
        {
            new KpiDto("Revenue", Currency(revenue), "+14.2%", "green"),
            new KpiDto("Receivables", Currency(receivables), "-3.8%", "amber"),
            new KpiDto("Open orders", openOrders.ToString(), "+5", "blue"),
            new KpiDto("Low stock", lowStock.ToString(), lowStock == 0 ? "0" : "+1", "rose")
        };

        var recentOrders = orders
            .Take(5)
            .Select(order => new RecentOrderDto(
                order.Number,
                order.Customer.Name,
                order.Status,
                order.Total,
                order.RequiredDate))
            .ToArray();

        return new DashboardSummaryDto(kpis, monthlyRevenue, categoryRevenue, risks, recentOrders);
    }

    private static string Currency(decimal value) => string.Create(
        System.Globalization.CultureInfo.InvariantCulture,
        $"${value / 1000:0.#}K");
}
