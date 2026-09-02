namespace MiniErp.Api.Domain;

public sealed class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Email { get; set; }
    public UserRole Role { get; set; }
    public required string PasswordSalt { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
}

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string TaxId { get; set; }
    public required string Segment { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public decimal CreditLimit { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<SalesOrder> Orders { get; set; } = [];
}

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public decimal UnitPrice { get; set; }
    public int ReorderPoint { get; set; }
    public bool IsActive { get; set; } = true;
    public List<InventoryItem> InventoryItems { get; set; } = [];
    public List<SalesOrderLine> SalesOrderLines { get; set; } = [];
}

public sealed class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public required string Warehouse { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int AvailableQuantity => QuantityOnHand - QuantityReserved;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SalesOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Number { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public OrderStatus Status { get; set; } = OrderStatus.Confirmed;
    public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset RequiredDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public List<SalesOrderLine> Lines { get; set; } = [];
    public Invoice? Invoice { get; set; }
}

public sealed class SalesOrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Number { get; set; }
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DueAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public decimal Amount { get; set; }
}

public sealed class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public required string Reference { get; set; }
    public required string Warehouse { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
