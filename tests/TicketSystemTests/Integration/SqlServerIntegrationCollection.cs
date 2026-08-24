using Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace TicketSystemTests.Integration;

[CollectionDefinition("SQL Server integration", DisableParallelization = true)]
public sealed class SqlServerIntegrationCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server integration";
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string DatabaseName = "TicketSystemIntegrationTests";
    private const string Password = "Fake-Test1234@!";

    private readonly MsSqlContainer _container = new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
        .WithPassword(Password)
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var masterConnectionString = new SqlConnectionStringBuilder(
            _container.GetConnectionString())
        {
            InitialCatalog = "master"
        }.ConnectionString;

        await using (var connection = new SqlConnection(masterConnectionString))
        {
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"IF DB_ID(N'{DatabaseName}') IS NULL CREATE DATABASE [{DatabaseName}];";
            await command.ExecuteNonQueryAsync();
        }

        ConnectionString = new SqlConnectionStringBuilder(
            _container.GetConnectionString())
        {
            InitialCatalog = DatabaseName,
            TrustServerCertificate = true
        }.ConnectionString;

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                ConnectionString,
                sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null))
            .Options;

        return new AppDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [TicketPurchases]; DELETE FROM [PricingTiers]; DELETE FROM [Events];");
    }
}

[Collection(SqlServerIntegrationCollection.Name)]
public abstract class SqlServerIntegrationTestBase : IAsyncLifetime
{
    protected SqlServerIntegrationTestBase(SqlServerFixture fixture)
    {
        Fixture = fixture;
    }

    protected SqlServerFixture Fixture { get; }

    public virtual Task InitializeAsync() => Fixture.ResetDatabaseAsync();

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected static Domain.Entities.Event CreateEvent(
        int capacity = 100,
        decimal generalPrice = 25m,
        decimal vipPrice = 100m)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var future = new DateTime(2030, 1, 1, 19, 0, 0, DateTimeKind.Utc);
        var futureDate = DateOnly.FromDateTime(future);
        var futureTime = TimeOnly.FromDateTime(future);

        return Domain.Entities.Event.Create(
            "Integration Test Event",
            "An event used by SQL Server integration tests.",
            "London",
            futureDate,
            futureTime,
            capacity,
            [
                new Domain.Models.PricingTierDefinition("General", generalPrice),
                new Domain.Models.PricingTierDefinition("VIP", vipPrice)
            ],
            now);
    }
}
