using Store.SharedKernel;

namespace Catalog.Domain.Products;

/// <summary>A sellable item of the catalog. Stock is NOT modelled here: it belongs to Inventory.</summary>
public sealed class Product : AggregateRoot<Guid>
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;
    private const int PriceDecimals = 2;

    private Product()
    {
    }

    public Sku Sku { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<Product> Create(Sku sku, string name, string? description, decimal price, DateTimeOffset now)
    {
        var product = new Product { Id = Guid.CreateVersion7(), Sku = sku, CreatedAt = now };
        var result = product.Update(name, description, price, now);
        return result.IsSuccess ? product : result.Error!;
    }

    public Result Update(string name, string? description, decimal price, DateTimeOffset now)
    {
        var error = Validate(name, description, price);
        if (error is not null)
        {
            return error;
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Price = decimal.Round(price, PriceDecimals);
        UpdatedAt = now;
        return Result.Success();
    }

    private static Error? Validate(string name, string? description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            return ProductErrors.InvalidName;
        }

        if (description?.Length > DescriptionMaxLength)
        {
            return ProductErrors.InvalidDescription;
        }

        return price <= 0 ? ProductErrors.InvalidPrice : null;
    }
}
