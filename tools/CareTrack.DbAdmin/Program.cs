using Npgsql;

var defaultConnection = "Host=localhost;Port=5432;Database=lms;Username=postgres;Password=Password@1";
var connectionString = defaultConnection;

var allowedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "admin@apollo.edu",
    "faculty@apollo.edu",
    "admin@meridian.edu",
    "supervisor@meridian.edu",
    "student@meridian.edu"
};

var command = args.FirstOrDefault()?.Trim().ToLowerInvariant();
foreach (var arg in args)
{
    if (arg.StartsWith("Host=", StringComparison.OrdinalIgnoreCase) || arg.Contains("Database=", StringComparison.OrdinalIgnoreCase))
        connectionString = arg;
}

if (command is not ("purge" or "status"))
{
    Console.WriteLine("CareTrack.DbAdmin");
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project tools/CareTrack.DbAdmin/CareTrack.DbAdmin.csproj -- status");
    Console.WriteLine("  dotnet run --project tools/CareTrack.DbAdmin/CareTrack.DbAdmin.csproj -- purge");
    Console.WriteLine("Optional: pass connection string as another arg.");
    return;
}

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

if (command == "status")
{
    var count = await ScalarLongAsync(conn, "SELECT COUNT(*) FROM \"AspNetUsers\";");
    Console.WriteLine($"AspNetUsers rows: {count}");
    return;
}

// PURGE: delete everything except the 5 login users
await using var tx = await conn.BeginTransactionAsync();
try
{
    await ExecAsync(conn, "SET session_replication_role = replica;");

    var emailList = string.Join(", ", allowedEmails.Select(e => $"'{e.Replace("'", "''")}'"));

    // Detach remaining users from tenant entities so we can truncate referenced tables safely.
    await ExecAsync(conn, $@"
UPDATE ""AspNetUsers""
SET ""UniversityId"" = NULL,
    ""CohortId"" = NULL,
    ""StudentId"" = NULL,
    ""SupervisorId"" = NULL
WHERE ""Email"" IN ({emailList});
");

    var tables = await GetPublicTablesAsync(conn);
    var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "AspNetUsers",
        "__EFMigrationsHistory"
    };

    foreach (var table in tables.Where(t => !exclude.Contains(t)))
    {
        await ExecAsync(conn, $@"TRUNCATE TABLE ""{table}"" RESTART IDENTITY CASCADE;");
    }

    await ExecAsync(conn, $@"DELETE FROM ""AspNetUsers"" WHERE ""Email"" NOT IN ({emailList});");

    await ExecAsync(conn, "SET session_replication_role = DEFAULT;");
    await tx.CommitAsync();

    Console.WriteLine("Purge completed. Only the 5 login accounts remain in AspNetUsers.");
}
catch
{
    try { await ExecAsync(conn, "SET session_replication_role = DEFAULT;"); } catch { /* ignore */ }
    await tx.RollbackAsync();
    throw;
}

static async Task<List<string>> GetPublicTablesAsync(NpgsqlConnection conn)
{
    var tables = new List<string>();
    await using var cmd = new NpgsqlCommand(
        """
        SELECT tablename
        FROM pg_tables
        WHERE schemaname = 'public'
        ORDER BY tablename
        """, conn);

    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        tables.Add(reader.GetString(0));
    return tables;
}

static async Task ExecAsync(NpgsqlConnection conn, string sql)
{
    await using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
}

static async Task<long> ScalarLongAsync(NpgsqlConnection conn, string sql)
{
    await using var cmd = new NpgsqlCommand(sql, conn);
    return (long)(await cmd.ExecuteScalarAsync() ?? 0L);
}
