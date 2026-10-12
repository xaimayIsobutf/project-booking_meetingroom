using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace RoomReservation.Infrastructure.Persistence;

public static class MigrationRunner
{
    public static async Task Run(AppDbContext db)
    {
        var migrationsPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "Persistence", "Migrations");

        if (!Directory.Exists(migrationsPath))
            throw new DirectoryNotFoundException($"Database migrations directory was not published: {migrationsPath}");

        foreach (var migration in Directory.GetFiles(migrationsPath, "*.sql").OrderBy(Path.GetFileName))
        {
            var sql = await File.ReadAllTextAsync(migration);
            // Execute the migration as literal SQL. ExecuteSqlRawAsync treats
            // braces such as PostgreSQL JSON operators/functions as format
            // placeholders, which can crash startup on valid migration files.
            var connection = db.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}
