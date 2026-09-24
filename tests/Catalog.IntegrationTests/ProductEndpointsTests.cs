using System.Net;
using System.Net.Http.Json;
using Catalog.Application.Products;
using Microsoft.AspNetCore.Mvc;
using Store.Contracts.Catalog;
using Store.Testing;

namespace Catalog.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class ProductEndpointsTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private const string Products = "/api/catalog/products";
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static string NewSku() => $"T-{Guid.NewGuid():N}"[..12];

    private async Task<ProductResponse> CreateAsync(string sku, decimal price = 350m)
    {
        var response = await _client.PostAsJsonAsync(Products, new { sku, name = "Teclado", description = "ABNT2", price, initialStock = 10 }, Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await response.ReadAs<ProductResponse>();
    }

    [Fact]
    public async Task Post_ValidProduct_Returns201WithLocationAndPublishesProductCreated()
    {
        var sku = NewSku();

        var response = await _client.PostAsJsonAsync(Products, new { sku, name = "Teclado", price = 350m, initialStock = 10 }, Token);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location!.ToString().ShouldContain(Products);
        var product = await response.ReadAs<ProductResponse>();
        product.Sku.ShouldBe(sku.ToUpperInvariant());
        (await factory.Harness.WaitForPublishedAsync<ProductCreated>(message => message.ProductId == product.Id && message.InitialStock == 10)).ShouldBeTrue();
    }

    [Fact]
    public async Task Post_DuplicateSku_Returns409ProblemDetails()
    {
        var sku = NewSku();
        await CreateAsync(sku);

        var response = await _client.PostAsJsonAsync(Products, new { sku, name = "Outro", price = 10m, initialStock = 0 }, Token);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ReadAs<ProblemDetails>()).Title.ShouldBe("Product.SkuAlreadyExists");
    }

    [Fact]
    public async Task Post_InvalidPayload_Returns400WithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync(Products, new { sku = "bad sku!", name = "", price = -1m, initialStock = -5 }, Token);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.ReadAs<ValidationProblemDetails>();
        problem.Errors.Keys.ShouldBe(["Sku", "Name", "Price", "InitialStock"], ignoreOrder: true);
    }

    [Fact]
    public async Task Put_ExistingProduct_UpdatesAndPublishesProductUpdated()
    {
        var product = await CreateAsync(NewSku());

        var response = await _client.PutAsJsonAsync($"{Products}/{product.Id}", new { name = "Teclado RGB", price = 399.90m }, Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadAs<ProductResponse>()).Price.ShouldBe(399.90m);
        (await factory.Harness.WaitForPublishedAsync<ProductUpdated>(message => message.ProductId == product.Id)).ShouldBeTrue();
    }

    [Fact]
    public async Task Get_UnknownProduct_Returns404()
    {
        var response = await _client.GetAsync($"{Products}/{Guid.NewGuid()}", Token);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBatch_ReturnsOnlyRequestedProducts()
    {
        var first = await CreateAsync(NewSku());
        var second = await CreateAsync(NewSku());
        await CreateAsync(NewSku());

        var products = await _client.GetFromJsonAsync<List<ProductResponse>>(
            $"{Products}/batch?ids={first.Id}&ids={second.Id}", HttpExtensions.Json, Token);

        products!.Select(product => product.Id).ShouldBe([first.Id, second.Id], ignoreOrder: true);
    }

    [Fact]
    public async Task List_WithSearchByExactSku_FindsProduct()
    {
        var sku = NewSku();
        var product = await CreateAsync(sku);

        var products = await _client.GetFromJsonAsync<List<ProductResponse>>($"{Products}?search={sku}", HttpExtensions.Json, Token);

        products!.ShouldContain(found => found.Id == product.Id);
    }

    [Fact]
    public async Task HealthReady_WithDatabaseAndBus_Returns200()
    {
        var response = await _client.GetAsync("/health/ready", Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
