using System.Net;
using System.Net.Http.Json;
using MassTransit.Testing;
using Ordering.Application.Orders;
using Ordering.Domain.Orders;
using Store.Contracts.Inventory;
using Store.Contracts.Ordering;
using Store.Contracts.Payment;
using Store.Testing;

namespace Ordering.IntegrationTests;

/// <summary>
/// Places orders through the HTTP API and plays the role of Inventory and Payment by publishing their
/// replies on the test bus, so the real saga (EF repository, outbox, SQL Server) runs end to end.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OrderEndpointsTests(OrderingApiFactory factory) : IClassFixture<OrderingApiFactory>
{
    private const string Orders = "/api/ordering/orders";
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<OrderResponse> PlaceAsync(Guid productId, int quantity = 2, bool simulatePaymentFailure = false)
    {
        var response = await _client.PostAsJsonAsync(Orders, new
        {
            customerName = "Ana",
            customerEmail = "ana@example.com",
            items = new[] { new { productId, quantity } },
            simulatePaymentFailure,
        }, Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        return await response.ReadAs<OrderResponse>();
    }

    private Task<OrderResponse> WaitForStatusAsync(Guid orderId, OrderStatus status) =>
        Eventually.Get(
            async () => (await _client.GetFromJsonAsync<OrderResponse>($"{Orders}/{orderId}", HttpExtensions.Json, Token))!,
            order => order.Status == status);

    private async Task WaitForPublishedAsync<T>(Guid orderId, Func<T, Guid> orderIdOf)
        where T : class =>
        (await factory.Harness.Published.Any<T>(message => orderIdOf(message.Context.Message) == orderId, Token)).ShouldBeTrue();

    [Fact]
    public async Task Post_ValidOrder_Returns202WithCatalogPricesAndStartsSaga()
    {
        var keyboard = factory.Catalog.Add("Teclado", 350m);

        var order = await PlaceAsync(keyboard.Id, quantity: 2);

        order.Status.ShouldBe(OrderStatus.Submitted);
        order.Total.ShouldBe(700m);
        await WaitForPublishedAsync<OrderSubmitted>(order.Id, message => message.OrderId);
        await WaitForPublishedAsync<ReserveStock>(order.Id, message => message.OrderId);
    }

    [Fact]
    public async Task HappyPath_ReachesConfirmedWithFullTimeline()
    {
        var product = factory.Catalog.Add("Mouse", 100m);
        var order = await PlaceAsync(product.Id);
        await WaitForPublishedAsync<ReserveStock>(order.Id, message => message.OrderId);

        await factory.Harness.Bus.Publish(new StockReserved(order.Id), Token);
        await WaitForPublishedAsync<ProcessPayment>(order.Id, message => message.OrderId);
        await factory.Harness.Bus.Publish(new PaymentApproved(order.Id, "TX-1"), Token);
        await WaitForPublishedAsync<CommitStock>(order.Id, message => message.OrderId);
        await factory.Harness.Bus.Publish(new StockCommitted(order.Id), Token);

        var confirmed = await WaitForStatusAsync(order.Id, OrderStatus.Confirmed);
        confirmed.History.Select(change => change.Status).ShouldBe(
            [OrderStatus.Submitted, OrderStatus.StockReserved, OrderStatus.PaymentApproved, OrderStatus.Confirmed]);
    }

    [Fact]
    public async Task PaymentDeclined_ReleasesStockAndCancels()
    {
        var product = factory.Catalog.Add("Monitor", 1200m);
        var order = await PlaceAsync(product.Id, simulatePaymentFailure: true);
        await WaitForPublishedAsync<ReserveStock>(order.Id, message => message.OrderId);

        await factory.Harness.Bus.Publish(new StockReserved(order.Id), Token);
        await WaitForPublishedAsync<ProcessPayment>(order.Id, message => message.OrderId);
        await factory.Harness.Bus.Publish(new PaymentDeclined(order.Id, "Card declined"), Token);
        await WaitForPublishedAsync<ReleaseStock>(order.Id, message => message.OrderId);
        await factory.Harness.Bus.Publish(new StockReleased(order.Id), Token);

        var cancelled = await WaitForStatusAsync(order.Id, OrderStatus.Cancelled);
        cancelled.FailureReason.ShouldBe("Card declined");
    }

    [Fact]
    public async Task InsufficientStock_RejectsOrder()
    {
        var product = factory.Catalog.Add("Headset", 250m);
        var order = await PlaceAsync(product.Id, quantity: 50);
        await WaitForPublishedAsync<ReserveStock>(order.Id, message => message.OrderId);

        await factory.Harness.Bus.Publish(new StockReservationFailed(order.Id, "Insufficient stock"), Token);

        var rejected = await WaitForStatusAsync(order.Id, OrderStatus.Rejected);
        rejected.FailureReason.ShouldBe("Insufficient stock");
    }

    [Fact]
    public async Task Post_UnknownProduct_Returns400()
    {
        var response = await _client.PostAsJsonAsync(Orders, new
        {
            customerName = "Ana",
            customerEmail = "ana@example.com",
            items = new[] { new { productId = Guid.NewGuid(), quantity = 1 } },
        }, Token);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_InvalidPayload_Returns400()
    {
        var response = await _client.PostAsJsonAsync(Orders, new
        {
            customerName = "",
            customerEmail = "not-an-email",
            items = Array.Empty<object>(),
        }, Token);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_ReturnsNewestFirst()
    {
        var product = factory.Catalog.Add("Cabo", 20m);
        var first = await PlaceAsync(product.Id);
        var second = await PlaceAsync(product.Id);

        var orders = (await _client.GetFromJsonAsync<List<OrderResponse>>(Orders, HttpExtensions.Json, Token))!;

        orders.FindIndex(order => order.Id == second.Id).ShouldBeLessThan(orders.FindIndex(order => order.Id == first.Id));
    }

    [Fact]
    public async Task HealthReady_Returns200()
    {
        (await _client.GetAsync("/health/ready", Token)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

/// <summary>Separate fixture instance: toggling the fake must not affect other test classes.</summary>
[Trait("Category", "Integration")]
public sealed class CatalogOutageTests(OrderingApiFactory factory) : IClassFixture<OrderingApiFactory>
{
    [Fact]
    public async Task Post_WhenCatalogUnavailable_Returns503()
    {
        factory.Catalog.Unavailable = true;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ordering/orders", new
        {
            customerName = "Ana",
            customerEmail = "ana@example.com",
            items = new[] { new { productId = Guid.NewGuid(), quantity = 1 } },
        }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}
