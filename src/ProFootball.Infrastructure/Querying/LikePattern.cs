namespace ProFootball.Infrastructure.Querying;

internal static class LikePattern
{
    public static string Contains(string value) => $"%{Escape(value)}%";

    public static string Exact(string value) => Escape(value);

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
