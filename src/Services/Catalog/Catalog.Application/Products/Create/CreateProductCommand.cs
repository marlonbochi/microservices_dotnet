namespace Catalog.Application.Products.Create;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    int InitialStock);
