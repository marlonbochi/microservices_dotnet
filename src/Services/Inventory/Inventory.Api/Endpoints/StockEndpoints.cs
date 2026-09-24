using FluentValidation;
using Inventory.Application.Stock;
using Store.ServiceDefaults.Endpoints;
using Store.ServiceDefaults.Http;
using Store.SharedKernel;

namespace Inventory.Api.Endpoints;

internal static class StockRoutes
{
    public const string Base = "/api/inventory/stock";
    public const string Tag = "Stock";
}

internal sealed class ListStockEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet(StockRoutes.Base, async (
                IQueryHandler<ListStockQuery, IReadOnlyList<StockItemResponse>> handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(new ListStockQuery(), cancellationToken)))
            .WithTags(StockRoutes.Tag)
            .WithSummary("Lists stock levels of all products")
            .Produces<IReadOnlyList<StockItemResponse>>();
}

internal sealed class GetStockEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet($"{StockRoutes.Base}/{{productId:guid}}", async (
                Guid productId,
                IQueryHandler<GetStockQuery, StockItemResponse?> handler,
                CancellationToken cancellationToken) =>
                await handler.HandleAsync(new GetStockQuery(productId), cancellationToken) is { } stock
                    ? Results.Ok(stock)
                    : Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Stock.NotFound"))
            .WithTags(StockRoutes.Tag)
            .Produces<StockItemResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
}

public sealed record RestockRequest(int Quantity);

internal sealed class RestockRequestValidator : AbstractValidator<RestockRequest>
{
    private const int MaxRestock = 100_000;

    public RestockRequestValidator() => RuleFor(request => request.Quantity).InclusiveBetween(1, MaxRestock);
}

internal sealed class RestockEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost($"{StockRoutes.Base}/{{productId:guid}}/restock", async (
                Guid productId,
                RestockRequest request,
                ICommandHandler<RestockCommand, StockItemResponse> handler,
                CancellationToken cancellationToken) =>
                (await handler.HandleAsync(new RestockCommand(productId, request.Quantity), cancellationToken)).Match(Results.Ok))
            .WithValidation<RestockRequest>()
            .WithTags(StockRoutes.Tag)
            .WithSummary("Adds units to a product's stock")
            .Produces<StockItemResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
}
