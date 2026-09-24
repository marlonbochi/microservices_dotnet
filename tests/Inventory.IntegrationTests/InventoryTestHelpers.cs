using System.Net;
using System.Net.Http.Json;
using Inventory.Application.Stock;
using Store.Contracts.Catalog;
using Store.Testing;

namespace Inventory.IntegrationTests;

internal static class InventoryTestHelpers
{
    public const string Stock = "/api/inventory/stock";

    /// <summary>Simulates Catalog publishing ProductCreated and waits for Inventory to consume it.</summary>
    public static async Task<Guid> SeedProductAsync(InventoryApiFactory factory, HttpClient client, int initialStock)
    {
        var productId = Guid.NewGuid();
        await factory.Harness.Bus.Publish(
            new ProductCreated(productId, $"SKU-{productId:N}"[..12], "Teclado", 100m, initialStock, DateTimeOffset.UtcNow));

        var stock = await Eventually.Get(
            () => client.GetAsync($"{Stock}/{productId}"),
            response => response.StatusCode == HttpStatusCode.OK);
        stock.StatusCode.ShouldBe(HttpStatusCode.OK);
        return productId;
    }

    public static async Task<StockItemResponse> GetStockAsync(HttpClient client, Guid productId) =>
        (await client.GetFromJsonAsync<StockItemResponse>($"{Stock}/{productId}", HttpExtensions.Json))!;
}
