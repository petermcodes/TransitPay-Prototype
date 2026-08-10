using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TransitPay.API.Data;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Tests.Integration;

/// <summary>
/// WebApplicationFactory that overrides the database to use EF Core InMemory
/// and replaces the TRN generator with a deterministic test double.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
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

        // Override rate limiting for tests (avoid 429s from repeated auth calls)
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "10000");
        builder.UseSetting("RateLimiting:Auth:WindowMinutes", "60");

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext configuration registrations to fully clean
            // the Npgsql provider registered in Program.cs
            services.RemoveAll<DbContextOptions<TransitPayDbContext>>();
            services.RemoveAll<TransitPayDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<TransitPayDbContext>>();

            // Register InMemory DbContext with a unique name per factory instance
            // Suppress the TransactionIgnoredWarning since InMemory doesn't support transactions.
            // Transaction semantics are verified in PostgreSQL integration tests.
            services.AddDbContext<TransitPayDbContext>(options =>
                options.UseInMemoryDatabase(_dbName)
                    .ConfigureWarnings(warnings => warnings.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));

            // Replace the TRN generator with a deterministic test double
            services.RemoveAll<ITransactionReferenceNumberGenerator>();
            services.AddScoped<ITransactionReferenceNumberGenerator, TestTrnGenerator>();
        });
    }

    /// <summary>
    /// Resets the InMemory database to a clean, seeded state.
    /// </summary>
    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransitPayDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        SeedDatabase(db);
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
            CreatedAt = DateTime.UtcNow
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
/// Deterministic test double for the TRN generator that produces unique,
/// well-formed TRNs without requiring PostgreSQL-specific SQL.
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