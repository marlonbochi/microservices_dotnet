using Catalog.Application.Products;
using Catalog.Application.Products.Update;
using FluentValidation;
using Store.ServiceDefaults.Endpoints;
using Store.ServiceDefaults.Http;
using Store.SharedKernel;

namespace Catalog.Api.Endpoints.Products;

public sealed record UpdateProductRequest(string Name, string? Description, decimal Price);

internal sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Description).MaximumLength(1000);
        RuleFor(request => request.Price).GreaterThan(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
    }
}

internal sealed class UpdateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut($"{ProductRoutes.Base}/{{id:guid}}", HandleAsync)
            .WithValidation<UpdateProductRequest>()
            .WithTags(ProductRoutes.Tag)
            .WithSummary("Updates product details and publishes ProductUpdated")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateProductRequest request,
        ICommandHandler<UpdateProductCommand, ProductResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateProductCommand(id, request.Name, request.Description, request.Price), cancellationToken);
        return result.Match(Results.Ok);
    }
}
