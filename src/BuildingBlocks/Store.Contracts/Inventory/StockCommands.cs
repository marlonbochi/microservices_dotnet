using Store.Contracts.Ordering;

namespace Store.Contracts.Inventory;

/// <summary>Command: reserve every line of the order (all-or-nothing).</summary>
public sealed record ReserveStock(Guid OrderId, IReadOnlyList<OrderLine> Items);

/// <summary>Command: turn the reservation into a definitive stock deduction.</summary>
public sealed record CommitStock(Guid OrderId);

/// <summary>Compensation command: give the reserved units back.</summary>
public sealed record ReleaseStock(Guid OrderId);
