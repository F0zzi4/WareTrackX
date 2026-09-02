using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Contracts;
using MiniErp.Api.Data;
using MiniErp.Api.Domain;
using MiniErp.Api.Services;

namespace MiniErp.Api.Endpoints;

public static class MiniErpEndpoints
{
    public static void MapMiniErpApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").WithTags("Mini ERP");

        api.MapPost("/auth/login", LoginAsync)
            .AllowAnonymous()
            .WithName("Login");

        var secured = api.MapGroup("").RequireAuthorization();

        secured.MapGet("/dashboard", (DashboardService service, CancellationToken cancellationToken) =>
                service.GetSummaryAsync(cancellationToken))
            .WithName("GetDashboard");

        secured.MapGet("/customers", ListCustomersAsync)
            .WithName("ListCustomers");

        secured.MapPost("/customers", CreateCustomerAsync)
            .RequireAuthorization("ManagerAccess")
            .WithName("CreateCustomer");

        secured.MapPatch("/customers/{id:guid}/status", UpdateCustomerStatusAsync)
            .RequireAuthorization("ManagerAccess")
            .WithName("UpdateCustomerStatus");

        secured.MapGet("/products", ListProductsAsync)
            .WithName("ListProducts");

        secured.MapPost("/products", CreateProductAsync)
            .RequireAuthorization("ManagerAccess")
            .WithName("CreateProduct");

        secured.MapGet("/inventory", ListInventoryAsync)
            .WithName("ListInventory");

        secured.MapPost("/inventory/adjustments", AdjustInventoryAsync)
            .RequireAuthorization("ManagerAccess")
            .WithName("AdjustInventory");

        secured.MapGet("/orders", ListOrdersAsync)
            .WithName("ListOrders");

        secured.MapPost("/orders", CreateOrderAsync)
            .RequireAuthorization("ManagerAccess")
            .WithName("CreateOrder");

        secured.MapGet("/finance/invoices", ListInvoicesAsync)
            .WithName("ListInvoices");

