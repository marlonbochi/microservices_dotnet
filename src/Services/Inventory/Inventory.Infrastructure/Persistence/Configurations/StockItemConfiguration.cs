using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public const string RowVersion = nameof(RowVersion);

    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems", table => table.HasCheckConstraint(
            "CK_StockItems_Quantities",
            "[QuantityOnHand] >= 0 AND [QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantityOnHand]"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("ProductId").ValueGeneratedNever();
        builder.Property(item => item.Sku).HasMaxLength(50).IsRequired();
        builder.Property(item => item.ProductName).HasMaxLength(200).IsRequired();
        builder.Ignore(item => item.Available);
        builder.Ignore(item => item.DomainEvents);

        // Optimistic concurrency: SQL Server bumps this on every UPDATE; EF adds it to the WHERE clause,
        // so a concurrent writer affects 0 rows and gets DbUpdateConcurrencyException.
        builder.Property<byte[]>(RowVersion).IsRowVersion();
    }
}
