using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Domain;
using MiniErp.Api.Security;

namespace MiniErp.Api.Data;

public static class DataSeeder
{
    public const string DemoPassword = "enterprise-demo";

    public static async Task SeedAsync(ErpDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

        var users = new[]
        {
            BuildUser("Victor Siqueira", "victor.siqueira@enterprise.dev", UserRole.Administrator, now),
            BuildUser("Marina Costa", "marina.costa@enterprise.dev", UserRole.Manager, now),
            BuildUser("Rafael Lima", "rafael.lima@enterprise.dev", UserRole.Analyst, now)
        };

        var customers = new[]
        {
            new Customer { Name = "Northwind Manufacturing", TaxId = "NW-102938", Segment = "Manufacturing", CreditLimit = 450000, CreatedAt = now.AddMonths(-9) },
            new Customer { Name = "Contoso Logistics", TaxId = "CL-485920", Segment = "Supply Chain", CreditLimit = 620000, CreatedAt = now.AddMonths(-7) },
            new Customer { Name = "Fabrikam Retail", TaxId = "FR-330219", Segment = "Retail", CreditLimit = 310000, CreatedAt = now.AddMonths(-5) },
            new Customer { Name = "A. Datum Energy", TaxId = "AD-761042", Segment = "Energy", CreditLimit = 780000, CreatedAt = now.AddMonths(-11) },
            new Customer { Name = "Tailspin Health", TaxId = "TH-904120", Segment = "Healthcare", Status = CustomerStatus.OnHold, CreditLimit = 260000, CreatedAt = now.AddMonths(-3) }
        };

        var products = new[]
        {
            new Product { Sku = "ERP-SVC-001", Name = "ERP implementation sprint", Category = "Services", UnitPrice = 18000, ReorderPoint = 10 },
            new Product { Sku = "DYN-CON-010", Name = "Dynamics connector pack", Category = "Integrations", UnitPrice = 12500, ReorderPoint = 8 },
            new Product { Sku = "BI-DAT-204", Name = "Analytics data mart", Category = "Analytics", UnitPrice = 22600, ReorderPoint = 6 },
            new Product { Sku = "SUP-AUT-115", Name = "Warehouse automation kit", Category = "Operations", UnitPrice = 34800, ReorderPoint = 4 },
            new Product { Sku = "FIN-CLO-330", Name = "Finance close accelerator", Category = "Finance", UnitPrice = 16400, ReorderPoint = 7 },
            new Product { Sku = "AI-ERP-700", Name = "ERP AI assistant bundle", Category = "AI", UnitPrice = 41000, ReorderPoint = 3 }
        };

        var inventory = new[]
        {
            Inventory(products[0], "Main Warehouse", 36, 5, now),
            Inventory(products[1], "Main Warehouse", 24, 4, now),
            Inventory(products[2], "Data Center", 18, 2, now),
            Inventory(products[3], "Main Warehouse", 7, 3, now),
            Inventory(products[4], "Finance Hub", 14, 1, now),
            Inventory(products[5], "Innovation Lab", 6, 2, now)
        };

        var orders = new[]
        {
            BuildOrder("SO-2026-0001", customers[0], now.AddDays(-36), now.AddDays(-21), products[1], 3, products[4], 2),
            BuildOrder("SO-2026-0002", customers[1], now.AddDays(-24), now.AddDays(-10), products[3], 2, products[2], 1),
            BuildOrder("SO-2026-0003", customers[3], now.AddDays(-11), now.AddDays(7), products[5], 1, products[0], 2)
        };

        var invoices = new[]
        {
            BuildInvoice("INV-2026-0001", orders[0], InvoiceStatus.Paid, now.AddDays(-30), now.AddDays(-5)),
            BuildInvoice("INV-2026-0002", orders[1], InvoiceStatus.Open, now.AddDays(-20), null),
            BuildInvoice("INV-2026-0003", orders[2], InvoiceStatus.Open, now.AddDays(-8), null)
        };

        var movements = inventory.Select(item => new StockMovement
        {
            Product = item.Product,
            Type = StockMovementType.Receipt,
            Quantity = item.QuantityOnHand,
            Reference = "OPENING-BALANCE",
            Warehouse = item.Warehouse,
            OccurredAt = now.AddMonths(-1)
        });

        await db.Users.AddRangeAsync(users, cancellationToken);
        await db.Customers.AddRangeAsync(customers, cancellationToken);
        await db.Products.AddRangeAsync(products, cancellationToken);
        await db.InventoryItems.AddRangeAsync(inventory, cancellationToken);
        await db.SalesOrders.AddRangeAsync(orders, cancellationToken);
        await db.Invoices.AddRangeAsync(invoices, cancellationToken);
        await db.StockMovements.AddRangeAsync(movements, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static UserAccount BuildUser(string name, string email, UserRole role, DateTimeOffset now)
    {
        var salt = PasswordHasher.CreateSalt();
        return new UserAccount
        {
            Name = name,
            Email = email,
            Role = role,
            PasswordSalt = salt,
            PasswordHash = PasswordHasher.Hash(DemoPassword, salt),
            CreatedAt = now
        };
    }

    private static InventoryItem Inventory(Product product, string warehouse, int onHand, int reserved, DateTimeOffset now) =>
        new()
        {
            Product = product,
            Warehouse = warehouse,
            QuantityOnHand = onHand,
            QuantityReserved = reserved,
            UpdatedAt = now
        };

    private static SalesOrder BuildOrder(
        string number,
        Customer customer,
        DateTimeOffset orderDate,
        DateTimeOffset requiredDate,
        Product firstProduct,
        int firstQuantity,
        Product secondProduct,
        int secondQuantity)
    {
        var order = new SalesOrder
        {
            Number = number,
            Customer = customer,
            Status = OrderStatus.Confirmed,
            OrderDate = orderDate,
            RequiredDate = requiredDate
        };

        order.Lines.Add(BuildLine(order, firstProduct, firstQuantity));
        order.Lines.Add(BuildLine(order, secondProduct, secondQuantity));
        order.Subtotal = order.Lines.Sum(line => line.LineTotal);
        order.Tax = decimal.Round(order.Subtotal * 0.08m, 2);
        order.Total = order.Subtotal + order.Tax;
        return order;
    }

    private static SalesOrderLine BuildLine(SalesOrder order, Product product, int quantity) =>
        new()
        {
            SalesOrder = order,
            Product = product,
            Quantity = quantity,
            UnitPrice = product.UnitPrice,
            LineTotal = product.UnitPrice * quantity
        };

    private static Invoice BuildInvoice(
        string number,
        SalesOrder order,
        InvoiceStatus status,
        DateTimeOffset issuedAt,
        DateTimeOffset? paidAt) =>
        new()
        {
            Number = number,
            SalesOrder = order,
            Status = status,
            IssuedAt = issuedAt,
            DueAt = issuedAt.AddDays(30),
            PaidAt = paidAt,
            Amount = order.Total
        };
}
