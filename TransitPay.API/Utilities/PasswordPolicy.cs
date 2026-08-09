namespace TransitPay.API.Utilities;

/// <summary>
/// Centralized password policy enforcement for all TransitPay account types.
/// All validation is performed server-side. The policy is applied to:
///   - Public passenger registration
///   - Admin-created driver accounts
///   - Admin-created administrator accounts
///   - Admin password resets
/// </summary>
public static class PasswordPolicy
{
    /// <summary>The minimum required password length.</summary>
    public const int MinimumLength = 8;

    /// <summary>Characters considered "special" for password complexity.</summary>
    private const string SpecialCharacters = "!@#$%^&*()_+-=[]{};':\"\\|,.<>/?`~";

    /// <summary>
    /// Frequently-used/weak passwords that are rejected outright.
    /// Case-insensitive comparison.
    /// </summary>
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Generic weak passwords
        "password",
        "password123",
        "password1234",
        "password12345",
        "password123456",
        "password123!",
        "password12",
        "password!",
        "1234567890",
        "123456789012",
        "1234567890123",
        "0123456789",
        "qwerty",
        "qwerty123",
        "qwerty1234",
        "qwerty12345",
        "qwerty123456",
        "qwerty123!",
        "qwertyuiop",
        "qwertyuiop123",
        "letmein",
        "letmein123",
        "letmein123!",
        "welcome",
        "welcome123",
        "welcome123!",
        "admin",
        "admin123",
        "admin1234",
        "admin12345",
        "admin123456",
        "admin123!",
        "administrator",
        "administrator1",
        "administrator123",
        "changeme",
        "changeme123",
        "changeme123!",
        "default",
        "default123",
        "default123!",
        "monkey",
        "monkey123",
        "monkey123!",
        "dragon",
        "dragon123",
        "dragon123!",
        "football",
        "football123",
        "football123!",
        "baseball",
        "baseball123",
        "baseball123!",
        "superman",
        "superman123",
        "superman123!",
        "batman",
        "batman123",
        "batman123!",
        "iloveyou",
        "iloveyou123",
        "iloveyou123!",
        "abc123",
        "abc123456",
        "abc123456789",
        "passw0rd",
        "passw0rd123",
        "passw0rd123!",
        "p@ssw0rd",
        "p@ssw0rd123",
        "P@ssw0rd",
        "P@ssw0rd123",
        "trustno1",
        "trustno123",
        "trustno123!",
        "secret",
        "secret123",
        "secret123!",
        "hello",
        "hello123",
        "hello123!",
        "summer",
        "summer123",
        "summer123!",
        "winter",
        "winter123",
        "winter123!",
        "spring",
        "spring123",
        "spring123!",
        "autumn",
        "autumn123",
        "autumn123!",
        "1qaz2wsx",
        "1qaz2wsx3edc",
        "1q2w3e4r",
        "1q2w3e4r5t",
        "zaq12wsx",
        // 12+ char common passwords with all character classes
        "Password12345!",
        "P@ssw0rd12345",
        "Qwerty123456!",
        "Admin123456!",
        "Welcome12345!",
        "Letmein12345!",
        "shadow",
        "shadow123",
        "shadow123!",
        "master",
        "master123",
        "master123!",
        "000000000000",
        "111111111111",
        "222222222222",
        "333333333333",
        "444444444444",
        "555555555555",
        "666666666666",
        "777777777777",
        "888888888888",
        "999999999999"
    };

    /// <summary>
    /// Validates a password against the TransitPay password policy.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="firstName">The user's first name (used for personal-info rejection).</param>
    /// <param name="lastName">The user's last name (used for personal-info rejection).</param>
    /// <param name="mobileNumber">The user's mobile number (used for personal-info rejection).</param>
    /// <returns>
    /// A tuple with <c>IsValid</c> indicating whether the password passes all policy checks,
    /// and <c>ErrorMessage</c> containing the specific policy failure when invalid.
    /// </returns>
    public static (bool IsValid, string? ErrorMessage) Validate(
        string? password,
        string? firstName = null,
        string? lastName = null,
        string? mobileNumber = null)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password is required.");
        }

        if (password.Length < MinimumLength)
        {
            return (false, $"Password must be at least {MinimumLength} characters long.");
        }

        if (!password.Any(char.IsDigit))
        {
            return (false, "Password must contain at least one number.");
        }

        if (!password.Any(c => SpecialCharacters.Contains(c)))
        {
            return (false, "Password must contain at least one special character.");
        }

        if (CommonPasswords.Contains(password))
        {
            return (false, "Password is too common. Please choose a more secure password.");
        }

        // Reject passwords that contain personal information.
        // Names are only checked when they are at least 3 characters long to avoid
        // false positives from short names. The mobile number is always checked.
        if (!string.IsNullOrWhiteSpace(firstName) && firstName.Length >= 3 &&
            password.Contains(firstName, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Password must not contain your personal information (such as your name).");
        }

        if (!string.IsNullOrWhiteSpace(lastName) && lastName.Length >= 3 &&
            password.Contains(lastName, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Password must not contain your personal information (such as your name).");
        }

        if (!string.IsNullOrWhiteSpace(mobileNumber) &&
            password.Contains(mobileNumber, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Password must not contain your mobile number.");
        }

        return (true, null);
    }
}