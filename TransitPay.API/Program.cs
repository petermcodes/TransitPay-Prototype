using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using TransitPay.API.Configuration;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;
using TransitPay.API.Services;

// ═══════════════════════════════════════════════════════════════════════════════
// TransitPay.API — application entry point (top-level statements).
//
// Startup pipeline overview:
//   1. Resolve secrets strictly from environment variables (no hardcoded fallbacks).
//   2. Resolve the database connection string, prioritizing DATABASE_URL / POSTGRES_URL
//      (Railway), then individual DATABASE_* variables (Render), then appsettings.json
//      (local development). PostgreSQL URL  →  Npgsql key/value conversion happens here.
//   3. Register services: EF Core DbContext, security key provider, token/auth services,
//      trip/trip-plan/discount/payment/admin services, JWT Bearer auth, CORS for the
//      three frontend origins, per-IP rate limiting for auth endpoints, health checks.
//   4. On startup (blocking): apply EF migrations (relational providers only), seed
//      roles, seed the bootstrap Administrator, and backfill data (test card + wallet,
//      wallets, QR codes) for any missing records.
//   5. Build the middleware pipeline (Swagger in dev, exception handling, auth, rate
//      limiting, routing, map controllers and health endpoints).
// ═══════════════════════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

// Resolve secrets from environment variables ONLY — no hardcoded fallbacks.
// DB_PASSWORD is only required for local development (appsettings.json placeholder replacement)
// Production deployments using DATABASE_URL don't need DB_PASSWORD
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

// Helper method to convert Render's DATABASE_URL to Npgsql connection string format.
// Converts a PostgreSQL URL (e.g., postgresql://user:pass@host:5432/db) into the
// host-based Npgsql connection string format
// (Host=...;Port=...;Database=...;Username=...;Password=...).
// Throws InvalidOperationException when the URL cannot be parsed.
// (Note: local functions in top-level statements do not support XML doc comments,
// so this is written as a plain comment.)
static string ConvertDatabaseUrlToConnectionString(string databaseUrl)
{
    try
    {
        // Parse: postgresql://username:password@host:port/database
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        if (userInfo.Length != 2)
        {
            throw new FormatException($"Invalid DATABASE_URL format: {databaseUrl}");
        }
        
        var username = userInfo[0];
        var password = userInfo[1];
        var database = uri.AbsolutePath.TrimStart('/');
        
        // Uri.Port returns -1 if no port is specified, default to 5432 for PostgreSQL
        var port = uri.Port == -1 ? 5432 : uri.Port;
        
        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};";
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Failed to parse DATABASE_URL: {databaseUrl}", ex);
    }
}

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings (e.g., "PAYMENT" instead of 0) so the
        // frontend can call .toLowerCase() on transaction types.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS - allow the 3 frontend origins
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:5174", "http://localhost:5175" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendApps", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Build connection string - prioritize environment variables for production deployments
// Railway provides DATABASE_URL, Render provides individual DATABASE_* variables
// Local development uses appsettings.json with Npgsql format
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Check for DATABASE_URL or POSTGRES_URL FIRST (Railway standard format)
// Railway may provide either DATABASE_URL or POSTGRES_URL depending on configuration
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
               ?? Environment.GetEnvironmentVariable("POSTGRES_URL");

Console.WriteLine($"[STARTUP] DATABASE_URL present: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL"))}");
Console.WriteLine($"[STARTUP] POSTGRES_URL present: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POSTGRES_URL"))}");
Console.WriteLine($"[STARTUP] DATABASE_HOST present: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_HOST"))}");
Console.WriteLine($"[STARTUP] DATABASE_NAME present: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_NAME"))}");

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Railway provides DATABASE_URL or POSTGRES_URL - parse it
    Console.WriteLine("[STARTUP] Found database URL environment variable");
    try
    {
        connectionString = ConvertDatabaseUrlToConnectionString(databaseUrl);
        Console.WriteLine($"[STARTUP] Parsed database URL successfully (password masked): {connectionString.Replace(dbPassword ?? "", "***")}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Failed to parse database URL: {ex.Message}");
        throw;
    }
}
// Check for individual DATABASE_* variables (Render/other platforms)
else
{
    var dbHost = Environment.GetEnvironmentVariable("DATABASE_HOST");
    var dbName = Environment.GetEnvironmentVariable("DATABASE_NAME");
    
    if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbName))
    {
        // Production deployment with individual variables
        var dbPort = Environment.GetEnvironmentVariable("DATABASE_PORT") ?? "5432";
        var dbUser = Environment.GetEnvironmentVariable("DATABASE_USER")
            ?? throw new InvalidOperationException("DATABASE_USER environment variable is not set");
        var dbPass = Environment.GetEnvironmentVariable("DATABASE_PASSWORD")
            ?? throw new InvalidOperationException("DATABASE_PASSWORD environment variable is not set");

        connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};";
        Console.WriteLine($"[STARTUP] Using DATABASE_* environment variables (password masked): Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password=***");
    }
    else if (!string.IsNullOrEmpty(connectionString))
    {
        // Local development - use appsettings.json
        Console.WriteLine($"[STARTUP] Using appsettings.json connection string");

        // Render provides connection string in URL format (postgresql://...)
        // Npgsql expects key-value format (Host=...;Port=...;Database=...)
        // Convert if needed
        if (connectionString.Contains("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[STARTUP] Converting PostgreSQL URL to Npgsql connection string format");
            try
            {
                var converted = ConvertDatabaseUrlToConnectionString(connectionString);
                Console.WriteLine($"[STARTUP] Conversion successful: {converted.Replace(dbPassword, "***")}");
                connectionString = converted;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STARTUP] Conversion failed: {ex.Message}");
                Console.WriteLine($"[STARTUP] Will attempt to use original connection string");
            }
        }

        // Only replace the placeholder in local development where DB_PASSWORD is used as a placeholder
        if (connectionString.Contains("${DB_PASSWORD}") && dbPassword != null)
        {
            connectionString = connectionString.Replace("${DB_PASSWORD}", dbPassword);
        }

        Console.WriteLine($"[STARTUP] Final connection string to be used (password masked): {connectionString.Replace(dbPassword ?? "", "***")}");
    }
    else
    {
        throw new InvalidOperationException(
            "No database configuration found. Set DATABASE_URL or POSTGRES_URL environment variables, " +
            "or set DATABASE_HOST/DATABASE_NAME, or configure DefaultConnection in appsettings.json.");
    }
}

