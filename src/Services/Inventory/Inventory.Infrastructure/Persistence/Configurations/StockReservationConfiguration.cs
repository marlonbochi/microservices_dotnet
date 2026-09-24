using Inventory.Domain.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

internal sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("StockReservations");
        builder.HasKey(reservation => reservation.Id);
        builder.Property(reservation => reservation.Id).HasColumnName("OrderId").ValueGeneratedNever();
        builder.Property(reservation => reservation.Status).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(reservation => reservation.DomainEvents);

        builder.OwnsMany(reservation => reservation.Lines, lines =>
        {
            lines.ToTable("StockReservationLines");
            lines.WithOwner().HasForeignKey("OrderId");
            lines.Property<int>("Id").ValueGeneratedOnAdd();
            lines.HasKey("Id");
        });
        builder.Navigation(reservation => reservation.Lines).HasField("_lines");
    }
}
