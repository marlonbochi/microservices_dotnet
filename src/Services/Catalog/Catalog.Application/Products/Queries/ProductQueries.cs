namespace Catalog.Application.Products.Queries;

public sealed record GetProductByIdQuery(Guid Id);

public sealed record ListProductsQuery(string? Search);

public sealed record GetProductsByIdsQuery(IReadOnlyCollection<Guid> Ids);
