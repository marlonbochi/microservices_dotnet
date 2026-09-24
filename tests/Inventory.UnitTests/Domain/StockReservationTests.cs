using Inventory.Domain.Reservations;
using Store.SharedKernel;

namespace Inventory.UnitTests.Domain;

public sealed class StockReservationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch;

    private static StockReservation NewReservation() =>
        StockReservation.Create(Guid.NewGuid(), [new ReservationLine(Guid.NewGuid(), 2)], Now);

    [Fact]
    public void Create_WithoutLines_Throws()
    {
        Should.Throw<DomainException>(() => StockReservation.Create(Guid.NewGuid(), [], Now));
    }

    [Fact]
    public void Commit_FromReserved_TransitionsOnce()
    {
        var reservation = NewReservation();

        reservation.Commit(Now).ShouldBeTrue();
        reservation.Commit(Now).ShouldBeFalse();
        reservation.Status.ShouldBe(ReservationStatus.Committed);
    }

    [Fact]
    public void Release_FromReserved_TransitionsOnce()
    {
        var reservation = NewReservation();

        reservation.Release(Now).ShouldBeTrue();
        reservation.Release(Now).ShouldBeFalse();
        reservation.Status.ShouldBe(ReservationStatus.Released);
    }

    [Fact]
    public void Release_AfterCommit_Throws()
    {
        var reservation = NewReservation();
        reservation.Commit(Now);

        Should.Throw<DomainException>(() => reservation.Release(Now));
    }
}
