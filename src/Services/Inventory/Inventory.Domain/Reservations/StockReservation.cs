using Store.SharedKernel;

namespace Inventory.Domain.Reservations;

/// <summary>
/// Units held for one order until payment is decided. Identity = OrderId, which makes
/// processing the same ReserveStock command twice harmless (idempotency).
/// </summary>
public sealed class StockReservation : AggregateRoot<Guid>
{
    private readonly List<ReservationLine> _lines = [];

    private StockReservation()
    {
    }

    public ReservationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? SettledAt { get; private set; }

    public IReadOnlyList<ReservationLine> Lines => _lines.AsReadOnly();

    public static StockReservation Create(Guid orderId, IEnumerable<ReservationLine> lines, DateTimeOffset now)
    {
        var reservation = new StockReservation { Id = orderId, Status = ReservationStatus.Reserved, CreatedAt = now };
        reservation._lines.AddRange(lines);
        if (reservation._lines.Count == 0)
        {
            throw new DomainException("A reservation needs at least one line.");
        }

        return reservation;
    }

    /// <returns><c>true</c> when the transition happened; <c>false</c> when it was already committed.</returns>
    public bool Commit(DateTimeOffset now) => SettleAs(ReservationStatus.Committed, now);

    /// <returns><c>true</c> when the transition happened; <c>false</c> when it was already released.</returns>
    public bool Release(DateTimeOffset now) => SettleAs(ReservationStatus.Released, now);

    private bool SettleAs(ReservationStatus target, DateTimeOffset now)
    {
        if (Status == target)
        {
            return false;
        }

        if (Status != ReservationStatus.Reserved)
        {
            throw new DomainException($"Reservation {Id} is {Status} and cannot become {target}.");
        }

        Status = target;
        SettledAt = now;
        return true;
    }
}
