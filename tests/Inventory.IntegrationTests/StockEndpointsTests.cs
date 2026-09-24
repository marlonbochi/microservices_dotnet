using System.Net;
using System.Net.Http.Json;
using Inventory.Application.Stock;
using Store.Contracts.Catalog;
using Store.Testing;
using static Inventory.IntegrationTests.InventoryTestHelpers;

namespace Inventory.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class StockEndpointsTests(InventoryApiFactory factory) : IClassFixture<InventoryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProductCreated_CreatesStockItemWithInitialQuantity()
    {
        var productId = await SeedProductAsync(factory, _client, initialStock: 10);

        var stock = await GetStockAsync(_client, productId);

        stock.QuantityOnHand.ShouldBe(10);
        stock.Available.ShouldBe(10);
    }

    [Fact]
    public async Task ProductCreated_DeliveredTwice_CreatesSingleStockItem()
    {
        var message = new ProductCreated(Guid.NewGuid(), "DUP-1", "Mouse", 10m, 3, DateTimeOffset.UtcNow);

        await factory.Harness.Bus.Publish(message, Token);
        await factory.Harness.Bus.Publish(message, Token);
        var stock = await Eventually.Get(() => _client.GetAsync($"{Stock}/{message.ProductId}", Token), response => response.IsSuccessStatusCode);

        stock.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _client.GetFromJsonAsync<List<StockItemResponse>>(Stock, HttpExtensions.Json, Token))!
            .Count(item => item.ProductId == message.ProductId).ShouldBe(1);
    }

    [Fact]
    public async Task Restock_ExistingProduct_IncreasesQuantity()
    {
        var productId = await SeedProductAsync(factory, _client, initialStock: 10);

        var response = await _client.PostAsJsonAsync($"{Stock}/{productId}/restock", new { quantity = 5 }, Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadAs<StockItemResponse>()).Available.ShouldBe(15);
    }

    [Fact]
    public async Task Restock_UnknownProduct_Returns404()
    {
        var response = await _client.PostAsJsonAsync($"{Stock}/{Guid.NewGuid()}/restock", new { quantity = 5 }, Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Restock_ZeroQuantity_Returns400()
    {
        var response = await _client.PostAsJsonAsync($"{Stock}/{Guid.NewGuid()}/restock", new { quantity = 0 }, Token);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HealthReady_Returns200()
    {
        (await _client.GetAsync("/health/ready", Token)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
