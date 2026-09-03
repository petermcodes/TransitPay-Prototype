namespace TransitPay.API.Enums;

/// <summary>
/// The kind of financial movement a transaction represents.
/// Serialized as strings (via JsonStringEnumConverter) so the frontends can
/// safely compare transaction types (e.g., "PAYMENT", "TOP_UP").
/// </summary>
public enum TransactionType
{
    /// <summary>A fare payment collected for a trip.</summary>
    PAYMENT,

    /// <summary>A load/top-up of wallet balance.</summary>
    TOP_UP,

    /// <summary>A refund/credit back to the wallet.</summary>
    REFUND,

    /// <summary>A legacy fare-type transaction (backward compatibility).</summary>
    FARE
}