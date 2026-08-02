using Microsoft.EntityFrameworkCore;
using TransitPay.API.Models;

namespace TransitPay.API.Data;

public class TransitPayDbContext : DbContext
{
    public TransitPayDbContext(DbContextOptions<TransitPayDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Card> Cards { get; set; }
    public DbSet<Town> Towns { get; set; }
    public DbSet<Station> Stations { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<FareRule> FareRules { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<Card>().ToTable("cards");
        modelBuilder.Entity<Town>().ToTable("towns");
        modelBuilder.Entity<Station>().ToTable("stations");
        modelBuilder.Entity<Wallet>().ToTable("wallets");
        modelBuilder.Entity<Transaction>().ToTable("transactions");
        modelBuilder.Entity<FareRule>().ToTable("fare_rules");
        modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens");
    }
}