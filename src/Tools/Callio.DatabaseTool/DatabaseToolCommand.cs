namespace Callio.DatabaseTool;

internal enum DatabaseToolCommand
{
    MigrateAll,
    SeedTestData
}

internal static class DatabaseToolCommandParser
{
    public static bool TryParse(string[] args, out DatabaseToolCommand command)
    {
        var rawCommand = args.FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(rawCommand))
        {
            command = DatabaseToolCommand.SeedTestData;
            return true;
        }

        switch (rawCommand.ToLowerInvariant())
        {
            case "migrate":
            case "migrate-all":
                command = DatabaseToolCommand.MigrateAll;
                return true;
            case "seed":
            case "seed-test-data":
            case "all":
                command = DatabaseToolCommand.SeedTestData;
                return true;
            case "-h":
            case "--help":
            case "help":
                command = default;
                return false;
            default:
                command = default;
                return false;
        }
    }

    public static string GetUsage()
        => """
Usage:
  dotnet run --project src/Tools/Callio.DatabaseTool
  dotnet run --project src/Tools/Callio.DatabaseTool -- migrate-all
  dotnet run --project src/Tools/Callio.DatabaseTool -- seed-test-data

Commands:
  migrate-all     Apply shared database migrations and ensure every tenant schema store exists.
  seed-test-data  Migrate the shared database, seed sample tenant data, and then apply tenant schema store setup.

Default:
  Running without a command executes seed-test-data.
""";
}
