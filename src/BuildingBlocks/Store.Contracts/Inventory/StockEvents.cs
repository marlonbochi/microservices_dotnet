namespace Store.Contracts.Inventory;

public sealed record StockReserved(Guid OrderId);

public sealed record StockReservationFailed(Guid OrderId, string Reason);

public sealed record StockCommitted(Guid OrderId);

public sealed record StockReleased(Guid OrderId);
