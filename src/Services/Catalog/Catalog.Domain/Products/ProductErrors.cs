using Store.SharedKernel;

namespace Catalog.Domain.Products;

public static class ProductErrors
{
    public static readonly Error InvalidSku = Error.Validation(
        "Product.InvalidSku", $"SKU must have 1-{Sku.MaxLength} characters: letters, digits or dashes.");

    public static readonly Error InvalidName = Error.Validation(
        "Product.InvalidName", $"Name is required and must have at most {Product.NameMaxLength} characters.");

    public static readonly Error InvalidDescription = Error.Validation(
        "Product.InvalidDescription", $"Description must have at most {Product.DescriptionMaxLength} characters.");

    public static readonly Error InvalidPrice = Error.Validation(
        "Product.InvalidPrice", "Price must be greater than zero.");

    public static Error SkuAlreadyExists(Sku sku) => Error.Conflict(
        "Product.SkuAlreadyExists", $"A product with SKU '{sku}' already exists.");

    public static Error NotFound(Guid id) => Error.NotFound(
        "Product.NotFound", $"Product '{id}' was not found.");
}
