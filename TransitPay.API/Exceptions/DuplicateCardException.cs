namespace TransitPay.API.Exceptions;

/// <summary>
/// Thrown when a card with the same card number already exists.
/// This is a domain business-rule violation, mapped to HTTP 400 by the controller.
/// </summary>
public class DuplicateCardException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateCardException"/> class.
    /// </summary>
    /// <param name="message">A human-readable message describing the duplicate card violation.</param>
    public DuplicateCardException(string message) : base(message)
    {
    }
}