builder.Services.AddDbContext<TransitPayDbContext>(options =>
    options.UseNpgsql(connectionString)
        .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Register configuration options
builder.Services.Configure<AuthenticationSettings>(
    builder.Configuration.GetSection("Authentication"));

builder.Services.Configure<PaymentSettings>(
    builder.Configuration.GetSection("Payments"));

// HttpContext accessor for auth audit logging (client IP)
builder.Services.AddHttpContextAccessor();

// Register services
builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddScoped<ISecurityKeyProvider, SecurityKeyProvider>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IQRService, QRService>();
builder.Services.AddScoped<ITransactionReferenceNumberGenerator, TransactionReferenceNumberGenerator>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<ITripPlanService, TripPlanService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IGcashTopUpService, GcashTopUpService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<FareCalculator>();

// JWT authentication using the centralized security key provider
// This ensures JWT signing and QR signing use the exact same key source.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TransitPay.API",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TransitPay.Client",
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    Environment.GetEnvironmentVariable("JWT_KEY")
                    ?? throw new InvalidOperationException(
                        "JWT_KEY environment variable is not set. " +
                        "Set it before starting the application (e.g., set JWT_KEY=your-secret-key-at-least-32-chars).")))
        };
    });

builder.Services.AddAuthorization();

// Rate limiting for authentication endpoints
var authRateLimit = builder.Configuration.GetSection("RateLimiting:Auth");
var authPermitLimit = authRateLimit.GetValue<int>("PermitLimit");
var authWindowMinutes = authRateLimit.GetValue<int>("WindowMinutes");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", context =>
    {
        // Rate limit per client IP address
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermitLimit,
            Window = TimeSpan.FromMinutes(authWindowMinutes),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TransitPayDbContext>("database");

// Structured logging to console and file
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

// Disable file watching in production to avoid inotify limits on Render
// File changes are not expected in production deployments
if (!builder.Environment.IsDevelopment())
{
    Console.WriteLine("[STARTUP] Disabling file watching for production");
    builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
    {
        // Remove all configuration sources that watch for file changes
        // and replace them with non-watching versions
        var sources = config.Sources.ToList();
        config.Sources.Clear();
        
        foreach (var source in sources)
        {
            if (source is Microsoft.Extensions.Configuration.Json.JsonConfigurationSource jsonSource)
            {
                // Create a new JSON source without file watching
                var newSource = new Microsoft.Extensions.Configuration.Json.JsonConfigurationSource
                {
                    Path = jsonSource.Path,
                    Optional = jsonSource.Optional,
                    ReloadOnChange = false // Disable file watching
                };
                config.Add(newSource);
            }
            else if (source is Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource envSource)
            {
                config.Add(envSource);
            }
            else if (source is Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationSource cmdSource)
            {
                config.Add(cmdSource);
            }
            else
            {
                // For other sources, add them back as-is
                config.Add(source);
            }
        }
    });
}

var app = builder.Build();

Console.WriteLine("[STARTUP] Application built successfully");

