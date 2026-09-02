using MiniErp.Api.Domain;

namespace MiniErp.Api.Contracts;

public static class DtoMapper
{
    public static UserProfileDto ToProfileDto(this UserAccount user) =>
        new(user.Id, user.Name, user.Email, user.Role);

    public static CustomerDto ToDto(this Customer customer) =>
        new(
            customer.Id,
            customer.Name,
            customer.TaxId,
            customer.Segment,
            customer.Status,
            customer.CreditLimit,
            customer.Orders.Count(order => order.Status is OrderStatus.Draft or OrderStatus.Confirmed));

    public static ProductDto ToDto(this Product product)
    {
        var onHand = product.InventoryItems.Sum(item => item.QuantityOnHand);
        var reserved = product.InventoryItems.Sum(item => item.QuantityReserved);
        return new ProductDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Category,
            product.UnitPrice,
            product.ReorderPoint,
            product.IsActive,
            onHand,
            reserved,
            onHand - reserved);
    }

    public static InventoryDto ToDto(this InventoryItem item) =>
        new(
            item.ProductId,
            item.Product.Sku,
            item.Product.Name,
            item.Warehouse,
            item.QuantityOnHand,
            item.QuantityReserved,
            item.AvailableQuantity,
            item.Product.ReorderPoint,
            item.AvailableQuantity <= item.Product.ReorderPoint);

    public static SalesOrderDto ToDto(this SalesOrder order) =>
        new(
            order.Id,
            order.Number,
            order.CustomerId,
            order.Customer.Name,
            order.Status,
            order.OrderDate,
            order.RequiredDate,
            order.Subtotal,
            order.Tax,
            order.Total,
            order.Lines.Select(line => line.ToDto()).ToArray());

    public static SalesOrderLineDto ToDto(this SalesOrderLine line) =>
        new(
            line.ProductId,
            line.Product.Sku,
            line.Product.Name,
            line.Quantity,
            line.UnitPrice,
            line.LineTotal);

    public static InvoiceDto ToDto(this Invoice invoice) =>
        new(
            invoice.Id,
            invoice.Number,
            invoice.SalesOrder.Number,
            invoice.SalesOrder.Customer.Name,
            invoice.Status,
            invoice.IssuedAt,
            invoice.DueAt,
            invoice.PaidAt,
            invoice.Amount);
}
