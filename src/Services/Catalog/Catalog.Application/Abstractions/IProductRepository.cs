using Catalog.Domain.Products;

namespace Catalog.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> SkuExistsAsync(Sku sku, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> ListAsync(string? search, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    void Add(Product product);
}
