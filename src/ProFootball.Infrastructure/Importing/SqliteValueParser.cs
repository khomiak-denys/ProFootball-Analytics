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
            long value => checked((int)value),
            int value => value,
            double value => checked((int)value),
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
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
            return dateTimeValue;
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
