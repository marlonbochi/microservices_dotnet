using Catalog.Application.Abstractions;
using Catalog.Domain.Products;
using Store.Contracts.Catalog;
using Store.SharedKernel;

namespace Catalog.Application.Products.Create;

/// <summary>
/// Registers a product and announces it with <see cref="ProductCreated"/> so Inventory can create
/// the stock record. The product row and the message are committed atomically (outbox).
/// </summary>
public sealed class CreateProductHandler(
    IProductRepository products,
    IIntegrationEventPublisher publisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<CreateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        var sku = Sku.Create(command.Sku);
        if (sku.IsFailure)
        {
            return sku.Error!;
        }

        if (await products.SkuExistsAsync(sku.Value, cancellationToken))
        {
            return ProductErrors.SkuAlreadyExists(sku.Value);
        }

        var now = timeProvider.GetUtcNow();
        var created = Product.Create(sku.Value, command.Name, command.Description, command.Price, now);
        if (created.IsFailure)
        {
            return created.Error!;
        }

        var product = created.Value;
        products.Add(product);
        await publisher.PublishAsync(
            new ProductCreated(product.Id, product.Sku.Value, product.Name, product.Price, command.InitialStock, now),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToResponse();
    }
}
