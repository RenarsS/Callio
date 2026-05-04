using Microsoft.Data.SqlClient;

namespace Callio.Provisioning.Infrastructure.Services;

public static class SqlServerTransientRetry
{
    public const string MigrationsHistorySchema = "dbo";
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";
    public const int MaxRetryCount = 10;
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);
    public static readonly int[] AdditionalErrorNumbers = [40613];

    private static readonly HashSet<int> TransientErrorNumbers =
    [
        20,
        64,
        233,
        4060,
        10928,
        10929,
        40143,
        40197,
        40501,
        40613,
        49918,
        49919,
        49920,
        10053,
        10054,
        10060,
        11001
    ];

    public static async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        for (var retry = 0; ; retry++)
        {
            try
            {
                await operation(cancellationToken);
                return;
            }
            catch (SqlException exception) when (retry < MaxRetryCount && IsTransient(exception))
            {
                await Task.Delay(GetRetryDelay(retry), cancellationToken);
            }
        }
    }

    public static bool IsTransient(SqlException exception)
        => exception.Errors
            .Cast<SqlError>()
            .Any(error => TransientErrorNumbers.Contains(error.Number));

    private static TimeSpan GetRetryDelay(int retry)
    {
        var seconds = Math.Min(MaxRetryDelay.TotalSeconds, Math.Pow(2, retry + 1));
        return TimeSpan.FromSeconds(seconds);
    }
}
