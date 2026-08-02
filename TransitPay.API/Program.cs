using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TransitPay.API.Data;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;
using TransitPay.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Resolve secrets from environment variables with development fallbacks
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "Akosipm123!";
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "TransitPayPrototypeDevelopmentSecretKey123456";

// Add services
builder.Services.AddControllers();

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
    ?? $"Host=localhost;Port=5432;Database=TransitPayDB;Username=postgres;Password={dbPassword}";

builder.Services.AddDbContext<TransitPayDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKeyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"] ?? "TransitPay.API",
            ValidAudience = jwtSection["Audience"] ?? "TransitPay.Client",
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes)
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

    if (!db.Roles.Any())
    {
        db.Roles.AddRange(
            new Role { RoleName = "Passenger" },
            new Role { RoleName = "Driver" },
            new Role { RoleName = "Admin" });
        db.SaveChanges();
    }

    if (!db.Towns.Any())
    {
        var town = new Town { TownName = "Lagos", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Towns.Add(town);
        db.SaveChanges();

        var origin = new Station { TownId = town.TownId, StationName = "Central Station", IsActive = true, CreatedAt = DateTime.UtcNow };
        var destination = new Station { TownId = town.TownId, StationName = "Airport Station", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Stations.AddRange(origin, destination);
        db.SaveChanges();

        db.FareRules.Add(new FareRule
        {
            OriginStationId = origin.StationId,
            DestinationStationId = destination.StationId,
            VehicleType = "BUS",
            PassengerType = "Passenger",
            FareAmount = 12.50m,
            EffectiveDate = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    var adminRole = db.Roles.FirstOrDefault(r => r.RoleName == "Admin");
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

    if (!db.Cards.Any())
    {
        var card = new Card
        {
            CardNumber = "4111111111111111",
            Status = "ACTIVE",
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
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    var cardsWithoutWallets = db.Cards.Where(c => !db.Wallets.Any(w => w.CardId == c.CardId)).ToList();
    if (cardsWithoutWallets.Any())
    {
        foreach (var card in cardsWithoutWallets)
        {
            db.Wallets.Add(new Wallet
            {
                CardId = card.CardId,
                Balance = 50.00m,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            });
        }
        db.SaveChanges();
    }
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendApps");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();