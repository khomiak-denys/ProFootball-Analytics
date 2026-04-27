namespace ProFootball.Application.Common.Errors;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Unexpected(string message) => new("common.unexpected", message);
}
