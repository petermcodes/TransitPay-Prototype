using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TransitPay.API.Data;
using TransitPay.API.DTOs.Payment;
using TransitPay.API.Enums;
using TransitPay.API.Interfaces;
using TransitPay.API.Models;
using TransitPay.API.Utilities;

namespace TransitPay.API.Services;

/// <summary>
/// Centralized service for QR code generation, retrieval, regeneration, and validation.
/// QR codes are permanently associated with transit cards and digitally signed
/// using HMAC-SHA256 with the centralized security key.
/// </summary>
public class QRService : IQRService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly ISecurityKeyProvider _securityKeyProvider;
    private readonly ILogger<QRService> _logger;

    public QRService(
        TransitPayDbContext dbContext,
        ISecurityKeyProvider securityKeyProvider,
        ILogger<QRService> logger)
    {
        _dbContext = dbContext;
        _securityKeyProvider = securityKeyProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<QRTicketResponse> GenerateOrRetrieveQRAsync(int cardId)
    {
        _logger.LogInformation("Generating or retrieving QR for card {CardId}", cardId);

        // Check if card exists and is active
        var card = await _dbContext.Cards.FindAsync(cardId);
        if (card == null)
        {
            _logger.LogWarning("QR generation failed - card not found: {CardId}", cardId);
            throw new InvalidOperationException("Card not found.");
        }

        if (card.Status != CardStatus.ACTIVE)
        {
            _logger.LogWarning("QR generation failed - card not active: {CardId}, Status: {Status}", cardId, card.Status);
            throw new InvalidOperationException($"Card is not active. Status: {card.Status}");
        }

        // Check if an active QR already exists for this card
        var existingQR = await _dbContext.QRCodes
            .FirstOrDefaultAsync(q => q.CardId == cardId && q.IsActive);

        if (existingQR != null)
        {
            _logger.LogInformation("Existing active QR found for card {CardId}", cardId);
            return CreateTicket(existingQR, card);
        }

        // Create a new QR code
        var qrCode = new QRCode
        {
            CardId = cardId,
            Token = GenerateToken(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.QRCodes.Add(qrCode);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("New QR generated for card {CardId}", cardId);
        return CreateTicket(qrCode, card);
    }

    /// <inheritdoc />
    public async Task<QRTicketResponse?> GetQRAsync(int cardId)
    {
        _logger.LogInformation("Retrieving QR for card {CardId}", cardId);

        var card = await _dbContext.Cards.FindAsync(cardId);
        if (card == null)
        {
            _logger.LogWarning("QR retrieval failed - card not found: {CardId}", cardId);
            return null;
        }

        var qrCode = await _dbContext.QRCodes
            .FirstOrDefaultAsync(q => q.CardId == cardId && q.IsActive);

        if (qrCode == null)
        {
            _logger.LogInformation("No active QR found for card {CardId}", cardId);
            return null;
        }

        return CreateTicket(qrCode, card);
    }

    /// <inheritdoc />
    public async Task<QRTicketResponse> RegenerateQRAsync(int cardId)
    {
        _logger.LogInformation("Regenerating QR for card {CardId}", cardId);

        var card = await _dbContext.Cards.FindAsync(cardId);
        if (card == null)
        {
            _logger.LogWarning("QR regeneration failed - card not found: {CardId}", cardId);
            throw new InvalidOperationException("Card not found.");
        }

        // Revoke all existing active QRs for this card
        var existingQRs = await _dbContext.QRCodes
            .Where(q => q.CardId == cardId && q.IsActive)
            .ToListAsync();

        foreach (var qr in existingQRs)
        {
            qr.IsActive = false;
            qr.RevokedAt = DateTime.UtcNow;
            qr.UpdatedAt = DateTime.UtcNow;
        }

        // Create a new QR code
        var newQR = new QRCode
        {
            CardId = cardId,
            Token = GenerateToken(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.QRCodes.Add(newQR);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("QR regenerated for card {CardId}. Old QRs revoked: {Count}", cardId, existingQRs.Count);
        return CreateTicket(newQR, card);
    }

    /// <inheritdoc />
    public async Task<int> ValidateQRAsync(string qrData, string signature)
    {
        _logger.LogInformation("Validating QR code");

        // Decode the QR data (Base64Url-encoded for QR code safety)
        string json;
        try
        {
            json = Encoding.UTF8.GetString(FromBase64Url(qrData));
        }
        catch (FormatException)
        {
            _logger.LogWarning("QR validation failed - invalid base64url format");
            throw new InvalidOperationException("Invalid QR code format.");
        }

        // Verify the signature
        var expectedSignature = ComputeSignature(json);
        if (!CryptographicOperations.FixedTimeEquals(
            FromBase64Url(signature),
            FromBase64Url(expectedSignature)))
        {
            _logger.LogWarning("QR validation failed - signature mismatch");
            throw new InvalidOperationException("Invalid QR code signature.");
        }

        // Parse the payload
        QRPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<QRPayload>(json);
        }
        catch (JsonException)
        {
            _logger.LogWarning("QR validation failed - invalid JSON payload");
            throw new InvalidOperationException("Invalid QR code data.");
        }

        if (payload == null || payload.CardId <= 0 || string.IsNullOrWhiteSpace(payload.Token))
        {
            _logger.LogWarning("QR validation failed - missing required fields");
            throw new InvalidOperationException("Invalid QR code data.");
        }

        // Look up the QR code in the database
        var qrCode = await _dbContext.QRCodes
            .Include(q => q.Card)
            .FirstOrDefaultAsync(q => q.Token == payload.Token && q.CardId == payload.CardId);

        if (qrCode == null)
        {
            _logger.LogWarning("QR validation failed - QR code not found in database");
            throw new InvalidOperationException("QR code is not registered.");
        }

        if (!qrCode.IsActive)
        {
            _logger.LogWarning("QR validation failed - QR code is revoked. CardId: {CardId}", payload.CardId);
            throw new InvalidOperationException("QR code has been revoked. Please regenerate your QR code.");
        }

        if (qrCode.Card == null || qrCode.Card.Status != CardStatus.ACTIVE)
        {
            _logger.LogWarning("QR validation failed - card not active. CardId: {CardId}", payload.CardId);
            throw new InvalidOperationException("Card is not active.");
        }

        _logger.LogInformation("QR validated successfully for card {CardId}", payload.CardId);
        return payload.CardId;
    }

    /// <summary>
    /// Creates a signed QR ticket response from a QRCode entity and its associated card.
    /// </summary>
    private QRTicketResponse CreateTicket(QRCode qrCode, Card card)
    {
        // Build payload with only non-sensitive fields. DO NOT include the full card number.
        // Keep the payload minimal (CardId + Token only) to reduce QR density for reliable scanning.
        var payload = new QRPayload
        {
            CardId = card.CardId,
            // CardNumber intentionally omitted from payload to prevent PAN exposure.
            // PlanId NOT included - QR is permanent, PlanId changes per trip
            Token = qrCode.Token
        };

        var json = JsonSerializer.Serialize(payload);
        var data = ToBase64Url(Encoding.UTF8.GetBytes(json));
        var signature = ComputeSignature(json);

        return new QRTicketResponse
        {
            Data = data,
            Signature = signature,
            CardId = card.CardId,
            // Expose only masked card number to frontend for display.
            MaskedCardNumber = CardFormatter.MaskCardNumber(card.CardNumber)
        };
    }

    /// <summary>
    /// Computes the HMAC-SHA256 signature for the given JSON payload.
    /// Uses the centralized security key provider.
    /// </summary>
    private string ComputeSignature(string json)
    {
        var keyBytes = _securityKeyProvider.GetSigningKeyBytes();
        var signatureBytes = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(json));
        return ToBase64Url(signatureBytes);
    }

    /// <summary>
    /// Generates a cryptographically secure random token for the QR code.
    /// 32 bytes of entropy, base64url-encoded.
    /// </summary>
    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Converts a byte array to a Base64Url string (URL-safe, no padding).
    /// Uses '-' and '_' instead of '+' and '/', and omits '=' padding.
    /// This ensures the encoded string is safe for QR codes and URL transmission.
    /// </summary>
    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Converts a Base64Url string back to a byte array.
    /// Restores padding and reverses the '-'/'_' substitutions.
    /// </summary>
    private static byte[] FromBase64Url(string base64Url)
    {
        var padded = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        // Restore padding
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}

/// <summary>
/// Internal payload structure for QR codes.
/// Contains only non-sensitive data — no passwords, no wallet balances.
/// No fare, origin, or destination — those belong to the active payment session.
/// Important: Do NOT include full card numbers in this payload.
/// </summary>
public class QRPayload
{
    public int CardId { get; set; }

    // NOTE: CardNumber intentionally removed to avoid embedding PAN in QR payload.
    // NOTE: PlanId NOT included because QR is permanent but PlanId changes per trip.
    // NOTE: QRVersion and CreatedAt removed to keep the payload minimal for reliable scanning.

    public string Token { get; set; } = string.Empty;
}
