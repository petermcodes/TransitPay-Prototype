namespace TransitPay.API.Enums;

/// <summary>
/// Lifecycle status of a transaction record.
/// Successful fare payments and top-ups are stored as COMPLETED; idempotency
/// duplicates and rejected attempts never reach a COMPLETED state.
/// </summary>
public enum TransactionStatus
{
    /// <summary>The transaction has been created but not yet finalized.</summary>
    PENDING,

    /// <summary>The transaction succeeded and the balance was updated.</summary>
    COMPLETED,

    /// <summary>The transaction failed (e.g., insufficient balance). No balance was changed.</summary>
    FAILED,

    /// <summary>The transaction was cancelled/voided.</summary>
    CANCELLED
}