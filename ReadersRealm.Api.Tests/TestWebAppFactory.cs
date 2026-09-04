using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReadersRealm.Api.Data;

namespace ReadersRealm.Api.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public StubCoverHandler Handler { get; } = new();

    public TestWebAppFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", "TestSignKeyThatIsAtLeast32CharsLong!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "ReadersUniverse");
        Environment.SetEnvironmentVariable("Jwt__Audience", "ReadersUniverseUsers");
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d =>
            d.ServiceType == typeof(DbContextOptions<AppDbContext>)
            );

            if(descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            var lookupDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(ReadersRealm.Api.Services.IBookLookup)
            );
            if (lookupDescriptor != null)
            {
                services.Remove(lookupDescriptor);
            }
            services.AddSingleton<ReadersRealm.Api.Services.IBookLookup, FakeBookLookup>();

            var coverDescriptors = services
                .Where(d => d.ServiceType == typeof(ReadersRealm.Api.Services.ICoverSource))
                .ToList();
            foreach (var coverDescriptor in coverDescriptors)
            {
                services.Remove(coverDescriptor);
            }
            services.AddSingleton<ReadersRealm.Api.Services.ICoverSource, FakeCoverSource>();

            var coverServiceDescriptors = services
                .Where(d => d.ServiceType == typeof(ReadersRealm.Api.Services.CoverService))
                .ToList();
            foreach (var coverServiceDescriptor in coverServiceDescriptors)
            {
                services.Remove(coverServiceDescriptor);
            }

            services
                .AddHttpClient<ReadersRealm.Api.Services.CoverService>()
                .ConfigurePrimaryHttpMessageHandler(() => Handler);

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}