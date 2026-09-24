using Ordering.Application.Abstractions;
using Ordering.Application.Orders.Place;
using Ordering.UnitTests.Fakes;
using Store.Contracts.Ordering;
using Store.SharedKernel;

namespace Ordering.UnitTests.Application;

public sealed class PlaceOrderHandlerTests
{
    private readonly ICatalogClient _catalog = Substitute.For<ICatalogClient>();
    private readonly InMemoryOrders _orders = new();
    private readonly PlaceOrderHandler _handler;
    private readonly CatalogProduct _keyboard = new(Guid.NewGuid(), "Teclado", 350m);

    public PlaceOrderHandlerTests()
    {
        _handler = new PlaceOrderHandler(_catalog, _orders, _orders, _orders, TimeProvider.System);
        _catalog.GetProductsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<CatalogProduct>>([_keyboard]));
    }

    private Task<Result<Ordering.Application.Orders.OrderResponse>> Place(params PlaceOrderLine[] lines) =>
        _handler.HandleAsync(new PlaceOrderCommand("Ana", "ana@example.com", lines, SimulatePaymentFailure: true),
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task HandleAsync_WithKnownProducts_UsesCatalogPriceAndPublishesOrderSubmitted()
    {
        var result = await Place(new PlaceOrderLine(_keyboard.Id, 2));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Total.ShouldBe(700m);
        result.Value.Items.ShouldHaveSingleItem().ProductName.ShouldBe("Teclado");
        var submitted = _orders.Published.ShouldHaveSingleItem().ShouldBeOfType<OrderSubmitted>();
        submitted.OrderId.ShouldBe(result.Value.Id);
        submitted.SimulatePaymentFailure.ShouldBeTrue();
        submitted.Items.ShouldHaveSingleItem().ShouldBe(new OrderLine(_keyboard.Id, 2));
        _orders.Commits.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownProduct_ReturnsValidationError()
    {
        var result = await Place(new PlaceOrderLine(Guid.NewGuid(), 1));

        result.Error!.Code.ShouldBe("Order.UnknownProducts");
        _orders.Published.ShouldBeEmpty();
        _orders.Commits.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_WhenCatalogUnavailable_FailsFastWithUnavailable()
    {
        _catalog.GetProductsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CatalogProduct>>.Failure(Error.Unavailable("Catalog.Unavailable", "down")));

        var result = await Place(new PlaceOrderLine(_keyboard.Id, 1));

        result.Error!.Type.ShouldBe(ErrorType.Unavailable);
        _orders.Published.ShouldBeEmpty();
    }
}
