using Microsoft.EntityFrameworkCore;
using TransitPay.API.Enums;
using TransitPay.API.Models;
using TransitPay.API.Models.History;

namespace TransitPay.API.Data;

/// <summary>
/// EF Core database context for TransitPay.
/// Maps all domain entities to their PostgreSQL tables (snake_case), configures:
///   - CHECK constraints (non-negative balances/fares, percentage ranges)
///   - soft-delete query filters (DeletedAt == null)
///   - unique / filtered-unique indexes (one active QR per card, one active plan per card, etc.)
///   - relationship behaviours (Restrict deletes to protect audit history)
/// Entity names are serialized via [Column] attributes; this fluent configuration layer
/// is the single source of truth for table-level constraints that cannot be expressed
/// with attributes alone.
/// </summary>
public class TransitPayDbContext : DbContext
{
    /// <summary>
    /// Creates a new database context with the given options.
    /// </summary>
    /// <param name="options">The DbContext options (connection, provider, etc.).</param>
    public TransitPayDbContext(DbContextOptions<TransitPayDbContext> options)
        : base(options)
    {
    }

    /// <summary>Application roles (Passenger, Driver, Admin).</summary>
    public DbSet<Role> Roles { get; set; }

    /// <summary>User accounts (passengers, drivers, administrators).</summary>
    public DbSet<User> Users { get; set; }

    /// <summary>Physical transit cards.</summary>
    public DbSet<Card> Cards { get; set; }

    /// <summary>Bus terminals/stations referenced by routes and fare rules.</summary>
    public DbSet<Terminal> Terminals { get; set; }

    /// <summary>Stored-value wallets bound one-to-one to cards.</summary>
    public DbSet<Wallet> Wallets { get; set; }

    /// <summary>Financial transactions (fare payments and top-ups).</summary>
    public DbSet<Transaction> Transactions { get; set; }

    /// <summary>Fare matrix entries (route × vehicle × passenger type → fare).</summary>
    public DbSet<FareRule> FareRules { get; set; }

    /// <summary>Refresh tokens for JWT re-authentication.</summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    /// <summary>Permanent per-card QR codes.</summary>
    public DbSet<QRCode> QRCodes { get; set; }

    /// <summary>Driver trips (journeys) that collect payments.</summary>
    public DbSet<Trip> Trips { get; set; }

    /// <summary>Passenger trip plans (locked fares for a boarding route).</summary>
    public DbSet<TripPlan> TripPlans { get; set; }

    /// <summary>Configurable discount types.</summary>
    public DbSet<DiscountType> DiscountTypes { get; set; }

    /// <summary>Passenger discount applications (approval workflow).</summary>
    public DbSet<DiscountApplication> DiscountApplications { get; set; }

    /// <summary>Configurable discount programs.</summary>
    public DbSet<DiscountProgram> DiscountPrograms { get; set; }

    /// <summary>Materialized discounts assigned to cards.</summary>
    public DbSet<PassengerDiscount> PassengerDiscounts { get; set; }

    /// <summary>PII-minimized authentication event audit log.</summary>
    public DbSet<AuthAuditLog> AuthAuditLogs { get; set; }

    /// <summary>Audit trail of passenger edits.</summary>
    public DbSet<PassengerEditHistory> PassengerEditHistories { get; set; }

    /// <summary>Audit trail of passenger deletions.</summary>
    public DbSet<PassengerDeleteHistory> PassengerDeleteHistories { get; set; }

    /// <summary>Audit trail of driver edits.</summary>
    public DbSet<DriverEditHistory> DriverEditHistories { get; set; }

    /// <summary>Audit trail of driver deletions.</summary>
    public DbSet<DriverDeleteHistory> DriverDeleteHistories { get; set; }

    /// <summary>Audit trail of terminal edits.</summary>
    public DbSet<TerminalEditHistory> TerminalEditHistories { get; set; }

