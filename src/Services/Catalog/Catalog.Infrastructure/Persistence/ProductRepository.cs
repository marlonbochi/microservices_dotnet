using Catalog.Application.Abstractions;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

internal sealed class ProductRepository(CatalogDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Product>().FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task<bool> SkuExistsAsync(Sku sku, CancellationToken cancellationToken) =>
        dbContext.Set<Product>().AnyAsync(product => product.Sku == sku, cancellationToken);

    public async Task<IReadOnlyList<Product>> ListAsync(string? search, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Product>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var sku = Sku.Create(term);
            query = sku.IsSuccess
                ? query.Where(product => product.Name.Contains(term) || product.Sku == sku.Value)
                : query.Where(product => product.Name.Contains(term));
        }

        return await query.OrderBy(product => product.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await dbContext.Set<Product>().AsNoTracking().Where(product => ids.Contains(product.Id)).ToListAsync(cancellationToken);

    public void Add(Product product) => dbContext.Add(product);
}
