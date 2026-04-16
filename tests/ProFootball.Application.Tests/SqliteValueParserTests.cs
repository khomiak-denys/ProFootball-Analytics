using Microsoft.Data.Sqlite;
using ProFootball.Infrastructure.Importing;
using Xunit;

namespace ProFootball.Application.Tests;

public class SqliteValueParserTests
{
    [Fact]
    public async Task ReadInt32_ShouldParseIntegerAndNumericText()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE parser_case (value_a INTEGER, value_b TEXT, value_c TEXT, value_d REAL, value_e REAL)";
            await command.ExecuteNonQueryAsync();
        }

        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText = "INSERT INTO parser_case (value_a, value_b, value_c, value_d, value_e) VALUES (42, ' 17 ', 'abc', 1.9, 18.0)";
            await insert.ExecuteNonQueryAsync();
        }

        await using var select = connection.CreateCommand();
        select.CommandText = "SELECT value_a, value_b, value_c, value_d, value_e FROM parser_case";
        await using var reader = await select.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());

        Assert.Equal(42, SqliteValueParser.ReadInt32(reader, 0));
        Assert.Equal(17, SqliteValueParser.ReadInt32(reader, 1));
        Assert.Null(SqliteValueParser.ReadInt32(reader, 2));
        Assert.Null(SqliteValueParser.ReadInt32(reader, 3));
        Assert.Equal(18, SqliteValueParser.ReadInt32(reader, 4));
    }

    [Fact]
    public async Task ReadStringAndDateTime_ShouldTrimAndParse()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE parser_case (value_text TEXT, value_date TEXT, value_bad_date TEXT)";
            await command.ExecuteNonQueryAsync();
        }

        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText = "INSERT INTO parser_case (value_text, value_date, value_bad_date) VALUES ('  test  ', '2015-01-01 00:00:00', 'not-a-date')";
            await insert.ExecuteNonQueryAsync();
        }

        await using var select = connection.CreateCommand();
        select.CommandText = "SELECT value_text, value_date, value_bad_date FROM parser_case";
        await using var reader = await select.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());

        Assert.Equal("test", SqliteValueParser.ReadString(reader, 0));
        Assert.Equal(new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc), SqliteValueParser.ReadDateTime(reader, 1));
        Assert.Null(SqliteValueParser.ReadDateTime(reader, 2));
    }

    [Fact]
    public async Task ReadString_ShouldHandleNonStringSQLiteValues()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE parser_case (value_numeric INTEGER, value_blob BLOB)";
            await command.ExecuteNonQueryAsync();
        }

        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText = "INSERT INTO parser_case (value_numeric, value_blob) VALUES (42, X'207465737420')";
            await insert.ExecuteNonQueryAsync();
        }

        await using var select = connection.CreateCommand();
        select.CommandText = "SELECT value_numeric, value_blob FROM parser_case";
        await using var reader = await select.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());

        Assert.Equal("42", SqliteValueParser.ReadString(reader, 0));
        Assert.Equal("test", SqliteValueParser.ReadString(reader, 1));
    }
}