    /// <summary>Audit trail of terminal deletions.</summary>
    public DbSet<TerminalDeleteHistory> TerminalDeleteHistories { get; set; }

    /// <summary>Audit trail of fare matrix edits.</summary>
    public DbSet<FareMatrixEditHistory> FareMatrixEditHistories { get; set; }

    /// <summary>Audit trail of fare matrix deletions.</summary>
    public DbSet<FareMatrixDeleteHistory> FareMatrixDeleteHistories { get; set; }

    /// <summary>
    /// Configures the EF Core model: table mappings, indexes, constraints,
    /// soft-delete query filters, and relationships. See the section banners
    /// below for each concern area.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Table mappings
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<Card>().ToTable("cards");
        modelBuilder.Entity<Terminal>().ToTable("terminals");
        modelBuilder.Entity<Wallet>().ToTable("wallets");
        modelBuilder.Entity<Transaction>().ToTable("transactions");
        modelBuilder.Entity<FareRule>().ToTable("fare_rules");
        modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens");
        modelBuilder.Entity<QRCode>().ToTable("qr_codes");
        modelBuilder.Entity<Trip>().ToTable("trips");
        modelBuilder.Entity<TripPlan>().ToTable("trip_plans");
        modelBuilder.Entity<DiscountType>().ToTable("discount_types");
        modelBuilder.Entity<DiscountApplication>().ToTable("discount_applications");
        modelBuilder.Entity<DiscountProgram>().ToTable("discount_programs");
        modelBuilder.Entity<PassengerDiscount>().ToTable("passenger_discounts");
        modelBuilder.Entity<AuthAuditLog>().ToTable("auth_audit_logs");
        modelBuilder.Entity<PassengerEditHistory>().ToTable("passenger_edit_history");
        modelBuilder.Entity<PassengerDeleteHistory>().ToTable("passenger_delete_history");
        modelBuilder.Entity<DriverEditHistory>().ToTable("driver_edit_history");
        modelBuilder.Entity<DriverDeleteHistory>().ToTable("driver_delete_history");
        modelBuilder.Entity<TerminalEditHistory>().ToTable("terminal_edit_history");
        modelBuilder.Entity<TerminalDeleteHistory>().ToTable("terminal_delete_history");
        modelBuilder.Entity<FareMatrixEditHistory>().ToTable("fare_matrix_edit_history");
        modelBuilder.Entity<FareMatrixDeleteHistory>().ToTable("fare_matrix_delete_history");

        modelBuilder.Entity<Wallet>().Property(w => w.RowVersion).IsRowVersion();
        modelBuilder.Entity<Trip>().Property(t => t.RowVersion).IsRowVersion();
        modelBuilder.Entity<Transaction>().Property(t => t.RowVersion).IsRowVersion();

        // ── Database integrity CHECK constraints ────────────────────────
        modelBuilder.Entity<Wallet>()
            .HasCheckConstraint("CK_wallets_balance_non_negative", "\"balance\" >= 0");

        modelBuilder.Entity<FareRule>()
            .HasCheckConstraint("CK_fare_rules_fare_amount_non_negative", "\"fare_amount\" >= 0");

        modelBuilder.Entity<Transaction>()
            .HasCheckConstraint("CK_transactions_amount_non_negative", "\"amount\" >= 0");

        modelBuilder.Entity<Transaction>()
            .HasCheckConstraint("CK_transactions_final_fare_non_negative", "\"final_fare\" >= 0");

        modelBuilder.Entity<Transaction>()
            .HasCheckConstraint("CK_transactions_regular_fare_non_negative", "\"regular_fare\" >= 0");

        modelBuilder.Entity<Transaction>()
            .HasCheckConstraint("CK_transactions_discount_amount_non_negative", "\"discount_amount\" IS NULL OR \"discount_amount\" >= 0");

        modelBuilder.Entity<Transaction>()
            .HasCheckConstraint("CK_transactions_discount_percentage_range", "\"discount_percentage\" IS NULL OR (\"discount_percentage\" >= 0 AND \"discount_percentage\" <= 100)");

