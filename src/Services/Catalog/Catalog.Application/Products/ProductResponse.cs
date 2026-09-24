using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

internal static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product) => new(
        product.Id,
        product.Sku.Value,
        product.Name,
        product.Description,
        product.Price,
        product.CreatedAt,
        product.UpdatedAt);
}
