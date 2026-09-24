using Catalog.Application.Abstractions;
using Catalog.Application.Products.Create;
using Catalog.Domain.Products;
using Microsoft.Extensions.Time.Testing;
using Store.Contracts.Catalog;
using Store.SharedKernel;

namespace Catalog.UnitTests.Application;

public sealed class CreateProductHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IIntegrationEventPublisher _publisher = Substitute.For<IIntegrationEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero));
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerTests() =>
        _handler = new CreateProductHandler(_products, _publisher, _unitOfWork, _time);

    [Fact]
    public async Task HandleAsync_WithNewSku_AddsProductPublishesEventAndCommits()
    {
        var command = new CreateProductCommand("kb-001", "Teclado", null, 350m, 10);

        var result = await _handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Sku.ShouldBe("KB-001");
        _products.Received(1).Add(Arg.Is<Product>(product => product.Name == "Teclado"));
        await _publisher.Received(1).PublishAsync(
            Arg.Is<ProductCreated>(message => message.ProductId == result.Value.Id && message.InitialStock == 10),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithExistingSku_ReturnsConflictAndDoesNothing()
    {
        _products.SkuExistsAsync(Arg.Any<Sku>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.HandleAsync(
            new CreateProductCommand("KB-001", "Teclado", null, 350m, 10), TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.Conflict);
        _products.DidNotReceive().Add(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPrice_ReturnsValidationErrorAndPublishesNothing()
    {
        var result = await _handler.HandleAsync(
            new CreateProductCommand("KB-001", "Teclado", null, 0m, 10), TestContext.Current.CancellationToken);

        result.Error.ShouldBe(ProductErrors.InvalidPrice);
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<ProductCreated>(), Arg.Any<CancellationToken>());
    }
}