        modelBuilder.Entity<DiscountType>()
            .HasCheckConstraint("CK_discount_types_discount_percentage_range", "\"discount_percentage\" >= 0 AND \"discount_percentage\" <= 100");

        modelBuilder.Entity<DiscountProgram>()
            .HasCheckConstraint("CK_discount_programs_discount_percentage_range", "\"discount_percentage\" >= 0 AND \"discount_percentage\" <= 100");

        // ── Soft delete query filters ───────────────────────────────────
        modelBuilder.Entity<Card>().HasQueryFilter(c => c.DeletedAt == null);
        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
        modelBuilder.Entity<Terminal>().HasQueryFilter(t => t.DeletedAt == null);
        modelBuilder.Entity<Wallet>().HasQueryFilter(w => w.DeletedAt == null);
        modelBuilder.Entity<Transaction>().HasQueryFilter(t => t.DeletedAt == null);
        modelBuilder.Entity<FareRule>().HasQueryFilter(fr => fr.DeletedAt == null);
        modelBuilder.Entity<DiscountType>().HasQueryFilter(dt => dt.DeletedAt == null);
        modelBuilder.Entity<DiscountApplication>().HasQueryFilter(da => da.DeletedAt == null);
        modelBuilder.Entity<DiscountProgram>().HasQueryFilter(dp => dp.DeletedAt == null);
        modelBuilder.Entity<PassengerDiscount>().HasQueryFilter(pd => pd.DeletedAt == null);

