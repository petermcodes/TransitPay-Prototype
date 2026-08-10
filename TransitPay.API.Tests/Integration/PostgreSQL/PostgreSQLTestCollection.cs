using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;
using Xunit.Sdk;

namespace TransitPay.API.Tests.Integration.PostgreSQL;

/// <summary>
/// xUnit collection fixture that manages a PostgreSQL Testcontainer.
/// Starts the container once per test collection and shares the connection string across all tests.
/// </summary>
public class PostgreSQLTestCollection : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private bool _disposed;

    public string ConnectionString { get; private set; } = string.Empty;

    public PostgreSQLTestCollection()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("transitpay_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        
        // Set environment variable so TestWebApplicationFactory can use it
        Environment.SetEnvironmentVariable("POSTGRES_CONNECTION_STRING", ConnectionString);
    }

    public async Task DisposeAsync()
    {
        if (_disposed)
            return;

        await _container.DisposeAsync();
        _disposed = true;
    }
}

/// <summary>
/// Collection definition for PostgreSQL integration tests.
/// Ensures all tests in this collection share the same PostgreSQL container.
/// </summary>
[CollectionDefinition("PostgreSQL collection")]
public class PostgreSQLCollection : ICollectionFixture<PostgreSQLTestCollection>
{
    // This class is used by xUnit to group tests that share the PostgreSQL container
}