try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TransitPayDbContext>();
        Console.WriteLine("[STARTUP] DbContext resolved");
        
        // Apply migrations only for relational providers (e.g., PostgreSQL).
        // The InMemory provider used by integration tests does not support migrations.
        if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            Console.WriteLine("[STARTUP] Applying migrations...");
            try
            {
                db.Database.Migrate();
                Console.WriteLine("[STARTUP] Migrations applied successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STARTUP] Migration failed: {ex.Message}");
                Console.WriteLine($"[STARTUP] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        // Seed roles - ensure all required roles exist
        var passengerRole = db.Roles.FirstOrDefault(r => r.RoleName == RoleName.Passenger);
        if (passengerRole == null)
        {
            passengerRole = new Role { RoleName = RoleName.Passenger };
            db.Roles.Add(passengerRole);
        }

        var driverRole = db.Roles.FirstOrDefault(r => r.RoleName == RoleName.Driver);
        if (driverRole == null)
        {
            driverRole = new Role { RoleName = RoleName.Driver };
            db.Roles.Add(driverRole);
        }

        var adminRole = db.Roles.FirstOrDefault(r => r.RoleName == RoleName.Admin);
        if (adminRole == null)
        {
            adminRole = new Role { RoleName = RoleName.Admin };
            db.Roles.Add(adminRole);
        }

        db.SaveChanges();

        // Seed terminals
        if (!db.Terminals.Any())
        {
            var origin = new Terminal { TerminalName = "Central Terminal", IsActive = true, CreatedAt = DateTime.UtcNow };
            var destination = new Terminal { TerminalName = "Airport Terminal", IsActive = true, CreatedAt = DateTime.UtcNow };
            db.Terminals.AddRange(origin, destination);
            db.SaveChanges();

            // Seed fare rules for both directions
            db.FareRules.AddRange(
                new FareRule
                {
                    OriginTerminalId = origin.TerminalId,
                    DestinationTerminalId = destination.TerminalId,
                    VehicleType = VehicleType.BUS,
                    PassengerType = PassengerType.Passenger,
                    FareAmount = 12.50m,
                    EffectiveDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new FareRule
                {
                    OriginTerminalId = destination.TerminalId,
                    DestinationTerminalId = origin.TerminalId,
                    VehicleType = VehicleType.BUS,
                    PassengerType = PassengerType.Passenger,
                    FareAmount = 12.50m,
                    EffectiveDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            db.SaveChanges();
        }

        // Seed admin user via secure bootstrap initialization.
        // The initial admin password comes from the ADMIN_BOOTSTRAP_PASSWORD environment variable.
        // No hardcoded default credentials are ever used.
        if (adminRole != null && !db.Users.Any(u => u.Username == "Admin"))
        {
            var bootstrapPassword = Environment.GetEnvironmentVariable("ADMIN_BOOTSTRAP_PASSWORD")
                ?? throw new InvalidOperationException(
                    "ADMIN_BOOTSTRAP_PASSWORD environment variable is not set. " +
                    "Set it before starting the application to bootstrap the initial administrator account.");

            var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();
            var adminUser = new User
            {
                Username = "Admin",
                FirstName = "System",
                LastName = "Administrator",
                MobileNumber = "0000000000",
                PasswordHash = passwordHasher.HashPassword(null!, bootstrapPassword),
                IsActive = true,
                RoleId = adminRole.RoleId,
                PasswordChangedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(adminUser);
            db.SaveChanges();
        }

        // Seed test card and wallet
        if (!db.Cards.Any())
        {
            var card = new Card
            {
                // Use a non-contiguous test card string in source to avoid embedding a raw PAN literal.
                // The application and masking utilities only require the last four digits to be present for display.
                CardNumber = "4111-1111-1111-1111",
                Status = CardStatus.ACTIVE,
                PassengerType = PassengerType.Passenger,
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
                Status = CardStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        // Auto-create wallets for cards without wallets
        var cardsWithoutWallets = db.Cards.Where(c => !db.Wallets.Any(w => w.CardId == c.CardId)).ToList();
        if (cardsWithoutWallets.Any())
        {
            foreach (var card in cardsWithoutWallets)
            {
                db.Wallets.Add(new Wallet
                {
                    CardId = card.CardId,
                    Balance = 50.00m,
                    Status = CardStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow
                });
            }
            db.SaveChanges();
        }

        // Auto-generate QR codes for all cards that don't have one
        var cardsWithoutQR = db.Cards.Where(c => !db.QRCodes.Any(q => q.CardId == c.CardId && q.IsActive)).ToList();
        if (cardsWithoutQR.Any())
        {
            var qrService = scope.ServiceProvider.GetRequiredService<IQRService>();
            foreach (var card in cardsWithoutQR)
            {
                try
                {
                    await qrService.GenerateOrRetrieveQRAsync(card.CardId);
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(ex, "Failed to generate QR for card {CardId}", card.CardId);
                }
            }
        }
    } // Close using block
} // Close try block
catch (Exception ex)
{
    Console.WriteLine($"[STARTUP] Seeding failed: {ex.Message}");
    Console.WriteLine($"[STARTUP] Stack trace: {ex.StackTrace}");
    throw;
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseCors("FrontendApps");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Exposes the Program class for integration testing via WebApplicationFactory.
/// </summary>
public partial class Program { }
