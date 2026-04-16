using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;

namespace ProFootball.Infrastructure.Importing;

internal static class SqliteValueParser
{
    public static int? ReadInt32(SqliteDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return null;
        }

        return reader.GetValue(index) switch
        {
            long value when value >= int.MinValue && value <= int.MaxValue => (int)value,
            long _ => null,
            int value => value,
            double value
                when value >= int.MinValue
                     && value <= int.MaxValue
                     && value == Math.Truncate(value) => (int)value,
            double _ => null,
            string text
                when long.TryParse(text.AsSpan().Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                     && parsed >= int.MinValue
                     && parsed <= int.MaxValue => (int)parsed,
            _ => null,
        };
    }

    public static string? ReadString(SqliteDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index);
        return value switch
        {
            string text => text.Trim(),
            byte[] bytes => Encoding.UTF8.GetString(bytes).Trim(),
            ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span).Trim(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim(),
        };
    }

    public static DateTime? ReadDateTime(SqliteDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index);
        if (value is DateTime dateTimeValue)
        {
            return dateTimeValue.Kind switch
            {
                DateTimeKind.Utc => dateTimeValue,
                DateTimeKind.Local => dateTimeValue.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc),
            };
        }

        if (DateTime.TryParse(
                value.ToString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        return null;
    }
}
