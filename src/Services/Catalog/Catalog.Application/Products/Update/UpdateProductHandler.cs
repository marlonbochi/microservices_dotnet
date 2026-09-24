using Catalog.Application.Abstractions;
using Catalog.Domain.Products;
using Store.Contracts.Catalog;
using Store.SharedKernel;

namespace Catalog.Application.Products.Update;

public sealed class UpdateProductHandler(
    IProductRepository products,
    IIntegrationEventPublisher publisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<UpdateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(command.Id);
        }

        var now = timeProvider.GetUtcNow();
        var updated = product.Update(command.Name, command.Description, command.Price, now);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await publisher.PublishAsync(new ProductUpdated(product.Id, product.Name, product.Price, now), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToResponse();
    }
}
