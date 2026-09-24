using Catalog.Application.Abstractions;
using Catalog.Application.Products.Update;
using Catalog.Domain.Products;
using Microsoft.Extensions.Time.Testing;
using Store.Contracts.Catalog;
using Store.SharedKernel;

namespace Catalog.UnitTests.Application;

public sealed class UpdateProductHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IIntegrationEventPublisher _publisher = Substitute.For<IIntegrationEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero));
    private readonly UpdateProductHandler _handler;

    public UpdateProductHandlerTests() =>
        _handler = new UpdateProductHandler(_products, _publisher, _unitOfWork, _time);

    [Fact]
    public async Task HandleAsync_WithUnknownProduct_ReturnsNotFound()
    {
        var result = await _handler.HandleAsync(
            new UpdateProductCommand(Guid.NewGuid(), "Novo", null, 10m), TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WithValidChange_UpdatesPublishesAndCommits()
    {
        var product = Product.Create(Sku.Create("KB-001").Value, "Teclado", null, 350m, _time.GetUtcNow()).Value;
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.HandleAsync(
            new UpdateProductCommand(product.Id, "Teclado RGB", "Novo", 399.9m), TestContext.Current.CancellationToken);

        result.Value.Price.ShouldBe(399.9m);
        await _publisher.Received(1).PublishAsync(
            Arg.Is<ProductUpdated>(message => message.ProductId == product.Id && message.Name == "Teclado RGB"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
