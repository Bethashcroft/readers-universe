namespace ReadersRealm.Api.Configuration;

public static class DatabaseConfig
{
    public static string ResolveConnectionString(IConfiguration configuration)
    {
        var raw = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrEmpty(raw))
        {
            raw = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrEmpty(raw))
        {
            throw new InvalidOperationException(
                "No database connection string configured. Set DATABASE_URL or ConnectionStrings:DefaultConnection."
            );
        }

        return Normalize(raw);
    }

    public static string Normalize(string connectionStringOrUrl)
    {
        var isUrl =
            connectionStringOrUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || connectionStringOrUrl.StartsWith(
                "postgresql://",
                StringComparison.OrdinalIgnoreCase
            );

        return isUrl ? ConvertUrlToConnectionString(connectionStringOrUrl) : connectionStringOrUrl;
    }

    public static string ConvertUrlToConnectionString(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        return $"Host={uri.Host};Port={port};Database={database};"
            + $"Username={user};Password={password};"
            + "SSL Mode=Require;Trust Server Certificate=true";
    }
}
