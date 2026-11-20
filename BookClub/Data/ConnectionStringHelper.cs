using Npgsql;

namespace BookClub.Data;

public static class ConnectionStringHelper
{
    public static string ResolveConnectionString(IConfiguration configuration)
    {
        var raw = Environment.GetEnvironmentVariable("DATABASE_URL")
                  ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "Connection string não configurada. Defina ConnectionStrings:DefaultConnection ou a variável de ambiente DATABASE_URL.");
        }

        raw = raw.Trim();

        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = string.IsNullOrWhiteSpace(database) ? "postgres" : database,
            Username = username,
            Password = password,
            Pooling = true
        };

        var isLocal = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                      uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);

        if (!isLocal)
        {
            builder.SslMode = SslMode.Require;
        }

        if (!string.IsNullOrWhiteSpace(uri.Query))
        {
            var pairs = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var kv = pair.Split('=', 2);
                if (kv.Length == 2 && kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                {
                    if (Enum.TryParse<SslMode>(kv[1], ignoreCase: true, out var sslMode))
                    {
                        builder.SslMode = sslMode;
                    }
                }
            }
        }

        return builder.ConnectionString;
    }
}
