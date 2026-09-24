using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();

        builder.Property(product => product.Sku)
            .HasConversion(sku => sku.Value, value => Sku.Create(value).Value)
            .HasMaxLength(Sku.MaxLength)
            .IsRequired();
        builder.HasIndex(product => product.Sku).IsUnique();

        builder.Property(product => product.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(Product.DescriptionMaxLength);
        builder.Property(product => product.Price).HasPrecision(18, 2);

        builder.Ignore(product => product.DomainEvents);
    }
}
