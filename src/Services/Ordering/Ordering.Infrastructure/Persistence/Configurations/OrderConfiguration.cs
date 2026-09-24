using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    private const int StatusMaxLength = 32;

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Property(order => order.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(order => order.CustomerEmail).HasMaxLength(254).IsRequired();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(StatusMaxLength);
        builder.Property(order => order.FailureReason).HasMaxLength(500);
        builder.Property(order => order.Total).HasPrecision(18, 2);
        builder.HasIndex(order => order.CreatedAt);
        builder.Ignore(order => order.IsFinal);
        builder.Ignore(order => order.DomainEvents);

        builder.OwnsMany(order => order.Items, items =>
        {
            items.ToTable("OrderItems");
            items.WithOwner().HasForeignKey("OrderId");
            items.Property<int>("Id").ValueGeneratedOnAdd();
            items.HasKey("Id");
            items.Property(item => item.ProductName).HasMaxLength(200);
            items.Property(item => item.UnitPrice).HasPrecision(18, 2);
            items.Ignore(item => item.LineTotal);
        });
        builder.Navigation(order => order.Items).HasField("_items");

        builder.OwnsMany(order => order.History, history =>
        {
            history.ToTable("OrderStatusHistory");
            history.WithOwner().HasForeignKey("OrderId");
            history.Property<int>("Id").ValueGeneratedOnAdd();
            history.HasKey("Id");
            history.Property(change => change.Status).HasConversion<string>().HasMaxLength(StatusMaxLength);
            history.Property(change => change.Note).HasMaxLength(500);
        });
        builder.Navigation(order => order.History).HasField("_history");
    }
}
