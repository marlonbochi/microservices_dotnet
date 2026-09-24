namespace Inventory.Domain.Reservations;

public sealed record ReservationLine(Guid ProductId, int Quantity);
