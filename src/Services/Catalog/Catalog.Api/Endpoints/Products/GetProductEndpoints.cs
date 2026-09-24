using Catalog.Application.Products;
using Catalog.Application.Products.Queries;
using Microsoft.AspNetCore.Mvc;
using Store.ServiceDefaults.Endpoints;
using Store.SharedKernel;

namespace Catalog.Api.Endpoints.Products;

internal sealed class ListProductsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet(ProductRoutes.Base, async (
                string? search,
                IQueryHandler<ListProductsQuery, IReadOnlyList<ProductResponse>> handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(new ListProductsQuery(search), cancellationToken)))
            .WithTags(ProductRoutes.Tag)
            .WithSummary("Lists products, optionally filtered by name or exact SKU")
            .Produces<IReadOnlyList<ProductResponse>>();
}

internal sealed class GetProductByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet($"{ProductRoutes.Base}/{{id:guid}}", async (
                Guid id,
                IQueryHandler<GetProductByIdQuery, ProductResponse?> handler,
                CancellationToken cancellationToken) =>
                await handler.HandleAsync(new GetProductByIdQuery(id), cancellationToken) is { } product
                    ? Results.Ok(product)
                    : Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Product.NotFound"))
            .WithName(ProductRoutes.GetByIdName)
            .WithTags(ProductRoutes.Tag)
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
}

/// <summary>Used by the Ordering service (synchronous query) to read authoritative prices.</summary>
internal sealed class GetProductsBatchEndpoint : IEndpoint
{
    private const int MaxIds = 100;

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet($"{ProductRoutes.Base}/batch", async (
                [FromQuery] Guid[] ids,
                IQueryHandler<GetProductsByIdsQuery, IReadOnlyList<ProductResponse>> handler,
                CancellationToken cancellationToken) =>
                ids.Length is 0 or > MaxIds
                    ? Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Product.InvalidBatch",
                        detail: $"Provide between 1 and {MaxIds} ids.")
                    : Results.Ok(await handler.HandleAsync(new GetProductsByIdsQuery(ids.Distinct().ToArray()), cancellationToken)))
            .WithTags(ProductRoutes.Tag)
            .WithSummary("Gets several products at once (used by Ordering)")
            .Produces<IReadOnlyList<ProductResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
}
