using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TransitPay.API.Data;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Tests.Integration.PostgreSQL;

/// <summary>
/// WebApplicationFactory that uses real PostgreSQL database via Testcontainers.
/// Used for integration tests that require database transactions, constraints, and real concurrency.
/// </summary>
public class PostgreSQLTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"IntegrationDb_{Guid.NewGuid():N}";

    /// <summary>
    /// The admin bootstrap password used by the test factory.
    /// </summary>
    public const string AdminBootstrapPassword = "Secur3AdminP@ss!";

    /// <summary>
    /// The driver password used by the test factory.
    /// </summary>
    public const string DriverPassword = "Driv3rT3st!Pass";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set required environment variables before the app builds
        Environment.SetEnvironmentVariable("DB_PASSWORD", "test-db-password");
        Environment.SetEnvironmentVariable("JWT_KEY", "test-secret-key-at-least-32-characters-long-for-testing");
        Environment.SetEnvironmentVariable("ADMIN_BOOTSTRAP_PASSWORD", AdminBootstrapPassword);

        // Override rate limiting for tests
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "10000");
        builder.UseSetting("RateLimiting:Auth:WindowMinutes", "60");

        builder.ConfigureServices(services =>
        {
            // Get the PostgreSQL connection string from the test collection
            var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "POSTGRES_CONNECTION_STRING environment variable not set. " +
                    "Ensure PostgreSQLTestCollection is running.");
            }

            // Remove ALL DbContext configuration registrations
            services.RemoveAll<DbContextOptions<TransitPayDbContext>>();
            services.RemoveAll<TransitPayDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<TransitPayDbContext>>();

            // Register PostgreSQL DbContext
            services.AddDbContext<TransitPayDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Replace the TRN generator with a deterministic test double
            services.RemoveAll<ITransactionReferenceNumberGenerator>();
            services.AddScoped<ITransactionReferenceNumberGenerator, TestTrnGenerator>();
        });
    }

    /// <summary>
    /// Resets the PostgreSQL database to a clean, seeded state.
    /// </summary>
    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransitPayDbContext>();
        
        // Delete all data and reset sequences
        db.Database.ExecuteSqlRaw("DELETE FROM transactions CASCADE");
        db.Database.ExecuteSqlRaw("DELETE FROM trips CASCADE");
        db.Database.ExecuteSqlRaw("DELETE FROM trip_plans CASCADE");
        db.Database.ExecuteSqlRaw("DELETE FROM qr_codes CASCADE");
        db.Database.ExecuteSqlRaw("DELETE FROM wallets CASCADE");
        db.Database.ExecuteSqlRaw("DELETE FROM cards CASCADE");
        db.Database.ExecuteSqlRaw("DELETE FROM passenger_discounts CASCADE");
        db.Database.ExecuteSqlRaw("DELETE FROM discount_applications CASCADE");
        db.Database.ExecuteSqlRaw("DELETE FROM fare_rules CASCADE");
        db.Database.ExecuteSqlRaw("DELETE FROM terminals CASCADE");
        db.Database.ExecuteSqlRaw("DELETE FROM users CASCADE");
        db.Database.ExecuteSqlRaw("DELETE FROM roles CASCADE");
        db.Database.ExecuteSqlRaw("ALTER SEQUENCE users_user_id_seq RESTART WITH 1");
        db.Database.ExecuteSqlRaw("ALTER SEQUENCE cards_card_id_seq RESTART WITH 1");
        db.Database.ExecuteSqlRaw("ALTER SEQUENCE wallets_wallet_id_seq RESTART WITH 1");
        db.Database.ExecuteSqlRaw("ALTER SEQUENCE terminals_terminal_id_seq RESTART WITH 1");
        db.Database.ExecuteSqlRaw("ALTER SEQUENCE fare_rules_fare_id_seq RESTART WITH 1");
        db.Database.ExecuteSqlRaw("ALTER SEQUENCE trips_trip_id_seq RESTART WITH 1");
        db.Database.ExecuteSqlRaw("ALTER SEQUENCE transactions_transaction_id_seq RESTART WITH 1");
        
        SeedDatabase(db);
    }

    /// <summary>
    /// Creates an HttpClient with the specified JWT bearer token.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static void SeedDatabase(TransitPayDbContext db)
    {
        // Roles
        var passengerRole = new Role { RoleName = Enums.RoleName.Passenger };
        var driverRole = new Role { RoleName = Enums.RoleName.Driver };
        var adminRole = new Role { RoleName = Enums.RoleName.Admin };
        db.Roles.AddRange(passengerRole, driverRole, adminRole);
        db.SaveChanges();

        // Terminals
        var central = new Terminal { TerminalName = "Central Terminal", IsActive = true, CreatedAt = DateTime.UtcNow };
        var airport = new Terminal { TerminalName = "Airport Terminal", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Terminals.AddRange(central, airport);
        db.SaveChanges();

        // Fare rules
        db.FareRules.AddRange(
            new FareRule
            {
                OriginTerminalId = central.TerminalId,
                DestinationTerminalId = airport.TerminalId,
                VehicleType = Enums.VehicleType.BUS,
                PassengerType = Enums.PassengerType.Passenger,
                FareAmount = 12.50m,
                EffectiveDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new FareRule
            {
                OriginTerminalId = airport.TerminalId,
                DestinationTerminalId = central.TerminalId,
                VehicleType = Enums.VehicleType.BUS,
                PassengerType = Enums.PassengerType.Passenger,
                FareAmount = 12.50m,
                EffectiveDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        db.SaveChanges();

        // Driver user (for trip management and payment tests)
        var driverHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var driverUser = new User
        {
            Username = "DriverTest",
            FirstName = "Driver",
            LastName = "User",
            MobileNumber = "09170000001",
            PasswordHash = driverHasher.HashPassword(null!, DriverPassword),
            IsActive = true,
            RoleId = driverRole.RoleId,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(driverUser);
        db.SaveChanges();

        // Admin user
        var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var adminUser = new User
        {
            Username = "Admin",
            FirstName = "System",
            LastName = "Administrator",
            MobileNumber = "0000000000",
            PasswordHash = passwordHasher.HashPassword(null!, AdminBootstrapPassword),
            IsActive = true,
            RoleId = adminRole.RoleId,
            PasswordChangedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(adminUser);
        db.SaveChanges();

        // Test card + wallet
        var card = new Card
        {
            CardNumber = "4111-1111-1111-1111",
            Status = Enums.CardStatus.ACTIVE,
            PassengerType = Enums.PassengerType.Passenger,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(2),
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };
        db.Cards.Add(card);
        db.SaveChanges();

        db.Wallets.Add(new Wallet
        {
            CardId = card.CardId,
            Balance = 50.00m,
            Status = Enums.CardStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}

/// <summary>
/// Deterministic test double for the TRN generator that produces unique, well-formed TRNs.
/// </summary>
public class TestTrnGenerator : ITransactionReferenceNumberGenerator
{
    private static int _counter = 0;

    public Task<string> GenerateNextAsync()
    {
        var seq = Interlocked.Increment(ref _counter);
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        return Task.FromResult($"TRN-{date}-{seq:D6}");
    }
}