using System.Text.RegularExpressions;
using Store.SharedKernel;

namespace Catalog.Domain.Products;

/// <summary>Stock Keeping Unit: a business identifier, unique per product, always upper-case.</summary>
public sealed partial record Sku
{
    public const int MaxLength = 50;

    private Sku(string value) => Value = value;

    public string Value { get; }

    public static Result<Sku> Create(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length is 0 or > MaxLength || !AllowedCharacters().IsMatch(normalized))
        {
            return ProductErrors.InvalidSku;
        }

        return new Sku(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9-]+$")]
    private static partial Regex AllowedCharacters();
}
