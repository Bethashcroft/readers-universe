using ReadersRealm.Api.Configuration;

namespace ReadersRealm.Api.Tests;

public class DatabaseConfigTests
{
    [Fact]
    public void ConvertUrlToConnectionString_ParsesNeonStyleUrlIntoNpgsqlFormat()
    {
        var result = DatabaseConfig.ConvertUrlToConnectionString(
            "postgresql://myuser:mypassword@ep-test-123.c-2.eu-west-2.aws.neon.tech/mydb?sslmode=require"
        );

        Assert.Contains("Host=ep-test-123.c-2.eu-west-2.aws.neon.tech", result);
        Assert.Contains("Port=5432", result);
        Assert.Contains("Database=mydb", result);
        Assert.Contains("Username=myuser", result);
        Assert.Contains("Password=mypassword", result);
        Assert.Contains("SSL Mode=Require", result);
    }

    [Fact]
    public void ConvertUrlToConnectionString_HonoursAnExplicitPort()
    {
        var result = DatabaseConfig.ConvertUrlToConnectionString(
            "postgresql://u:p@localhost:6543/dev"
        );

        Assert.Contains("Port=6543", result);
    }

    [Fact]
    public void Normalize_LeavesAKeywordConnectionStringUntouched()
    {
        var keyword = "Host=localhost;Port=5432;Database=dev;Username=u;Password=p";

        Assert.Equal(keyword, DatabaseConfig.Normalize(keyword));
    }

    [Fact]
    public void Normalize_ConvertsAUrlFromEitherSource()
    {
        var result = DatabaseConfig.Normalize(
            "postgresql://u:p@ep-x.eu-west-2.aws.neon.tech/db?sslmode=require"
        );

        Assert.Contains("Host=ep-x.eu-west-2.aws.neon.tech", result);
        Assert.Contains("SSL Mode=Require", result);
    }
}