        secured.MapPost("/finance/invoices/{id:guid}/mark-paid", MarkInvoicePaidAsync)
            .RequireAuthorization("AdministratorOnly")
            .WithName("MarkInvoicePaid");
    }

    private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>> LoginAsync(
        LoginRequest request,
        AuthService auth,
        CancellationToken cancellationToken)
    {
        var response = await auth.AuthenticateAsync(request, cancellationToken);
        return response is null ? TypedResults.Unauthorized() : TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<CustomerDto>>> ListCustomersAsync(
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var customers = await db.Customers
            .Include(customer => customer.Orders)
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .Select(customer => customer.ToDto())
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyCollection<CustomerDto>>(customers);
    }

    private static async Task<Results<Created<CustomerDto>, BadRequest<ApiError>>> CreateCustomerAsync(
        CreateCustomerRequest request,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        if (Blank(request.Name) || Blank(request.TaxId) || Blank(request.Segment) || request.CreditLimit < 0)
        {
            return TypedResults.BadRequest(new ApiError("InvalidCustomer", "Customer name, tax id, segment and credit limit are required."));
        }

        var exists = await db.Customers.AnyAsync(customer => customer.TaxId == request.TaxId, cancellationToken);
        if (exists)
        {
            return TypedResults.BadRequest(new ApiError("DuplicateTaxId", "Customer tax id already exists."));
        }

        var customer = new Customer
        {
            Name = request.Name.Trim(),
            TaxId = request.TaxId.Trim(),
            Segment = request.Segment.Trim(),
            CreditLimit = request.CreditLimit
        };

        await db.Customers.AddAsync(customer, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/api/customers/{customer.Id}", customer.ToDto());
    }

    private static async Task<Results<Ok<CustomerDto>, NotFound, BadRequest<ApiError>>> UpdateCustomerStatusAsync(
        Guid id,
        UpdateCustomerStatusRequest request,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var customer = await db.Customers
            .Include(item => item.Orders)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (customer is null)
        {
            return TypedResults.NotFound();
        }

        if (!Enum.IsDefined(request.Status))
        {
            return TypedResults.BadRequest(new ApiError("InvalidStatus", "Customer status is invalid."));
        }

        customer.Status = request.Status;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(customer.ToDto());
    }

    private static async Task<Ok<IReadOnlyCollection<ProductDto>>> ListProductsAsync(
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var products = await db.Products
            .Include(product => product.InventoryItems)
            .AsNoTracking()
            .OrderBy(product => product.Sku)
            .Select(product => product.ToDto())
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyCollection<ProductDto>>(products);
    }

    private static async Task<Results<Created<ProductDto>, BadRequest<ApiError>>> CreateProductAsync(
        CreateProductRequest request,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        if (Blank(request.Sku) || Blank(request.Name) || Blank(request.Category) || Blank(request.Warehouse)
            || request.UnitPrice <= 0 || request.ReorderPoint < 0 || request.OpeningStock < 0)
        {
            return TypedResults.BadRequest(new ApiError("InvalidProduct", "Product fields are invalid."));
        }

        var exists = await db.Products.AnyAsync(product => product.Sku == request.Sku, cancellationToken);
        if (exists)
        {
            return TypedResults.BadRequest(new ApiError("DuplicateSku", "Product SKU already exists."));
        }

        var product = new Product
        {
            Sku = request.Sku.Trim(),
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            UnitPrice = request.UnitPrice,
            ReorderPoint = request.ReorderPoint
        };

        product.InventoryItems.Add(new InventoryItem
        {
            Product = product,
            Warehouse = request.Warehouse.Trim(),
            QuantityOnHand = request.OpeningStock,
            QuantityReserved = 0
        });

        await db.Products.AddAsync(product, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/api/products/{product.Id}", product.ToDto());
    }

    private static async Task<Ok<IReadOnlyCollection<InventoryDto>>> ListInventoryAsync(
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var inventory = await db.InventoryItems
            .Include(item => item.Product)
            .AsNoTracking()
            .OrderBy(item => item.Product.Sku)
            .Select(item => item.ToDto())
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyCollection<InventoryDto>>(inventory);
    }

    private static async Task<Results<Ok<InventoryDto>, NotFound, BadRequest<ApiError>>> AdjustInventoryAsync(
        InventoryAdjustmentRequest request,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        if (Blank(request.Warehouse) || Blank(request.Reason) || request.Quantity == 0)
        {
            return TypedResults.BadRequest(new ApiError("InvalidAdjustment", "Warehouse, quantity and reason are required."));
        }

        var item = await db.InventoryItems
            .Include(inventory => inventory.Product)
            .SingleOrDefaultAsync(
                inventory => inventory.ProductId == request.ProductId && inventory.Warehouse == request.Warehouse,
                cancellationToken);

        if (item is null)
        {
            return TypedResults.NotFound();
        }

        if (item.QuantityOnHand + request.Quantity < item.QuantityReserved)
        {
            return TypedResults.BadRequest(new ApiError("InvalidStockBalance", "On-hand stock cannot fall below reserved quantity."));
        }

        item.QuantityOnHand += request.Quantity;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await db.StockMovements.AddAsync(new StockMovement
        {
            ProductId = request.ProductId,
            Type = StockMovementType.Adjustment,
            Quantity = request.Quantity,
            Reference = request.Reason.Trim(),
            Warehouse = request.Warehouse.Trim(),
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(item.ToDto());
    }

    private static async Task<Ok<IReadOnlyCollection<SalesOrderDto>>> ListOrdersAsync(
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var orders = await db.SalesOrders
            .Include(order => order.Customer)
            .Include(order => order.Lines)
            .ThenInclude(line => line.Product)
            .AsNoTracking()
            .OrderByDescending(order => order.OrderDate)
            .Select(order => order.ToDto())
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyCollection<SalesOrderDto>>(orders);
    }

    private static async Task<Results<Created<SalesOrderDto>, BadRequest<ApiError>, NotFound>> CreateOrderAsync(
        CreateSalesOrderRequest request,
        OrderService orders,
        CancellationToken cancellationToken)
    {
        var result = await orders.CreateAsync(request, cancellationToken);
        return result.Status switch
        {
            ServiceResultStatus.Ok => TypedResults.Created($"/api/orders/{result.Value!.Id}", result.Value),
            ServiceResultStatus.NotFound => TypedResults.NotFound(),
            _ => TypedResults.BadRequest(result.Error!)
        };
    }

    private static async Task<Ok<IReadOnlyCollection<InvoiceDto>>> ListInvoicesAsync(
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var invoices = await db.Invoices
            .Include(invoice => invoice.SalesOrder)
            .ThenInclude(order => order.Customer)
            .AsNoTracking()
            .OrderByDescending(invoice => invoice.IssuedAt)
            .Select(invoice => invoice.ToDto())
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyCollection<InvoiceDto>>(invoices);
    }

    private static async Task<Results<Ok<InvoiceDto>, NotFound, BadRequest<ApiError>>> MarkInvoicePaidAsync(
        Guid id,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .Include(item => item.SalesOrder)
            .ThenInclude(order => order.Customer)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (invoice is null)
        {
            return TypedResults.NotFound();
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return TypedResults.BadRequest(new ApiError("InvoiceAlreadyPaid", "Invoice is already paid."));
        }

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(invoice.ToDto());
    }

    private static bool Blank(string value) => string.IsNullOrWhiteSpace(value);
}
