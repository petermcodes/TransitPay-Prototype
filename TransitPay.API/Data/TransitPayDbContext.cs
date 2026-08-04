using Microsoft.EntityFrameworkCore;
using TransitPay.API.Enums;
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
    public DbSet<QRCode> QRCodes { get; set; }
    public DbSet<PaymentSession> PaymentSessions { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<DiscountType> DiscountTypes { get; set; }
    public DbSet<DiscountApplication> DiscountApplications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Table mappings
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<Card>().ToTable("cards");
        modelBuilder.Entity<Town>().ToTable("towns");
        modelBuilder.Entity<Station>().ToTable("stations");
        modelBuilder.Entity<Wallet>().ToTable("wallets");
        modelBuilder.Entity<Transaction>().ToTable("transactions");
        modelBuilder.Entity<FareRule>().ToTable("fare_rules");
        modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens");
        modelBuilder.Entity<QRCode>().ToTable("qr_codes");
        modelBuilder.Entity<PaymentSession>().ToTable("payment_sessions");
        modelBuilder.Entity<Trip>().ToTable("trips");
        modelBuilder.Entity<DiscountType>().ToTable("discount_types");
        modelBuilder.Entity<DiscountApplication>().ToTable("discount_applications");

        // ── Discount Type relationships ──────────────────────────────────
        modelBuilder.Entity<DiscountType>()
            .HasMany(dt => dt.DiscountApplications)
            .WithOne(da => da.DiscountType)
            .HasForeignKey(da => da.DiscountTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Discount Application relationships ───────────────────────────
        modelBuilder.Entity<DiscountApplication>()
            .HasOne(da => da.Card)
            .WithMany()
            .HasForeignKey(da => da.CardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DiscountApplication>()
            .HasOne(da => da.ApprovedByUser)
            .WithMany()
            .HasForeignKey(da => da.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // ── QR Code constraints ──────────────────────────────────────────
        // Unique constraint on token — no two QR codes can share the same token
        modelBuilder.Entity<QRCode>()
            .HasIndex(q => q.Token)
            .IsUnique();

        // Unique constraint on card_id WHERE is_active = true
        // Only one active QR per card (filtered unique index)
        modelBuilder.Entity<QRCode>()
            .HasIndex(q => new { q.CardId, q.IsActive })
            .IsUnique()
            .HasFilter("is_active = true");

        // ── Card constraints ─────────────────────────────────────────────
        // Unique constraint on card_number — no duplicate card numbers
        modelBuilder.Entity<Card>()
            .HasIndex(c => c.CardNumber)
            .IsUnique();

        // ── User constraints ─────────────────────────────────────────────
        // Unique constraint on mobile_number — no duplicate mobile numbers
        modelBuilder.Entity<User>()
            .HasIndex(u => u.MobileNumber)
            .IsUnique();

        // ── FareRule constraints & relationships ──────────────────────────
        // Unique constraint to prevent duplicate fare rules for the same route
        modelBuilder.Entity<FareRule>()
            .HasIndex(fr => new
            {
                fr.OriginStationId,
                fr.DestinationStationId,
                fr.VehicleType,
                fr.PassengerType
            })
            .IsUnique()
            .HasFilter("is_active = true AND deleted_at IS NULL");

        // FareRule has two FKs to Station (origin and destination)
        // These must be configured explicitly so EF Core can distinguish them.
        modelBuilder.Entity<FareRule>()
            .HasOne(fr => fr.OriginStation)
            .WithMany()
            .HasForeignKey(fr => fr.OriginStationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FareRule>()
            .HasOne(fr => fr.DestinationStation)
            .WithMany()
            .HasForeignKey(fr => fr.DestinationStationId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── PaymentSession relationships ─────────────────────────────────
        // PaymentSession has two FKs to Station (origin and destination)
        modelBuilder.Entity<PaymentSession>()
            .HasOne(ps => ps.OriginStation)
            .WithMany()
            .HasForeignKey(ps => ps.OriginStationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentSession>()
            .HasOne(ps => ps.DestinationStation)
            .WithMany()
            .HasForeignKey(ps => ps.DestinationStationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentSession>()
            .HasOne(ps => ps.Card)
            .WithMany()
            .HasForeignKey(ps => ps.CardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentSession>()
            .HasOne(ps => ps.User)
            .WithMany()
            .HasForeignKey(ps => ps.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Filtered unique index: only one active session (PENDING/SCANNING/PROCESSING) per card
        // Status enum values: PENDING=0, SCANNING=1, PROCESSING=2
        modelBuilder.Entity<PaymentSession>()
            .HasIndex(ps => ps.CardId)
            .IsUnique()
            .HasFilter("status IN (0, 1, 2)");

        // ── Transaction relationships ─────────────────────────────────────
        // The Transaction has two FKs to Station:
        //   - StationId (destination, via Station nav property)
        //   - OriginStationId (origin, via OriginStation nav property)
        // These must be configured explicitly so EF Core can distinguish them.
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Station)
            .WithMany(s => s.Transactions)
            .HasForeignKey(t => t.StationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.OriginStation)
            .WithMany() // No inverse navigation on Station for origin transactions
            .HasForeignKey(t => t.OriginStationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.PaymentSession)
            .WithMany()
            .HasForeignKey(t => t.PaymentSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Driver)
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Trip)
            .WithMany(t => t.Transactions)
            .HasForeignKey(t => t.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Trip relationships ────────────────────────────────────────────
        // Trip has two FKs to Station (origin and final destination)
        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Driver)
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.OriginStation)
            .WithMany()
            .HasForeignKey(t => t.OriginStationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.FinalDestinationStation)
            .WithMany()
            .HasForeignKey(t => t.FinalDestinationStationId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Trip indexes ──────────────────────────────────────────────────
        // Index on driver_id for fast trip lookups by driver
        modelBuilder.Entity<Trip>()
            .HasIndex(t => t.DriverId);

        // Filtered unique index: only one ACTIVE trip per conductor/driver
        // TripStatus enum values: Pending=0, Active=1, Completed=2, Cancelled=3
        modelBuilder.Entity<Trip>()
            .HasIndex(t => t.DriverId)
            .IsUnique()
            .HasFilter("trip_status = 1");

        // ── Transaction indexes ───────────────────────────────────────────
        // Index on card_id for fast transaction lookups by card
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.CardId);

        // Index on trip_id for fast transaction lookups by trip
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.TripId);

        // Index on fare_id for fast lookups by fare rule
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.FareId);

        // Unique index on TransactionReferenceNumber (TRN) — no duplicates allowed
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.TransactionReferenceNumber)
            .IsUnique()
            .HasFilter("transaction_reference_number IS NOT NULL");

        // ── Wallet constraints ────────────────────────────────────────────
        // One wallet per card (unique constraint)
        modelBuilder.Entity<Wallet>()
            .HasIndex(w => w.CardId)
            .IsUnique();
    }
}