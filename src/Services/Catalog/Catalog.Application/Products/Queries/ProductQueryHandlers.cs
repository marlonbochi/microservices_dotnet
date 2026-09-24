using Catalog.Application.Abstractions;
using Store.SharedKernel;

namespace Catalog.Application.Products.Queries;

public sealed class GetProductByIdHandler(IProductRepository products)
    : IQueryHandler<GetProductByIdQuery, ProductResponse?>
{
    public async Task<ProductResponse?> HandleAsync(GetProductByIdQuery query, CancellationToken cancellationToken = default) =>
        (await products.GetByIdAsync(query.Id, cancellationToken))?.ToResponse();
}

public sealed class ListProductsHandler(IProductRepository products)
    : IQueryHandler<ListProductsQuery, IReadOnlyList<ProductResponse>>
{
    public async Task<IReadOnlyList<ProductResponse>> HandleAsync(ListProductsQuery query, CancellationToken cancellationToken = default) =>
        [.. (await products.ListAsync(query.Search, cancellationToken)).Select(product => product.ToResponse())];
}

public sealed class GetProductsByIdsHandler(IProductRepository products)
    : IQueryHandler<GetProductsByIdsQuery, IReadOnlyList<ProductResponse>>
{
    public async Task<IReadOnlyList<ProductResponse>> HandleAsync(GetProductsByIdsQuery query, CancellationToken cancellationToken = default) =>
        [.. (await products.GetByIdsAsync(query.Ids, cancellationToken)).Select(product => product.ToResponse())];
}
