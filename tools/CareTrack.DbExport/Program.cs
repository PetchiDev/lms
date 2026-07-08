using System.Text;
using Npgsql;

var defaultConnection = "Host=localhost;Port=5432;Database=lms;Username=postgres;Password=Password@1";
var connectionString = defaultConnection;
var outputPath = Path.Combine("database", "caretrack_data.sql");

foreach (var arg in args)
{
    if (arg.StartsWith("Host=", StringComparison.OrdinalIgnoreCase) || arg.Contains("Database=", StringComparison.OrdinalIgnoreCase))
        connectionString = arg;
    else if (arg.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
        outputPath = arg;
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

var tables = new List<string>();
await using (var cmd = new NpgsqlCommand(
    """
    SELECT tablename
    FROM pg_tables
    WHERE schemaname = 'public'
    ORDER BY tablename
    """, conn))
await using (var reader = await cmd.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
        tables.Add(reader.GetString(0));
}

var sb = new StringBuilder();
sb.AppendLine("-- CareTrack LMS data export");
sb.AppendLine($"-- Generated: {DateTime.UtcNow:O}");
sb.AppendLine($"-- Tables: {tables.Count}");
sb.AppendLine();
sb.AppendLine("SET session_replication_role = replica;");
sb.AppendLine();

foreach (var table in tables)
{
    if (string.Equals(table, "__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase))
        continue;

    var columns = new List<(string Name, string DataType)>();
    await using (var colCmd = new NpgsqlCommand(
        """
        SELECT column_name, data_type
        FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = @table
        ORDER BY ordinal_position
        """, conn))
    {
        colCmd.Parameters.AddWithValue("table", table);
        await using var colReader = await colCmd.ExecuteReaderAsync();
        while (await colReader.ReadAsync())
            columns.Add((colReader.GetString(0), colReader.GetString(1)));
    }

    if (columns.Count == 0)
        continue;

    var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{table}\"", conn);
    var rowCount = (long)(await countCmd.ExecuteScalarAsync() ?? 0L);

    sb.AppendLine($"-- Table: {table} ({rowCount} rows)");
    if (rowCount == 0)
    {
        sb.AppendLine();
        continue;
    }

    var colList = string.Join(", ", columns.Select(c => $"\"{c.Name}\""));
    await using var dataCmd = new NpgsqlCommand($"SELECT {colList} FROM \"{table}\"", conn);
    await using var dataReader = await dataCmd.ExecuteReaderAsync();

    while (await dataReader.ReadAsync())
    {
        var values = new string[columns.Count];
        for (var i = 0; i < columns.Count; i++)
        {
            values[i] = FormatSqlValue(dataReader.IsDBNull(i) ? null : dataReader.GetValue(i), columns[i].DataType);
        }

        sb.AppendLine($"INSERT INTO \"{table}\" ({colList}) VALUES ({string.Join(", ", values)});");
    }

    sb.AppendLine();
}

sb.AppendLine("SET session_replication_role = DEFAULT;");

await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"Exported {tables.Count} tables to {Path.GetFullPath(outputPath)}");

static string FormatSqlValue(object? value, string dataType)
{
    if (value is null)
        return "NULL";

    return value switch
    {
        bool b => b ? "TRUE" : "FALSE",
        byte or sbyte or short or int or long or float or double or decimal =>
            Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!,
        Guid g => $"'{g}'",
        DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.ffffff}'",
        DateTimeOffset dto => $"'{dto.UtcDateTime:yyyy-MM-dd HH:mm:ss.ffffff}+00'",
        DateOnly d => $"'{d:yyyy-MM-dd}'",
        TimeOnly t => $"'{t:HH:mm:ss.ffffff}'",
        byte[] bytes => $"'\\x{Convert.ToHexString(bytes)}'",
        string s => $"'{s.Replace("'", "''")}'",
        _ when dataType is "json" or "jsonb" => $"'{value.ToString()!.Replace("'", "''")}'",
        _ => $"'{value.ToString()!.Replace("'", "''")}'"
    };
}
