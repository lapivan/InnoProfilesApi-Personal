using Microsoft.EntityFrameworkCore;
using ProfilesApi.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace ProfilesApi.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:99-alpine")
        .WithDatabase("profiles_db_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    public AppDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;

        DbContext = new AppDbContext(options);
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    public static T CreateEntity<T>() where T : class
    {
        return (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
    }
}