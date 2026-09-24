namespace Ordering.Domain.Orders;

public enum OrderStatus
{
    Submitted,
    StockReserved,
    PaymentApproved,
    PaymentDeclined,
    Confirmed,
    Rejected,
    Cancelled,
}
