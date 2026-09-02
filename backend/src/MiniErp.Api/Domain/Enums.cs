namespace MiniErp.Api.Domain;

public enum UserRole
{
    Administrator,
    Manager,
    Analyst
}

public enum CustomerStatus
{
    Active,
    OnHold,
    Inactive
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    Fulfilled,
    Cancelled
}

public enum InvoiceStatus
{
    Open,
    Paid,
    Overdue,
    Cancelled
}

public enum StockMovementType
{
    Receipt,
    Reservation,
    Shipment,
    Adjustment
}