        // ── Discount Type relationships ──────────────────────────────────
        modelBuilder.Entity<DiscountType>()
            .HasMany(dt => dt.DiscountApplications)
            .WithOne(da => da.DiscountType)
            .HasForeignKey(da => da.DiscountTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Discount Program relationships ────────────────────────────────
        modelBuilder.Entity<DiscountProgram>()
            .HasMany(dp => dp.DiscountApplications)
            .WithOne(da => da.DiscountProgram)
            .HasForeignKey(da => da.DiscountProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Passenger Discount relationships ─────────────────────────────
        modelBuilder.Entity<PassengerDiscount>()
            .HasOne(pd => pd.Card)
            .WithMany()
            .HasForeignKey(pd => pd.CardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PassengerDiscount>()
            .HasOne(pd => pd.DiscountProgram)
            .WithMany()
            .HasForeignKey(pd => pd.DiscountProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PassengerDiscount>()
            .HasOne(pd => pd.ApprovedByUser)
            .WithMany()
            .HasForeignKey(pd => pd.ApprovedBy)
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

        // ── User / Role relationships ───────────────────────────────────
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
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

        // Username lookup is a hot auth and account lookup path; make it unique and indexed.
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Business identifier uniqueness on master data names.
        modelBuilder.Entity<DiscountType>()
            .HasIndex(dt => dt.Name)
            .IsUnique()
            .HasFilter("name IS NOT NULL AND name <> ''");

        modelBuilder.Entity<DiscountProgram>()
            .HasIndex(dp => dp.Name)
            .IsUnique()
            .HasFilter("name IS NOT NULL AND name <> ''");

        modelBuilder.Entity<Terminal>()
            .HasIndex(t => t.TerminalName)
            .IsUnique()
            .HasFilter("terminal_name IS NOT NULL AND terminal_name <> ''");

        // ── FareRule constraints & relationships ──────────────────────────
        // Unique constraint to prevent duplicate fare rules for the same route
        modelBuilder.Entity<FareRule>()
            .HasIndex(fr => new
            {
                fr.OriginTerminalId,
                fr.DestinationTerminalId
            })
            .IsUnique()
            .HasFilter("is_active = true AND deleted_at IS NULL");

        // FareRule has two FKs to Terminal (origin and destination)
        // These must be configured explicitly so EF Core can distinguish them.
        modelBuilder.Entity<FareRule>()
            .HasOne(fr => fr.OriginTerminal)
            .WithMany()
            .HasForeignKey(fr => fr.OriginTerminalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FareRule>()
            .HasOne(fr => fr.DestinationTerminal)
            .WithMany()
            .HasForeignKey(fr => fr.DestinationTerminalId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Transaction relationships ─────────────────────────────────────
        // The Transaction has two FKs to Terminal:
        //   - TerminalId (destination, via Terminal nav property)
        //   - OriginTerminalId (origin, via OriginTerminal nav property)
        // These must be configured explicitly so EF Core can distinguish them.
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Card)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.DiscountType)
            .WithMany()
            .HasForeignKey(t => t.DiscountTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Terminal)
            .WithMany()
            .HasForeignKey(t => t.TerminalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.OriginTerminal)
            .WithMany() // No inverse navigation on Terminal for origin transactions
            .HasForeignKey(t => t.OriginTerminalId)
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
        // Trip has two FKs to Terminal (origin and final destination)
        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Driver)
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.OriginTerminal)
            .WithMany()
            .HasForeignKey(t => t.OriginTerminalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.FinalDestinationTerminal)
            .WithMany()
            .HasForeignKey(t => t.FinalDestinationTerminalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.CurrentBoardingOriginTerminal)
            .WithMany()
            .HasForeignKey(t => t.CurrentBoardingOriginTerminalId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Trip indexes ──────────────────────────────────────────────────
        // Index on driver_id for fast trip lookups by driver
        modelBuilder.Entity<Trip>()
            .HasIndex(t => t.DriverId);

        // Index on current boarding origin terminal for fast lookups by boarding terminal
        modelBuilder.Entity<Trip>()
            .HasIndex(t => t.CurrentBoardingOriginTerminalId);

        // Composite lookup for the hot active-trip check: driver_id + trip_status
        modelBuilder.Entity<Trip>()
            .HasIndex(t => new { t.DriverId, t.TripStatus });

        modelBuilder.Entity<Trip>()
            .HasIndex(t => t.TripStatus);

        modelBuilder.Entity<Trip>()
            .HasIndex(t => t.StartedAt);

        modelBuilder.Entity<Trip>()
            .HasIndex(t => t.EndedAt);

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

        // Hot pagination path in the transaction controller: card_id + created_at DESC
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => new { t.CardId, t.CreatedAt })
            .IsDescending(false, true);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.TransactionType);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.Status);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.DiscountTypeId);

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

        // Unique index on PaymentRequestKey for COMPLETED transactions only.
        // This is the hard idempotency guarantee — a duplicate scan/charge event
        // for the same trip/card/origin/destination is rejected at the DB level.
        // TransactionStatus enum values: PENDING=0, COMPLETED=1, FAILED=2, CANCELLED=3
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.PaymentRequestKey)
            .IsUnique()
            .HasFilter("payment_request_key IS NOT NULL AND status = 1");

        // Receipts and legacy customer-facing references must remain unique for the non-empty business value.
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.ReferenceNumber)
            .IsUnique()
            .HasFilter("reference_number IS NOT NULL AND reference_number <> ''");

        // ── DiscountApplication indexes ───────────────────────────────────
        modelBuilder.Entity<DiscountApplication>()
            .HasIndex(da => da.CardId);

        modelBuilder.Entity<DiscountApplication>()
            .HasIndex(da => da.DiscountTypeId);

        modelBuilder.Entity<DiscountApplication>()
            .HasIndex(da => da.Status);

        modelBuilder.Entity<DiscountApplication>()
            .HasIndex(da => da.DiscountProgramId);

        // ── PassengerDiscount indexes ─────────────────────────────────────
        modelBuilder.Entity<PassengerDiscount>()
            .HasIndex(pd => pd.CardId);

        modelBuilder.Entity<PassengerDiscount>()
            .HasIndex(pd => pd.DiscountProgramId);

        modelBuilder.Entity<PassengerDiscount>()
            .HasIndex(pd => pd.Status);

        // Filtered unique index: only one ACTIVE passenger discount per card.
        // PassengerDiscountStatus enum values: Active=0, Expired=1, Revoked=2
        modelBuilder.Entity<PassengerDiscount>()
            .HasIndex(pd => pd.CardId)
            .IsUnique()
            .HasFilter("status = 0");

        // ── AuthAuditLog indexes ──────────────────────────────────────────
        modelBuilder.Entity<AuthAuditLog>()
            .HasIndex(al => al.UserId);

        modelBuilder.Entity<AuthAuditLog>()
            .HasIndex(al => al.EventType);

        modelBuilder.Entity<AuthAuditLog>()
            .HasIndex(al => al.CreatedAt);

        // ── RefreshToken indexes ──────────────────────────────────────────
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.UserId, rt.Token });

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.ExpiresAt);

        // ── Wallet constraints ────────────────────────────────────────────
        // One wallet per card (unique constraint)
        modelBuilder.Entity<Wallet>()
            .HasIndex(w => w.CardId)
            .IsUnique();

        // ── History table indexes ─────────────────────────────────────────
        modelBuilder.Entity<PassengerEditHistory>()
            .HasIndex(h => h.OriginalRecordId);
        modelBuilder.Entity<PassengerEditHistory>()
            .HasIndex(h => h.PerformedAt);
        modelBuilder.Entity<PassengerEditHistory>()
            .HasIndex(h => h.Operation);

        modelBuilder.Entity<PassengerDeleteHistory>()
            .HasIndex(h => h.OriginalRecordId);
        modelBuilder.Entity<PassengerDeleteHistory>()
            .HasIndex(h => h.PerformedAt);
        modelBuilder.Entity<PassengerDeleteHistory>()
            .HasIndex(h => h.Operation);

        modelBuilder.Entity<DriverEditHistory>()
            .HasIndex(h => h.OriginalRecordId);
        modelBuilder.Entity<DriverEditHistory>()
            .HasIndex(h => h.PerformedAt);
        modelBuilder.Entity<DriverEditHistory>()
            .HasIndex(h => h.Operation);

        modelBuilder.Entity<DriverDeleteHistory>()
            .HasIndex(h => h.OriginalRecordId);
        modelBuilder.Entity<DriverDeleteHistory>()
            .HasIndex(h => h.PerformedAt);
        modelBuilder.Entity<DriverDeleteHistory>()
            .HasIndex(h => h.Operation);

        modelBuilder.Entity<TerminalEditHistory>()
            .HasIndex(h => h.OriginalRecordId);
        modelBuilder.Entity<TerminalEditHistory>()
            .HasIndex(h => h.PerformedAt);
        modelBuilder.Entity<TerminalEditHistory>()
            .HasIndex(h => h.Operation);

        modelBuilder.Entity<TerminalDeleteHistory>()
            .HasIndex(h => h.OriginalRecordId);
        modelBuilder.Entity<TerminalDeleteHistory>()
            .HasIndex(h => h.PerformedAt);
        modelBuilder.Entity<TerminalDeleteHistory>()
            .HasIndex(h => h.Operation);


        modelBuilder.Entity<FareMatrixEditHistory>()
            .HasIndex(h => h.OriginalRecordId);
        modelBuilder.Entity<FareMatrixEditHistory>()
            .HasIndex(h => h.PerformedAt);
        modelBuilder.Entity<FareMatrixEditHistory>()
            .HasIndex(h => h.Operation);

        modelBuilder.Entity<FareMatrixDeleteHistory>()
            .HasIndex(h => h.OriginalRecordId);
        modelBuilder.Entity<FareMatrixDeleteHistory>()
            .HasIndex(h => h.PerformedAt);
        modelBuilder.Entity<FareMatrixDeleteHistory>()
            .HasIndex(h => h.Operation);
    }
}