using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.DTOs.Card;
using TransitPay.API.Enums;
using TransitPay.API.Exceptions;
using TransitPay.API.Interfaces;
using TransitPay.API.Mappings;
using TransitPay.API.Utilities;

namespace TransitPay.API.Services;

/// <summary>
/// Service for retrieving and managing Transit Cards.
/// Pure data access and business rules — no authorization logic.
/// Repository pattern is not used in this solution; DbContext is injected
/// directly per existing module conventions (TripService, PaymentService, etc.).
/// </summary>
public class CardService : ICardService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly ILogger<CardService> _logger;

    /// <summary>
    /// Creates a new CardService.
    /// </summary>
    public CardService(TransitPayDbContext dbContext, ILogger<CardService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CardDto?> GetCardByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var card = await _dbContext.Cards
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);

            if (card == null)
            {
                _logger.LogWarning("Card not found for user {UserId}", userId);
                return null;
            }

            _logger.LogInformation("Retrieved card {CardId} for user {UserId}", card.CardId, userId);
            return CardMapper.ToDto(card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving card for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CardDetailsDto?> GetCardByNumberAsync(string cardNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var card = await _dbContext.Cards
                .AsNoTracking()
                .Where(c => c.CardNumber == cardNumber && c.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);

            if (card == null)
            {
                _logger.LogWarning("Card not found: {CardNumber}", CardFormatter.MaskCardNumber(cardNumber));
                return null;
            }

            _logger.LogInformation("Retrieved card {CardId} by card number", card.CardId);
            return CardMapper.ToDetailsDto(card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving card by number: {CardNumber}", CardFormatter.MaskCardNumber(cardNumber));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CardCreatedDto> CreateCardAsync(CardRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Business rule: no duplicate card numbers
            var existingCard = await _dbContext.Cards
                .AsNoTracking()
                .AnyAsync(c => c.CardNumber == request.CardNumber, cancellationToken);

            if (existingCard)
            {
                _logger.LogWarning("Duplicate card number {CardNumber} attempted", CardFormatter.MaskCardNumber(request.CardNumber));
                throw new DuplicateCardException("Card number already exists.");
            }

            // Atomic transaction: Card + Wallet must be created together
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var card = new Models.Card
            {
                CardNumber = request.CardNumber,
                UserId = request.UserId,
                Status = CardStatus.ACTIVE,
                IssueDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
            };

            _dbContext.Cards.Add(card);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _dbContext.Wallets.Add(new Models.Wallet
            {
                CardId = card.CardId,
                Balance = 0,
                Status = CardStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Created card {CardNumber} for user {UserId}",
                CardFormatter.MaskCardNumber(request.CardNumber),
                request.UserId);

            return CardMapper.ToCreatedDto(card);
        }
        catch (DuplicateCardException)
        {
            throw; // Re-throw domain exception without wrapping
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating card {CardNumber}", CardFormatter.MaskCardNumber(request.CardNumber));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CardValidationDto?> ValidateCardAsync(string cardNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _dbContext.Cards
                .AsNoTracking()
                .Where(c => c.CardNumber == cardNumber && c.DeletedAt == null)
                .Select(c => new CardValidationDto
                {
                    CardId = c.CardId,
                    MaskedCardNumber = CardFormatter.MaskCardNumber(c.CardNumber) ?? string.Empty,
                    Status = c.Status,
                    Balance = c.Wallet != null ? c.Wallet.Balance : 0m
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("Card not found during validation: {CardNumber}", CardFormatter.MaskCardNumber(cardNumber));
                return null;
            }

            _logger.LogInformation("Card {CardId} validated successfully", result.CardId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating card: {CardNumber}", CardFormatter.MaskCardNumber(cardNumber));
            throw;
        }
    }
}