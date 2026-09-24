namespace Store.SharedKernel;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unavailable,
}

/// <summary>An expected failure, returned instead of thrown.</summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Unavailable(string code, string message) => new(code, message, ErrorType.Unavailable);
}
