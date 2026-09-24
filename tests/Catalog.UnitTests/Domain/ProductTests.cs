using Catalog.Domain.Products;

namespace Catalog.UnitTests.Domain;

public sealed class ProductTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly Sku ValidSku = Sku.Create("KB-001").Value;

    [Fact]
    public void Create_WithValidData_ReturnsProductWithTrimmedValues()
    {
        var result = Product.Create(ValidSku, "  Teclado  ", "  ABNT2 ", 350.456m, Now);

        result.IsSuccess.ShouldBeTrue();
        var product = result.Value;
        product.Id.ShouldNotBe(Guid.Empty);
        product.Name.ShouldBe("Teclado");
        product.Description.ShouldBe("ABNT2");
        product.Price.ShouldBe(350.46m);
        product.CreatedAt.ShouldBe(Now);
        product.UpdatedAt.ShouldBe(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ReturnsInvalidName(string name)
    {
        var result = Product.Create(ValidSku, name, null, 10m, Now);

        result.Error.ShouldBe(ProductErrors.InvalidName);
    }

    [Fact]
    public void Create_WithTooLongName_ReturnsInvalidName()
    {
        var result = Product.Create(ValidSku, new string('a', Product.NameMaxLength + 1), null, 10m, Now);

        result.Error.ShouldBe(ProductErrors.InvalidName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositivePrice_ReturnsInvalidPrice(decimal price)
    {
        var result = Product.Create(ValidSku, "Mouse", null, price, Now);

        result.Error.ShouldBe(ProductErrors.InvalidPrice);
    }

    [Fact]
    public void Create_WithTooLongDescription_ReturnsInvalidDescription()
    {
        var result = Product.Create(ValidSku, "Mouse", new string('d', Product.DescriptionMaxLength + 1), 10m, Now);

        result.Error.ShouldBe(ProductErrors.InvalidDescription);
    }

    [Fact]
    public void Update_WithValidData_ChangesDetailsAndKeepsSku()
    {
        var product = Product.Create(ValidSku, "Mouse", null, 10m, Now).Value;
        var later = Now.AddHours(1);

        var result = product.Update("Mouse Gamer", "RGB", 99.9m, later);

        result.IsSuccess.ShouldBeTrue();
        product.Name.ShouldBe("Mouse Gamer");
        product.Description.ShouldBe("RGB");
        product.Price.ShouldBe(99.9m);
        product.Sku.ShouldBe(ValidSku);
        product.CreatedAt.ShouldBe(Now);
        product.UpdatedAt.ShouldBe(later);
    }

    [Fact]
    public void Update_WithInvalidPrice_KeepsPreviousState()
    {
        var product = Product.Create(ValidSku, "Mouse", null, 10m, Now).Value;

        var result = product.Update("Mouse", null, -5m, Now.AddHours(1));

        result.IsFailure.ShouldBeTrue();
        product.Price.ShouldBe(10m);
        product.UpdatedAt.ShouldBe(Now);
    }
}
