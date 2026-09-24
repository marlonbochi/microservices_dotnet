using Catalog.Application.Products;
using Catalog.Application.Products.Create;
using FluentValidation;
using Store.ServiceDefaults.Endpoints;
using Store.ServiceDefaults.Http;
using Store.SharedKernel;

namespace Catalog.Api.Endpoints.Products;

public sealed record CreateProductRequest(string Sku, string Name, string? Description, decimal Price, int InitialStock);

internal sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Sku).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9-]+$");
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Description).MaximumLength(1000);
        RuleFor(request => request.Price).GreaterThan(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(request => request.InitialStock).GreaterThanOrEqualTo(0);
    }
}

internal sealed class CreateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost(ProductRoutes.Base, HandleAsync)
            .WithValidation<CreateProductRequest>()
            .WithTags(ProductRoutes.Tag)
            .WithSummary("Registers a product and publishes ProductCreated")
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        CreateProductRequest request,
        ICommandHandler<CreateProductCommand, ProductResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(request.Sku, request.Name, request.Description, request.Price, request.InitialStock);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.Match(product => Results.CreatedAtRoute(ProductRoutes.GetByIdName, new { id = product.Id }, product));
    }
}
