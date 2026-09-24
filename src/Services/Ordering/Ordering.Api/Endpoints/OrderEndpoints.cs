using FluentValidation;
using Ordering.Application.Orders;
using Ordering.Application.Orders.Place;
using Ordering.Application.Orders.Queries;
using Store.ServiceDefaults.Endpoints;
using Store.ServiceDefaults.Http;
using Store.SharedKernel;

namespace Ordering.Api.Endpoints;

internal static class OrderRoutes
{
    public const string Base = "/api/ordering/orders";
    public const string Tag = "Orders";
    public const string GetByIdName = "GetOrderById";
}

public sealed record PlaceOrderItemRequest(Guid ProductId, int Quantity);

public sealed record PlaceOrderRequest(
    string CustomerName,
    string CustomerEmail,
    IReadOnlyList<PlaceOrderItemRequest> Items,
    bool SimulatePaymentFailure);

internal sealed class PlaceOrderRequestValidator : AbstractValidator<PlaceOrderRequest>
{
    private const int MaxItems = 20;
    private const int MaxQuantity = 1_000;

    public PlaceOrderRequestValidator()
    {
        RuleFor(request => request.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(request => request.Items).NotEmpty().Must(items => items.Count <= MaxItems)
            .WithMessage($"An order can have at most {MaxItems} items.");
        RuleForEach(request => request.Items).ChildRules(item =>
        {
            item.RuleFor(line => line.ProductId).NotEmpty();
            item.RuleFor(line => line.Quantity).InclusiveBetween(1, MaxQuantity);
        });
    }
}

internal sealed class PlaceOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost(OrderRoutes.Base, HandleAsync)
            .WithValidation<PlaceOrderRequest>()
            .WithTags(OrderRoutes.Tag)
            .WithSummary("Places an order; processing continues asynchronously (202 Accepted)")
            .Produces<OrderResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

    private static async Task<IResult> HandleAsync(
        PlaceOrderRequest request,
        ICommandHandler<PlaceOrderCommand, OrderResponse> handler,
        LinkGenerator links,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new PlaceOrderCommand(
            request.CustomerName,
            request.CustomerEmail,
            [.. request.Items.Select(item => new PlaceOrderLine(item.ProductId, item.Quantity))],
            request.SimulatePaymentFailure);

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.Match(order =>
            Results.Accepted(links.GetPathByName(httpContext, OrderRoutes.GetByIdName, new { id = order.Id }), order));
    }
}

internal sealed class GetOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet($"{OrderRoutes.Base}/{{id:guid}}", async (
                Guid id,
                IQueryHandler<GetOrderQuery, OrderResponse?> handler,
                CancellationToken cancellationToken) =>
                await handler.HandleAsync(new GetOrderQuery(id), cancellationToken) is { } order
                    ? Results.Ok(order)
                    : Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Order.NotFound"))
            .WithName(OrderRoutes.GetByIdName)
            .WithTags(OrderRoutes.Tag)
            .Produces<OrderResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
}

internal sealed class ListOrdersEndpoint : IEndpoint
{
    private const int DefaultTake = 50;
    private const int MaxTake = 200;

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet(OrderRoutes.Base, async (
                int? take,
                IQueryHandler<ListOrdersQuery, IReadOnlyList<OrderResponse>> handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(
                    new ListOrdersQuery(Math.Clamp(take ?? DefaultTake, 1, MaxTake)), cancellationToken)))
            .WithTags(OrderRoutes.Tag)
            .WithSummary("Lists the most recent orders")
            .Produces<IReadOnlyList<OrderResponse>>();
}
