using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.DTOs.Payment;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;

namespace TransitPay.API.Services;

/// <summary>
/// Service for managing payment sessions.
/// A payment session is created when a passenger selects a route and locks the fare.
/// Only one active session (PENDING/SCANNING/PROCESSING) may exist per card.
/// </summary>
public class PaymentSessionService : IPaymentSessionService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly ILogger<PaymentSessionService> _logger;

    /// <summary>
    /// The lifetime of a payment session before it expires.
    /// </summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(10);

    public PaymentSessionService(TransitPayDbContext dbContext, ILogger<PaymentSessionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PaymentSessionResponse> CreateOrUpdateSessionAsync(int cardId, int userId, int originStationId, int destinationStationId)
    {
        _logger.LogInformation("Creating/updating payment session for card {CardId} from station {OriginStationId} to station {DestinationStationId}",
            cardId, originStationId, destinationStationId);

        // Validate inputs
        if (cardId <= 0)
        {
            return new PaymentSessionResponse { Success = false, Message = "Invalid card ID." };
        }

        if (userId <= 0)
        {
            return new PaymentSessionResponse { Success = false, Message = "Invalid user ID." };
        }

        if (originStationId <= 0)
        {
            return new PaymentSessionResponse { Success = false, Message = "Invalid origin station ID." };
        }

        if (destinationStationId <= 0)
        {
            return new PaymentSessionResponse { Success = false, Message = "Invalid destination station ID." };
        }

        if (originStationId == destinationStationId)
        {
            return new PaymentSessionResponse { Success = false, Message = "Origin and destination stations must be different." };
        }

        // Validate card exists and is active
        var card = await _dbContext.Cards.FindAsync(cardId);
        if (card == null)
        {
            return new PaymentSessionResponse { Success = false, Message = "Card not found." };
        }

        if (card.Status != CardStatus.ACTIVE)
        {
            return new PaymentSessionResponse { Success = false, Message = "Card is not active." };
        }

        // Validate stations exist
        var originStation = await _dbContext.Stations.FindAsync(originStationId);
        if (originStation == null)
        {
            return new PaymentSessionResponse { Success = false, Message = "Origin station not found." };
        }

        var destinationStation = await _dbContext.Stations.FindAsync(destinationStationId);
        if (destinationStation == null)
        {
            return new PaymentSessionResponse { Success = false, Message = "Destination station not found." };
        }

        // Server ALWAYS determines the fare from active fare rules
        var fareRule = await _dbContext.FareRules
            .Where(fr =>
                fr.OriginStationId == originStationId &&
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
                originStationId, destinationStationId, card.PassengerType);
            return new PaymentSessionResponse
            {
                Success = false,
                Message = $"No active fare found for route {originStation.StationName} → {destinationStation.StationName}."
            };
        }

        var fareAmount = fareRule.FareAmount;
        if (fareAmount <= 0)
        {
            return new PaymentSessionResponse { Success = false, Message = "Invalid fare amount configured for this route." };
        }

        // Check for an existing active session (PENDING/SCANNING/PROCESSING)
        var existingSession = await _dbContext.PaymentSessions
            .FirstOrDefaultAsync(ps =>
                ps.CardId == cardId &&
                (ps.Status == PaymentSessionStatus.PENDING ||
                 ps.Status == PaymentSessionStatus.SCANNING ||
                 ps.Status == PaymentSessionStatus.PROCESSING));

        if (existingSession != null)
        {
            // If the session is being processed, reject the update
            if (existingSession.Status == PaymentSessionStatus.SCANNING ||
                existingSession.Status == PaymentSessionStatus.PROCESSING)
            {
                return new PaymentSessionResponse
                {
                    Success = false,
                    Message = "Payment is currently being processed."
                };
            }

            // If the session is expired, mark it EXPIRED and create a new one
            if (DateTime.UtcNow > existingSession.ExpiresAt)
            {
                existingSession.Status = PaymentSessionStatus.EXPIRED;
                existingSession.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                // Update the existing PENDING session with the new route and re-lock the fare
                _logger.LogInformation("Updating existing payment session {PaymentSessionId} for card {CardId}",
                    existingSession.PaymentSessionId, cardId);

                existingSession.OriginStationId = originStationId;
                existingSession.DestinationStationId = destinationStationId;
                existingSession.Fare = fareAmount;
                existingSession.UpdatedAt = DateTime.UtcNow;
                existingSession.ExpiresAt = DateTime.UtcNow.Add(SessionLifetime);
                await _dbContext.SaveChangesAsync();

                return new PaymentSessionResponse
                {
                    Success = true,
                    Message = "Payment session updated successfully.",
                    Data = MapToData(existingSession, originStation, destinationStation)
                };
            }
        }

        // Create a new PENDING payment session with the locked fare
        var newSession = new PaymentSession
        {
            PaymentSessionId = Guid.NewGuid(),
            CardId = cardId,
            UserId = userId,
            OriginStationId = originStationId,
            DestinationStationId = destinationStationId,
            Fare = fareAmount,
            Status = PaymentSessionStatus.PENDING,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(SessionLifetime)
        };

        _dbContext.PaymentSessions.Add(newSession);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created new payment session {PaymentSessionId} for card {CardId} with locked fare {Fare}",
            newSession.PaymentSessionId, cardId, fareAmount);

        return new PaymentSessionResponse
        {
            Success = true,
            Message = "Payment session created successfully.",
            Data = MapToData(newSession, originStation, destinationStation)
        };
    }

    /// <inheritdoc />
    public async Task<PaymentSessionResponse?> GetActiveSessionAsync(int cardId)
    {
        _logger.LogInformation("Retrieving active payment session for card {CardId}", cardId);

        var session = await _dbContext.PaymentSessions
            .Include(ps => ps.OriginStation)
            .Include(ps => ps.DestinationStation)
            .FirstOrDefaultAsync(ps =>
                ps.CardId == cardId &&
                ps.Status == PaymentSessionStatus.PENDING);

        if (session == null)
        {
            return null;
        }

        return new PaymentSessionResponse
        {
            Success = true,
            Message = "Active payment session retrieved successfully.",
            Data = MapToData(session, session.OriginStation, session.DestinationStation)
        };
    }

    /// <inheritdoc />
    public async Task ExpireSessionAsync(Guid paymentSessionId)
    {
        var session = await _dbContext.PaymentSessions.FindAsync(paymentSessionId);
        if (session == null || session.Status == PaymentSessionStatus.EXPIRED)
        {
            return;
        }

        session.Status = PaymentSessionStatus.EXPIRED;
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Payment session {PaymentSessionId} marked as EXPIRED", paymentSessionId);
    }

    /// <summary>
    /// Maps a PaymentSession entity to the response DTO.
    /// </summary>
    private static PaymentSessionData MapToData(PaymentSession session, Station? originStation, Station? destinationStation)
    {
        return new PaymentSessionData
        {
            PaymentSessionId = session.PaymentSessionId,
            CardId = session.CardId,
            UserId = session.UserId,
            OriginStationId = session.OriginStationId,
            DestinationStationId = session.DestinationStationId,
            OriginStationName = originStation?.StationName,
            DestinationStationName = destinationStation?.StationName,
            LockedFare = session.Fare,
            Status = session.Status,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            ExpiresAt = session.ExpiresAt
        };
    }
}