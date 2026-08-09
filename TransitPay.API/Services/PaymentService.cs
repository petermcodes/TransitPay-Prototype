using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.DTOs.Payment;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;
using TransitPay.API.Utilities;

namespace TransitPay.API.Services;

/// <summary>
/// Service for processing transit fare payments via payment sessions.
/// The fare is locked in the session at creation time and charged during driver scan.
/// All operations are wrapped in a database transaction for atomicity.
/// Duplicate processing is prevented via row locking and status transitions.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly IQRService _qrService;
    private readonly TransactionReferenceNumberGenerator _trnGenerator;
    private readonly ITripService _tripService;
    private readonly IDiscountService _discountService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        TransitPayDbContext dbContext,
        IQRService qrService,
        TransactionReferenceNumberGenerator trnGenerator,
        ITripService tripService,
        IDiscountService discountService,
        ILogger<PaymentService> logger)
    {
        _dbContext = dbContext;
        _qrService = qrService;
        _trnGenerator = trnGenerator;
        _tripService = tripService;
        _discountService = discountService;
        _logger = logger;
    }

    // Card number masking is now centralized in Utilities/CardFormatter.MaskCardNumber.
    // PaymentService calls CardFormatter directly where masking is needed.

    /// <inheritdoc />
    public async Task<PaymentResponse> ProcessConductorPaymentAsync(
        string qrData, string signature, int driverId, int planId = 0)
    {
        _logger.LogInformation("Processing conductor-initiated payment for driver {DriverId}, plan {PlanId}", driverId, planId);

        try
        {
            // Business Rule: Only ACTIVE trips may accept payments
            var activeTrip = await _tripService.GetActiveTripAsync(driverId);
            if (activeTrip == null || activeTrip.TripStatus != TripStatus.Active)
            {
                _logger.LogWarning("Driver {DriverId} attempted to process payment without an active trip", driverId);
                return new PaymentResponse
                {
                    Success = false,
                    Message = "No active trip found. Please start a trip before processing payments."
                };
            }

            // Business Rule: Verify TripPlan is ACTIVE before processing payment
            if (planId > 0)
            {
                var tripPlan = await _dbContext.TripPlans
                    .FirstOrDefaultAsync(tp => tp.PlanId == planId && tp.Status == "Active");

                if (tripPlan == null)
                {
                    _logger.LogWarning("Trip plan {PlanId} not found or not active", planId);
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Trip plan not found or already used. Please verify the passenger's QR code."
                    };
                }
            }

            // Step 1: Validate the QR code and identify the card
            var cardId = await _qrService.ValidateQRAsync(qrData, signature);

            // Step 2: Process the payment using the passenger's active Trip Plan.
            // The destination is read from the TripPlans table inside ProcessConductorPaymentCoreAsync.
            // No PaymentSession lookup is needed — the Trip Plan is the source of truth.
            return await ProcessConductorPaymentCoreAsync(
                cardId, driverId, activeTrip, null, qrData, signature, planId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Conductor payment failed: {Message}", ex.Message);
            return new PaymentResponse { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing conductor payment for driver {DriverId}", driverId);
            return new PaymentResponse { Success = false, Message = "An error occurred while processing the payment." };
        }
    }

    /// <inheritdoc />
    public async Task<PaymentResponse> ProcessConductorPhysicalCardPaymentAsync(
        string cardNumber, int driverId)
    {
        _logger.LogInformation("Processing conductor-initiated physical card payment for driver {DriverId}", driverId);

        try
        {
            // Business Rule: Only ACTIVE trips may accept payments
            var activeTrip = await _tripService.GetActiveTripAsync(driverId);
            if (activeTrip == null || activeTrip.TripStatus != TripStatus.Active)
            {
                _logger.LogWarning("Driver {DriverId} attempted to process payment without an active trip", driverId);
                return new PaymentResponse
                {
                    Success = false,
                    Message = "No active trip found. Please start a trip before processing payments."
                };
            }

            // Look up the card by its 16-digit number
            var card = await _dbContext.Cards
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
            if (card == null)
            {
                return new PaymentResponse { Success = false, Message = "Card not found." };
            }

            return await ProcessConductorPaymentCoreAsync(
                card.CardId, driverId, activeTrip, cardNumber, null, null);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Physical card payment failed: {Message}", ex.Message);
            return new PaymentResponse { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing physical card payment for driver {DriverId}", driverId);
            return new PaymentResponse { Success = false, Message = "An error occurred while processing the payment." };
        }
    }

    /// <summary>
    /// Shared conductor payment core used by both QR and physical-card flows.
    /// Validates the trip, route, card, wallet, and fare rule, then processes the payment
    /// inside a single atomic database transaction with idempotency protection.
    /// </summary>
    private async Task<PaymentResponse> ProcessConductorPaymentCoreAsync(
        int cardId, int driverId, Trip activeTrip,
        string? cardNumber, string? qrData, string? signature, int planId = 0)
    {
        // Step 2: Read the passenger's active trip plan to get the destination.
        var tripPlan = await _dbContext.TripPlans
            .Include(tp => tp.OriginTerminal)
            .Include(tp => tp.DestinationTerminal)
            .FirstOrDefaultAsync(tp => tp.CardId == cardId && tp.Status == "Active");

        if (tripPlan == null)
        {
            _logger.LogWarning("No active trip plan found for card {CardId}", cardId);
            return new PaymentResponse
            {
                Success = false,
                Message = "Passenger has no active trip plan. Please ask the passenger to plan their trip first."
            };
        }

        if (tripPlan.ExpiresAt.HasValue && DateTime.UtcNow > tripPlan.ExpiresAt.Value)
        {
            _logger.LogWarning("Trip plan {PlanId} has expired for card {CardId}", tripPlan.PlanId, cardId);
            tripPlan.Status = "Cancelled";
            tripPlan.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return new PaymentResponse
            {
                Success = false,
                Message = "Passenger's trip plan has expired. Please ask them to plan their trip again."
            };
        }

        // Step 3: Determine the boarding origin.
        // Prefer the driver's explicit current boarding origin when set;
        // otherwise fall back to the passenger's planned origin from the Trip Plan.
        var persistedBoardingOrigin = activeTrip.CurrentBoardingOriginTerminalId;
        var originTerminalId = persistedBoardingOrigin ?? tripPlan.OriginTerminalId;

        if (originTerminalId <= 0)
        {
            return new PaymentResponse
            {
                Success = false,
                Message = "Current boarding origin is not set for this trip. Please select a boarding origin."
            };
        }

        var destinationTerminalId = tripPlan.DestinationTerminalId;

        // Step 4: Validate terminals and route
        var validation = await ValidateConductorRouteAsync(activeTrip, originTerminalId, destinationTerminalId);
        if (validation != null)
        {
            return validation;
        }

        // Load the origin/destination terminal names for snapshots
        var originTerminal = await _dbContext.Terminals.FirstOrDefaultAsync(t => t.TerminalId == originTerminalId);
        var destTerminal = await _dbContext.Terminals.FirstOrDefaultAsync(t => t.TerminalId == destinationTerminalId);
        var originTerminalName = originTerminal?.TerminalName ?? "Unknown";
        var destinationTerminalName = destTerminal?.TerminalName ?? "Unknown";

        // Step 5: Retrieve the card with wallet and user
        var card = await _dbContext.Cards
            .Include(c => c.Wallet)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CardId == cardId);

        if (card == null)
        {
            return new PaymentResponse { Success = false, Message = "Card not found." };
        }

        if (card.Status != CardStatus.ACTIVE)
        {
            return new PaymentResponse { Success = false, Message = "Card is not active." };
        }

        var wallet = card.Wallet;
        if (wallet == null)
        {
            return new PaymentResponse { Success = false, Message = "Wallet not found." };
        }

        if (wallet.Status != CardStatus.ACTIVE)
        {
            return new PaymentResponse { Success = false, Message = "Wallet is not active." };
        }

        // Build a deterministic idempotency key for this scan/charge event.
        var paymentRequestKey = BuildPaymentRequestKey(
            card.CardId, driverId, activeTrip.TripId, originTerminalId, destinationTerminalId, qrData, signature, cardNumber, tripPlan.PlanId);

        // Step 5: Check for an existing COMPLETED transaction with the same idempotency key.
        // If found, return the existing receipt idempotently — no second charge.
        var existing = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.PaymentRequestKey == paymentRequestKey &&
                                      t.Status == TransactionStatus.COMPLETED);
        if (existing != null)
        {
            _logger.LogInformation("Duplicate conductor payment detected for key {Key}. Returning existing receipt {Trn}.",
                paymentRequestKey, existing.TransactionReferenceNumber);
            return new PaymentResponse
            {
                Success = true,
                Message = "Payment already completed.",
                Data = new PaymentData
                {
                    CardId = card.CardId,
                    PassengerName = card.User != null
                        ? $"{card.User.FirstName} {card.User.LastName}".Trim()
                        : null,
                    MaskedCardNumber = CardFormatter.MaskCardNumber(card.CardNumber),
                    OriginTerminalId = existing.OriginTerminalId ?? originTerminalId,
                    DestinationTerminalId = existing.TerminalId ?? destinationTerminalId,
                    OriginTerminalName = existing.OriginTerminalName,
                    DestinationTerminalName = existing.DestinationTerminalName,
                    LockedFare = existing.FinalFare,
                    RegularFare = existing.RegularFare,
                    DiscountPercentage = existing.DiscountPercentage,
                    DiscountAmount = existing.DiscountAmount,
                    FinalFare = existing.FinalFare,
                    RemainingBalance = existing.RemainingBalance,
                    TransactionReferenceNumber = existing.TransactionReferenceNumber,
                    PaymentTimestamp = existing.CreatedAt,
                    DriverId = existing.DriverId,
                    TransactionName = existing.TransactionName
                }
            };
        }

        // Step 6: Look up the fare rule for this origin → destination and passenger type.
        // Backend ALWAYS determines the fare from active fare rules — never the frontend.
        var fareRule = await _dbContext.FareRules
            .Where(fr =>
                fr.OriginTerminalId == originTerminalId &&
                fr.DestinationTerminalId == destinationTerminalId &&
                fr.VehicleType == VehicleType.BUS &&
                fr.PassengerType == card.PassengerType &&
                fr.IsActive &&
                fr.DeletedAt == null)
            .OrderByDescending(fr => fr.EffectiveDate)
            .FirstOrDefaultAsync();

        if (fareRule == null)
        {
            _logger.LogWarning("No active fare rule found for route {OriginTerminalId} -> {DestinationTerminalId} with passenger type {PassengerType}",
                originTerminalId, destinationTerminalId, card.PassengerType);

            return new PaymentResponse
            {
                Success = false,
                Message = $"No active fare found for route {originTerminalName} → {destinationTerminalName}."
            };
        }

        var fareAmount = fareRule.FareAmount;
        if (fareAmount <= 0)
        {
            return new PaymentResponse { Success = false, Message = "Invalid fare amount configured for this route." };
        }

        // Step 7: Process the payment inside a database transaction.
        // Pass the loaded tripPlan.PlanId so the plan is always marked as "Used" after payment.
        return await ProcessConductorPaymentTransactionAsync(
            card, wallet, fareAmount, fareRule.FareId, driverId, activeTrip,
            originTerminalId, destinationTerminalId, originTerminalName, destinationTerminalName,
            paymentRequestKey, tripPlan.PlanId);
    }

    /// <summary>
    /// Validates that the current boarding origin and destination belong to the active trip's route.
    /// Returns a failure response or null when the route is valid.
    /// </summary>
    private async Task<PaymentResponse?> ValidateConductorRouteAsync(
        Trip trip, int originTerminalId, int destinationTerminalId)
    {
        // Validate terminal existence and activity
        var originTerminal = await _dbContext.Terminals
            .FirstOrDefaultAsync(t => t.TerminalId == originTerminalId && t.IsActive);
        if (originTerminal == null)
        {
            return new PaymentResponse { Success = false, Message = "Invalid or inactive origin terminal." };
        }

        var destinationTerminal = await _dbContext.Terminals
            .FirstOrDefaultAsync(t => t.TerminalId == destinationTerminalId && t.IsActive);
        if (destinationTerminal == null)
        {
            return new PaymentResponse { Success = false, Message = "Invalid destination terminal." };
        }

        // Origin and destination cannot be identical
        if (originTerminalId == destinationTerminalId)
        {
            return new PaymentResponse { Success = false, Message = "Origin and destination terminals must be different." };
        }

        // Business Rule: The boarding origin must belong to the trip's route.
        // When the driver's trip has no route defined (started without origin/destination),
        // the passenger's Trip Plan is the source of truth — skip the route check.
        // Otherwise, the origin must be the trip's origin terminal or have a fare rule
        // from that boarding origin towards the trip's final destination.
        var tripHasRoute = trip.OriginTerminalId.HasValue && trip.FinalDestinationTerminalId.HasValue;
        var originOnRoute = !tripHasRoute ||
                            originTerminalId == trip.OriginTerminalId ||
                            await _dbContext.FareRules.AnyAsync(fr =>
                                fr.OriginTerminalId == originTerminalId &&
                                fr.DestinationTerminalId == trip.FinalDestinationTerminalId &&
                                fr.VehicleType == VehicleType.BUS &&
                                fr.IsActive &&
                                fr.DeletedAt == null);
        if (!originOnRoute)
        {
            return new PaymentResponse
            {
                Success = false,
                Message = $"Boarding origin '{originTerminal.TerminalName}' is not on the trip route."
            };
        }

        // Business Rule: The passenger destination must belong to the same route.
        // Valid when the destination is on the route toward the final destination
        // (i.e., a fare rule exists from the boarding origin to the destination).
        var destinationOnRoute = await _dbContext.FareRules.AnyAsync(fr =>
            fr.OriginTerminalId == originTerminalId &&
            fr.DestinationTerminalId == destinationTerminalId &&
            fr.VehicleType == VehicleType.BUS &&
            fr.IsActive &&
            fr.DeletedAt == null);
        if (!destinationOnRoute)
        {
            return new PaymentResponse
            {
                Success = false,
                Message = $"Passenger destination '{destinationTerminal.TerminalName}' is not on the trip route from '{originTerminal.TerminalName}'."
            };
        }

        return null;
    }

    /// <summary>
    /// Builds a deterministic idempotency key for a single conductor scan/charge event.
    /// The same card, trip, driver, origin, destination, and scan payload produce the same key.
    /// </summary>
    private static string BuildPaymentRequestKey(
        int cardId, int driverId, int tripId,
        int originStationId, int destinationStationId,
        string? qrData, string? signature, string? cardNumber, int tripPlanId)
    {
        var scanIdentifier = !string.IsNullOrEmpty(qrData)
            ? $"{qrData}|{signature}"
            : $"card:{cardNumber}";

        var raw = $"{tripId}|{driverId}|{cardId}|{originStationId}|{destinationStationId}|{tripPlanId}|{scanIdentifier}";

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes)[..64];
    }

    /// <summary>
    /// Processes a conductor-initiated payment inside a single atomic database transaction.
    /// Locks the wallet row to serialize concurrent charges, applies the idempotency key,
    /// and stores a full payment snapshot.
    /// </summary>
    private async Task<PaymentResponse> ProcessConductorPaymentTransactionAsync(
        Card card, Wallet wallet, decimal fareAmount, int fareId, int driverId, Trip trip,
        int originTerminalId, int destinationTerminalId, string originTerminalName, string destinationTerminalName,
        string paymentRequestKey, int planId = 0)
    {
        // Begin database transaction for atomicity
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Lock the wallet row to serialize concurrent payments for the same card.
            // SELECT ... FOR UPDATE ensures the balance check + deduction is atomic.
            // For providers that don't support raw SQL (e.g., in-memory tests), fall back to a regular query.
            var providerName = _dbContext.Database.ProviderName;
            var supportsRowLock = providerName != null &&
                                  providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);

            Wallet? lockedWallet;
            if (supportsRowLock)
            {
                lockedWallet = await _dbContext.Wallets
                    .FromSqlRaw("SELECT * FROM wallets WHERE wallet_id = {0} FOR UPDATE", wallet.WalletId)
                    .FirstOrDefaultAsync();
            }
            else
            {
                lockedWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.WalletId == wallet.WalletId);
            }

            if (lockedWallet == null)
            {
                return new PaymentResponse { Success = false, Message = "Wallet not found." };
            }

            // Re-check balance after acquiring the lock
            if (lockedWallet.Balance < fareAmount)
            {
                _logger.LogWarning("Insufficient balance. CardId: {CardId}, Balance: {Balance}, Required: {FareAmount}",
                    card.CardId, lockedWallet.Balance, fareAmount);
                return new PaymentResponse { Success = false, Message = "Insufficient balance." };
            }

            // Re-check idempotency inside the transaction (after lock acquisition)
            var duplicate = await _dbContext.Transactions
                .AnyAsync(t => t.PaymentRequestKey == paymentRequestKey &&
                               t.Status == TransactionStatus.COMPLETED);
            if (duplicate)
            {
                _logger.LogWarning("Concurrent duplicate payment detected for key {Key}", paymentRequestKey);
                return new PaymentResponse { Success = false, Message = "This payment has already been processed." };
            }

            // Retrieve active discount for the card (if any) from the PassengerDiscount table.
            // The percentage is snapshotted at approval time — NOT re-read from the program.
            var activeDiscount = await _discountService.GetActiveDiscountForCardAsync(card.CardId);

            // Calculate discount
            decimal regularFare = fareAmount;
            decimal discountPercentage = 0;
            decimal discountAmount = 0;
            decimal finalFare = fareAmount;
            int? discountTypeId = null;

            if (activeDiscount != null)
            {
                discountPercentage = activeDiscount.DiscountPercentage;
                discountAmount = regularFare * (discountPercentage / 100);
                finalFare = regularFare - discountAmount;

                // Populate discountTypeId for financial reporting and reconciliation
                if (activeDiscount.DiscountProgramId.HasValue)
                {
                    discountTypeId = await _dbContext.DiscountPrograms
                        .Where(dp => dp.DiscountProgramId == activeDiscount.DiscountProgramId.Value)
                        .Select(dp => dp.DiscountTypeId)
                        .FirstOrDefaultAsync();
                }

                _logger.LogInformation("Discount applied. CardId: {CardId}, RegularFare: {RegularFare}, DiscountPercentage: {DiscountPercentage}%, DiscountAmount: {DiscountAmount}, FinalFare: {FinalFare}, DiscountTypeId: {DiscountTypeId}",
                    card.CardId, regularFare, discountPercentage, discountAmount, finalFare, discountTypeId);
            }

            // Deduct the final fare from the wallet
            lockedWallet.Balance -= finalFare;
            lockedWallet.UpdatedAt = DateTime.UtcNow;

            // Generate the Transaction Reference Number (TRN) inside this transaction
            var trn = await _trnGenerator.GenerateNextAsync();

            // Create the transaction record with the full payment snapshot
            var transactionRecord = new Models.Transaction
            {
                CardId = card.CardId,
                DriverId = driverId,
                TripId = trip.TripId,
                OriginTerminalId = originTerminalId,
                OriginTerminalName = originTerminalName,
                TerminalId = destinationTerminalId,
                DestinationTerminalName = destinationTerminalName,
                FareId = fareId,
                RegularFare = regularFare,
                DiscountPercentage = discountPercentage > 0 ? discountPercentage : null,
                DiscountAmount = discountAmount > 0 ? discountAmount : null,
                FinalFare = finalFare,
                RemainingBalance = lockedWallet.Balance,
                DiscountTypeId = discountTypeId,
                Amount = finalFare,
                PaymentRequestKey = paymentRequestKey,
                TransactionType = TransactionType.PAYMENT,
                TransactionName = $"Fare payment: {originTerminalName} → {destinationTerminalName}",
                Status = TransactionStatus.COMPLETED,
                TransactionReferenceNumber = trn,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Transactions.Add(transactionRecord);

            // Update trip statistics
            trip.PassengerCount += 1;
            trip.TotalRevenue += finalFare;
            trip.UpdatedAt = DateTime.UtcNow;

            // Update TripPlan status to "Used" if planId is provided
            if (planId > 0)
            {
                var tripPlan = await _dbContext.TripPlans
                    .FirstOrDefaultAsync(tp => tp.PlanId == planId);
                
                if (tripPlan != null && tripPlan.Status == "Active")
                {
                    tripPlan.Status = "Used";
                    tripPlan.UsedAt = DateTime.UtcNow;
                    tripPlan.UpdatedAt = DateTime.UtcNow;
                    
                    _logger.LogInformation("Trip plan {PlanId} marked as Used", planId);
                }
            }

            await _dbContext.SaveChangesAsync();

            // Commit the transaction
            await transaction.CommitAsync();

            var paymentTimestamp = DateTime.UtcNow;

            _logger.LogInformation("Conductor payment successful. CardId: {CardId}, RegularFare: {RegularFare}, FinalFare: {FinalFare}, NewBalance: {Balance}, TRN: {Trn}",
                card.CardId, regularFare, finalFare, lockedWallet.Balance, trn);

            return new PaymentResponse
            {
                Success = true,
                Message = "Payment completed successfully.",
                Data = new PaymentData
                {
                    CardId = card.CardId,
                    PassengerName = card.User != null
                        ? $"{card.User.FirstName} {card.User.LastName}".Trim()
                        : null,
                    MaskedCardNumber = CardFormatter.MaskCardNumber(card.CardNumber),
                    OriginTerminalId = originTerminalId,
                    OriginTerminalName = originTerminalName,
                    DestinationTerminalId = destinationTerminalId,
                    DestinationTerminalName = destinationTerminalName,
                    LockedFare = regularFare,
                    RegularFare = regularFare,
                    DiscountPercentage = discountPercentage > 0 ? discountPercentage : null,
                    DiscountAmount = discountAmount > 0 ? discountAmount : null,
                    FinalFare = finalFare,
                    RemainingBalance = lockedWallet.Balance,
                    TransactionReferenceNumber = trn,
                    PaymentTimestamp = paymentTimestamp,
                    DriverId = driverId,
                    TransactionName = transactionRecord.TransactionName
                }
            };
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("payment_request_key", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Concurrent duplicate submission hit the unique filtered index — reject idempotently.
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Unique idempotency key violation for payment on card {CardId}", card.CardId);
            return new PaymentResponse { Success = false, Message = "This payment has already been processed." };
        }
        catch (Exception ex)
        {
            // Roll back the transaction on any error
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing conductor payment for driver {DriverId}", driverId);
            return new PaymentResponse { Success = false, Message = "An error occurred while processing the payment." };
        }
    }
}