using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.DTOs.Payment;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

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

    /// <inheritdoc />
    public async Task<PaymentResponse> ProcessQRPaymentAsync(string qrData, string signature, int driverId)
    {
        _logger.LogInformation("Processing QR payment for driver {DriverId}", driverId);

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

            // Step 1: Validate the QR code and identify the card
            var cardId = await _qrService.ValidateQRAsync(qrData, signature);

            // Step 2: Retrieve the active PENDING payment session for the card.
            // If a COMPLETED session exists (e.g., a second scan after payment), reject it.
            var session = await _dbContext.PaymentSessions
                .Include(ps => ps.OriginStation)
                .Include(ps => ps.DestinationStation)
                .Include(ps => ps.Card)
                    .ThenInclude(c => c!.User)
                .Include(ps => ps.Card)
                    .ThenInclude(c => c!.Wallet)
                .FirstOrDefaultAsync(ps => ps.CardId == cardId &&
                    ps.Status == PaymentSessionStatus.PENDING);

            if (session == null)
            {
                _logger.LogWarning("No active payment session found for card {CardId}", cardId);

                // Check if the card has a COMPLETED session — this is a duplicate scan attempt
                var hasCompleted = await _dbContext.PaymentSessions
                    .AnyAsync(ps => ps.CardId == cardId && ps.Status == PaymentSessionStatus.COMPLETED);

                if (hasCompleted)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Payment has already been completed."
                    };
                }

                return new PaymentResponse
                {
                    Success = false,
                    Message = "No active payment session found. Please select your route first."
                };
            }

            // Step 3: Check expiration
            if (DateTime.UtcNow > session.ExpiresAt)
            {
                _logger.LogWarning("Payment session {PaymentSessionId} has expired for card {CardId}", session.PaymentSessionId, cardId);
                session.Status = PaymentSessionStatus.EXPIRED;
                session.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Your selected route has expired. Please select your route again."
                };
            }

            // Step 4: Process the payment using the session
            return await ProcessSessionPaymentAsync(session, driverId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("QR payment failed: {Message}", ex.Message);
            return new PaymentResponse { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing QR payment for driver {DriverId}", driverId);
            return new PaymentResponse { Success = false, Message = "An error occurred while processing the QR payment." };
        }
    }

    /// <summary>
    /// Processes a payment for a given session inside a single atomic database transaction.
    /// Uses row locking to prevent concurrent processing and duplicate deductions.
    /// </summary>
    private async Task<PaymentResponse> ProcessSessionPaymentAsync(PaymentSession session, int driverId)
    {
        // Begin database transaction for atomicity
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Lock the payment session row to prevent concurrent processing.
            // SELECT ... FOR UPDATE ensures only one request can process this session at a time.
            // For providers that don't support raw SQL (e.g., in-memory tests), fall back to a regular query.
            // The status transition (PENDING → SCANNING → PROCESSING) still prevents duplicates.
            PaymentSession? lockedSession;
            var providerName = _dbContext.Database.ProviderName;
            var supportsRowLock = providerName != null &&
                                  providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);

            if (supportsRowLock)
            {
                lockedSession = await _dbContext.PaymentSessions
                    .FromSqlRaw(
                        "SELECT * FROM payment_sessions WHERE payment_session_id = {0} FOR UPDATE",
                        session.PaymentSessionId)
                    .Include(ps => ps.OriginStation)
                    .Include(ps => ps.DestinationStation)
                    .Include(ps => ps.Card)
                        .ThenInclude(c => c!.User)
                    .Include(ps => ps.Card)
                        .ThenInclude(c => c!.Wallet)
                    .FirstOrDefaultAsync();
            }
            else
            {
                lockedSession = await _dbContext.PaymentSessions
                    .Include(ps => ps.OriginStation)
                    .Include(ps => ps.DestinationStation)
                    .Include(ps => ps.Card)
                        .ThenInclude(c => c!.User)
                    .Include(ps => ps.Card)
                        .ThenInclude(c => c!.Wallet)
                    .FirstOrDefaultAsync(ps => ps.PaymentSessionId == session.PaymentSessionId);
            }

            if (lockedSession == null)
            {
                return new PaymentResponse { Success = false, Message = "Payment session not found." };
            }

            // Re-check status after acquiring the lock
            if (lockedSession.Status == PaymentSessionStatus.SCANNING ||
                lockedSession.Status == PaymentSessionStatus.PROCESSING)
            {
                _logger.LogWarning("Payment session {PaymentSessionId} is already being processed", lockedSession.PaymentSessionId);
                return new PaymentResponse { Success = false, Message = "Payment is currently being processed." };
            }

            if (lockedSession.Status == PaymentSessionStatus.COMPLETED)
            {
                _logger.LogWarning("Payment session {PaymentSessionId} has already been completed", lockedSession.PaymentSessionId);
                return new PaymentResponse { Success = false, Message = "Payment has already been completed." };
            }

            if (lockedSession.Status == PaymentSessionStatus.EXPIRED)
            {
                return new PaymentResponse { Success = false, Message = "Your selected route has expired. Please select your route again." };
            }

            if (lockedSession.Status == PaymentSessionStatus.FAILED ||
                lockedSession.Status == PaymentSessionStatus.CANCELLED)
            {
                return new PaymentResponse { Success = false, Message = "Payment session is no longer valid." };
            }

            // Re-check expiration after lock
            if (DateTime.UtcNow > lockedSession.ExpiresAt)
            {
                lockedSession.Status = PaymentSessionStatus.EXPIRED;
                lockedSession.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return new PaymentResponse { Success = false, Message = "Your selected route has expired. Please select your route again." };
            }

            // Transition: PENDING → SCANNING (QR validated, session located)
            lockedSession.Status = PaymentSessionStatus.SCANNING;
            lockedSession.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            // Validate card
            var card = lockedSession.Card;
            if (card == null)
            {
                lockedSession.Status = PaymentSessionStatus.FAILED;
                lockedSession.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return new PaymentResponse { Success = false, Message = "Card not found." };
            }

            if (card.Status != CardStatus.ACTIVE)
            {
                lockedSession.Status = PaymentSessionStatus.FAILED;
                lockedSession.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return new PaymentResponse { Success = false, Message = "Card is not active." };
            }

            // Validate wallet
            var wallet = card.Wallet;
            if (wallet == null)
            {
                lockedSession.Status = PaymentSessionStatus.FAILED;
                lockedSession.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return new PaymentResponse { Success = false, Message = "Wallet not found." };
            }

            if (wallet.Status != CardStatus.ACTIVE)
            {
                lockedSession.Status = PaymentSessionStatus.FAILED;
                lockedSession.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return new PaymentResponse { Success = false, Message = "Wallet is not active." };
            }

            // Validate route
            if (lockedSession.OriginStation == null || lockedSession.DestinationStation == null)
            {
                lockedSession.Status = PaymentSessionStatus.FAILED;
                lockedSession.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return new PaymentResponse { Success = false, Message = "Invalid route for this payment session." };
            }

            // Charge the LOCKED fare from the session — never recalculate
            var fareAmount = lockedSession.Fare;
            if (fareAmount <= 0)
            {
                lockedSession.Status = PaymentSessionStatus.FAILED;
                lockedSession.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return new PaymentResponse { Success = false, Message = "Invalid fare amount for this payment session." };
            }

            // Validate sufficient balance
            if (wallet.Balance < fareAmount)
            {
                _logger.LogWarning("Insufficient balance. CardId: {CardId}, Balance: {Balance}, Required: {FareAmount}",
                    card.CardId, wallet.Balance, fareAmount);
                lockedSession.Status = PaymentSessionStatus.FAILED;
                lockedSession.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                return new PaymentResponse { Success = false, Message = "Insufficient balance." };
            }

            // Transition: SCANNING → PROCESSING (wallet validation, deduction, transaction creation)
            lockedSession.Status = PaymentSessionStatus.PROCESSING;
            lockedSession.UpdatedAt = DateTime.UtcNow;

            // Deduct the locked fare from the wallet
            wallet.Balance -= fareAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            // Generate the Transaction Reference Number (TRN) inside this transaction
            var trn = await _trnGenerator.GenerateNextAsync();

            // Create the transaction record
            var transactionRecord = new Models.Transaction
            {
                CardId = card.CardId,
                PaymentSessionId = lockedSession.PaymentSessionId,
                DriverId = driverId,
                OriginStationId = lockedSession.OriginStationId,
                StationId = lockedSession.DestinationStationId,
                Amount = fareAmount,
                TransactionType = TransactionType.PAYMENT,
                TransactionName = $"Fare payment: {lockedSession.OriginStation.StationName} → {lockedSession.DestinationStation.StationName}",
                Status = TransactionStatus.COMPLETED,
                TransactionReferenceNumber = trn,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Transactions.Add(transactionRecord);

            // Mark the session as COMPLETED
            lockedSession.Status = PaymentSessionStatus.COMPLETED;
            lockedSession.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            // Commit the transaction
            await transaction.CommitAsync();

            var paymentTimestamp = DateTime.UtcNow;

            _logger.LogInformation("Payment successful. CardId: {CardId}, Amount: {Amount}, NewBalance: {Balance}, TRN: {Trn}",
                card.CardId, fareAmount, wallet.Balance, trn);

            return new PaymentResponse
            {
                Success = true,
                Message = "Payment completed successfully.",
                Data = new PaymentData
                {
                    PaymentSessionId = lockedSession.PaymentSessionId,
                    CardId = card.CardId,
                    PassengerName = card.User != null
                        ? $"{card.User.FirstName} {card.User.LastName}".Trim()
                        : null,
                    MaskedCardNumber = MaskCardNumber(card.CardNumber),
                    OriginStationId = lockedSession.OriginStationId,
                    DestinationStationId = lockedSession.DestinationStationId,
                    OriginStationName = lockedSession.OriginStation.StationName,
                    DestinationStationName = lockedSession.DestinationStation.StationName,
                    LockedFare = fareAmount,
                    RemainingBalance = wallet.Balance,
                    TransactionReferenceNumber = trn,
                    PaymentTimestamp = paymentTimestamp,
                    DriverId = driverId,
                    TransactionName = transactionRecord.TransactionName
                }
            };
        }
        catch (Exception ex)
        {
            // Roll back the transaction on any error
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing payment for session {PaymentSessionId}", session.PaymentSessionId);
            return new PaymentResponse { Success = false, Message = "An error occurred while processing the payment." };
        }
    }

    /// <summary>
    /// Masks a card number for display (e.g., "4111111111111111" → "•••• 1111").
    /// </summary>
    private static string MaskCardNumber(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4)
        {
            return cardNumber;
        }

        return $"•••• {cardNumber[^4..]}";
    }

    /// <inheritdoc />
    public async Task<PaymentResponse> ProcessConductorPaymentAsync(string qrData, string signature, int driverId, int destinationStationId)
    {
        _logger.LogInformation("Processing conductor-initiated payment for driver {DriverId} to destination {DestinationStationId}",
            driverId, destinationStationId);

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

            // Validate destination station exists
            var destinationStation = await _dbContext.Stations.FindAsync(destinationStationId);
            if (destinationStation == null)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Invalid destination station."
                };
            }

            // Validate origin and destination are different
            if (activeTrip.OriginStationId == destinationStationId)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Origin and destination stations must be different."
                };
            }

            // Step 1: Validate the QR code and identify the card
            var cardId = await _qrService.ValidateQRAsync(qrData, signature);

            // Step 2: Retrieve the card with wallet
            var card = await _dbContext.Cards
                .Include(c => c.Wallet)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CardId == cardId);

            if (card == null)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Card not found."
                };
            }

            if (card.Status != CardStatus.ACTIVE)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Card is not active."
                };
            }

            var wallet = card.Wallet;
            if (wallet == null)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Wallet not found."
                };
            }

            if (wallet.Status != CardStatus.ACTIVE)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Wallet is not active."
                };
            }

            // Step 3: Calculate fare based on trip origin, selected destination, and card's passenger type
            // Backend ALWAYS determines the fare from active fare rules
            var fareRule = await _dbContext.FareRules
                .Where(fr =>
                    fr.OriginStationId == activeTrip.OriginStationId &&
                    fr.DestinationStationId == destinationStationId &&
                    fr.VehicleType == VehicleType.BUS &&
                    fr.PassengerType == card.PassengerType &&
                    fr.IsActive &&
                    fr.DeletedAt == null)
                .OrderByDescending(fr => fr.EffectiveDate)
                .FirstOrDefaultAsync();

            if (fareRule == null)
            {
                _logger.LogWarning("No active fare rule found for route {OriginStationId} -> {DestinationStationId} with passenger type {PassengerType}",
                    activeTrip.OriginStationId, destinationStationId, card.PassengerType);

                var originStationName = activeTrip.OriginStation?.StationName ?? "Unknown";
                return new PaymentResponse
                {
                    Success = false,
                    Message = $"No active fare found for route {originStationName} → {destinationStation.StationName}."
                };
            }

            var fareAmount = fareRule.FareAmount;
            if (fareAmount <= 0)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Invalid fare amount configured for this route."
                };
            }

            // Step 4: Validate sufficient balance
            if (wallet.Balance < fareAmount)
            {
                _logger.LogWarning("Insufficient balance. CardId: {CardId}, Balance: {Balance}, Required: {FareAmount}",
                    card.CardId, wallet.Balance, fareAmount);
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Insufficient balance."
                };
            }

            // Step 5: Process the payment inside a database transaction
            return await ProcessConductorPaymentTransactionAsync(
                card, wallet, fareAmount, fareRule.FareId, driverId, activeTrip,
                activeTrip.OriginStationId, destinationStationId, destinationStation.StationName);
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

    /// <summary>
    /// Processes a conductor-initiated payment inside a single atomic database transaction.
    /// </summary>
    private async Task<PaymentResponse> ProcessConductorPaymentTransactionAsync(
        Card card, Wallet wallet, decimal fareAmount, int fareId, int driverId, Trip trip,
        int originStationId, int destinationStationId, string destinationStationName)
    {
        // Begin database transaction for atomicity
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Retrieve active discount for the card (if any)
            var activeDiscount = await _discountService.GetActiveDiscountForCardAsync(card.CardId);
            
            // Calculate discount
            decimal regularFare = fareAmount;
            decimal discountPercentage = 0;
            decimal discountAmount = 0;
            decimal finalFare = fareAmount;
            int? discountTypeId = null;

            if (activeDiscount != null)
            {
                discountTypeId = activeDiscount.DiscountTypeId;
                discountPercentage = activeDiscount.DiscountType.DiscountPercentage;
                discountAmount = regularFare * (discountPercentage / 100);
                finalFare = regularFare - discountAmount;

                _logger.LogInformation("Discount applied. CardId: {CardId}, RegularFare: {RegularFare}, DiscountPercentage: {DiscountPercentage}%, DiscountAmount: {DiscountAmount}, FinalFare: {FinalFare}",
                    card.CardId, regularFare, discountPercentage, discountAmount, finalFare);
            }

            // Deduct the final fare from the wallet
            wallet.Balance -= finalFare;
            wallet.UpdatedAt = DateTime.UtcNow;

            // Generate the Transaction Reference Number (TRN) inside this transaction
            var trn = await _trnGenerator.GenerateNextAsync();

            // Get origin station name for transaction name
            var originStationName = trip.OriginStation?.StationName ?? "Unknown";

            // Create the transaction record with discount details
            var transactionRecord = new Models.Transaction
            {
                CardId = card.CardId,
                DriverId = driverId,
                TripId = trip.TripId,
                OriginStationId = originStationId,
                StationId = destinationStationId,
                FareId = fareId,
                RegularFare = regularFare,
                DiscountPercentage = discountPercentage > 0 ? discountPercentage : null,
                DiscountAmount = discountAmount > 0 ? discountAmount : null,
                FinalFare = finalFare,
                DiscountTypeId = discountTypeId,
                Amount = finalFare,
                TransactionType = TransactionType.PAYMENT,
                TransactionName = $"Fare payment: {originStationName} → {destinationStationName}",
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

            await _dbContext.SaveChangesAsync();

            // Commit the transaction
            await transaction.CommitAsync();

            var paymentTimestamp = DateTime.UtcNow;

            _logger.LogInformation("Conductor payment successful. CardId: {CardId}, RegularFare: {RegularFare}, FinalFare: {FinalFare}, NewBalance: {Balance}, TRN: {Trn}",
                card.CardId, regularFare, finalFare, wallet.Balance, trn);

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
                    MaskedCardNumber = MaskCardNumber(card.CardNumber),
                    OriginStationId = originStationId,
                    OriginStationName = originStationName,
                    DestinationStationId = destinationStationId,
                    DestinationStationName = destinationStationName,
                    LockedFare = finalFare,
                    RemainingBalance = wallet.Balance,
                    TransactionReferenceNumber = trn,
                    PaymentTimestamp = paymentTimestamp,
                    DriverId = driverId,
                    TransactionName = transactionRecord.TransactionName
                }
            };
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
