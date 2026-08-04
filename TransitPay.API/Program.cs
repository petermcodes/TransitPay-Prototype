using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;
using TransitPay.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Resolve secrets from environment variables ONLY — no hardcoded fallbacks.
// Fail fast at startup if required secrets are missing.
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD")
    ?? throw new InvalidOperationException(
        "DB_PASSWORD environment variable is not set. " +
        "Set it before starting the application (e.g., set DB_PASSWORD=your-db-password).");

// Add services
builder.Services.AddControllers();
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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?.Replace("${DB_PASSWORD}", dbPassword)
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured in appsettings.json.");

builder.Services.AddDbContext<TransitPayDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register services
builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddScoped<ISecurityKeyProvider, SecurityKeyProvider>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IQRService, QRService>();
builder.Services.AddScoped<IPaymentSessionService, PaymentSessionService>();
builder.Services.AddScoped<TransactionReferenceNumberGenerator>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TransitPayDbContext>();
    db.Database.Migrate();

    // Seed roles
    if (!db.Roles.Any())
    {
        db.Roles.AddRange(
            new Role { RoleName = RoleName.Passenger },
            new Role { RoleName = RoleName.Driver },
            new Role { RoleName = RoleName.Admin });
        db.SaveChanges();
    }

    // Seed towns and stations
    if (!db.Towns.Any())
    {
        var town = new Town { TownName = "Lagos", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Towns.Add(town);
        db.SaveChanges();

        var origin = new Station { TownId = town.TownId, StationName = "Central Station", IsActive = true, CreatedAt = DateTime.UtcNow };
        var destination = new Station { TownId = town.TownId, StationName = "Airport Station", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Stations.AddRange(origin, destination);
        db.SaveChanges();

        // Seed fare rules for both directions
        db.FareRules.AddRange(
            new FareRule
            {
                OriginStationId = origin.StationId,
                DestinationStationId = destination.StationId,
                VehicleType = VehicleType.BUS,
                PassengerType = PassengerType.Passenger,
                FareAmount = 12.50m,
                EffectiveDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new FareRule
            {
                OriginStationId = destination.StationId,
                DestinationStationId = origin.StationId,
                VehicleType = VehicleType.BUS,
                PassengerType = PassengerType.Passenger,
                FareAmount = 12.50m,
                EffectiveDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        db.SaveChanges();
    }

    // Seed admin user
    var adminRole = db.Roles.FirstOrDefault(r => r.RoleName == RoleName.Admin);
    if (adminRole != null && !db.Users.Any(u => u.Username == "Admin"))
    {
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();
        var adminUser = new User
        {
            Username = "Admin",
            FirstName = "System",
            LastName = "Administrator",
            MobileNumber = "0000000000",
            PasswordHash = passwordHasher.HashPassword(null!, "Admin"),
            IsActive = true,
            RoleId = adminRole.RoleId,
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
            CardNumber = "4111111111111111",
            Status = CardStatus.ACTIVE,
            PassengerType = PassengerType.Passenger,
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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();