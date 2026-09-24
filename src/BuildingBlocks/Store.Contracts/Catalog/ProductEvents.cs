namespace Store.Contracts.Catalog;

/// <summary>Published by Catalog when a product is registered. Carries the initial stock for Inventory.</summary>
public sealed record ProductCreated(
    Guid ProductId,
    string Sku,
    string Name,
    decimal Price,
    int InitialStock,
    DateTimeOffset OccurredAt);

/// <summary>Published by Catalog when product details change.</summary>
public sealed record ProductUpdated(
    Guid ProductId,
    string Name,
    decimal Price,
    DateTimeOffset OccurredAt);
