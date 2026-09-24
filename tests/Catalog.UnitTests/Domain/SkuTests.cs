using Catalog.Domain.Products;

namespace Catalog.UnitTests.Domain;

public sealed class SkuTests
{
    [Theory]
    [InlineData("kb-001", "KB-001")]
    [InlineData("  abc123 ", "ABC123")]
    public void Create_WithValidValue_NormalizesToUpperCase(string input, string expected)
    {
        var result = Sku.Create(input);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("KB 001")]
    [InlineData("KB_001")]
    [InlineData("ÇÃO")]
    public void Create_WithInvalidValue_ReturnsInvalidSku(string? input)
    {
        Sku.Create(input).Error.ShouldBe(ProductErrors.InvalidSku);
    }

    [Fact]
    public void Create_WithTooLongValue_ReturnsInvalidSku()
    {
        Sku.Create(new string('A', Sku.MaxLength + 1)).Error.ShouldBe(ProductErrors.InvalidSku);
    }

    [Fact]
    public void Equality_SameNormalizedValue_AreEqual()
    {
        Sku.Create("kb-1").Value.ShouldBe(Sku.Create("KB-1").Value);
    }
}
