using System.Globalization;
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
            double value when value >= int.MinValue && value <= int.MaxValue => (int)value,
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

        return reader.GetString(index).Trim();
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
