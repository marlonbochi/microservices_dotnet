namespace Store.SharedKernel;

/// <summary>Thrown when a domain invariant is violated (a programming error, not user input).</summary>
public sealed class DomainException(string message) : Exception(message);
