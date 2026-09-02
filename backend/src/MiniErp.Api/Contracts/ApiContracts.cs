using MiniErp.Api.Domain;

namespace MiniErp.Api.Contracts;

public sealed record ApiError(string Code, string Message);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserProfileDto User);

public sealed record UserProfileDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role);

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string TaxId,
    string Segment,
    CustomerStatus Status,
    decimal CreditLimit,
    int OpenOrders);

public sealed record CreateCustomerRequest(
    string Name,
    string TaxId,
    string Segment,
    decimal CreditLimit);

public sealed record UpdateCustomerStatusRequest(CustomerStatus Status);

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Category,
    decimal UnitPrice,
    int ReorderPoint,
    bool IsActive,
    int QuantityOnHand,
    int QuantityReserved,
    int AvailableQuantity);

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string Category,
    decimal UnitPrice,
    int ReorderPoint,
    int OpeningStock,
    string Warehouse);

public sealed record InventoryDto(
    Guid ProductId,
    string Sku,
    string ProductName,
    string Warehouse,
    int QuantityOnHand,
    int QuantityReserved,
    int AvailableQuantity,
    int ReorderPoint,
    bool IsLowStock);

public sealed record InventoryAdjustmentRequest(
    Guid ProductId,
    string Warehouse,
    int Quantity,
    string Reason);

public sealed record SalesOrderDto(
    Guid Id,
    string Number,
    Guid CustomerId,
    string CustomerName,
    OrderStatus Status,
    DateTimeOffset OrderDate,
    DateTimeOffset RequiredDate,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    IReadOnlyCollection<SalesOrderLineDto> Lines);

public sealed record SalesOrderLineDto(
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record CreateSalesOrderRequest(
    Guid CustomerId,
    DateTimeOffset RequiredDate,
    IReadOnlyCollection<CreateSalesOrderLineRequest> Lines);

public sealed record CreateSalesOrderLineRequest(
    Guid ProductId,
    int Quantity);

public sealed record InvoiceDto(
    Guid Id,
    string Number,
    string SalesOrderNumber,
    string CustomerName,
    InvoiceStatus Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset DueAt,
    DateTimeOffset? PaidAt,
    decimal Amount);

public sealed record DashboardSummaryDto(
    IReadOnlyCollection<KpiDto> Kpis,
    IReadOnlyCollection<RevenuePointDto> Revenue,
    IReadOnlyCollection<CategoryRevenueDto> CategoryRevenue,
    IReadOnlyCollection<RiskItemDto> Risks,
    IReadOnlyCollection<RecentOrderDto> RecentOrders);

public sealed record KpiDto(string Label, string Value, string Delta, string Tone);

public sealed record RevenuePointDto(string Month, decimal Revenue, decimal Receivables);

public sealed record CategoryRevenueDto(string Category, decimal Revenue);

public sealed record RiskItemDto(string Label, int Value, string Tone);

public sealed record RecentOrderDto(
    string Number,
    string CustomerName,
    OrderStatus Status,
    decimal Total,
    DateTimeOffset RequiredDate);
