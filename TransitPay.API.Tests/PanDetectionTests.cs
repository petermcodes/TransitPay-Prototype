using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TransitPay.API.DTOs.Payment;
using TransitPay.API.Utilities;
using Xunit;

namespace TransitPay.API.Tests
{
    /// <summary>
    /// Regression guards that panic-scan serialized DTOs and API responses to ensure no
    /// full card number (PAN) patterns leak through any endpoint.
    /// </summary>
    public class PanDetectionTests
    {
        private static readonly Regex PanRegex = new(@"\b\d{12,19}\b", RegexOptions.Compiled);

        [Fact]
        public void SerializedResponseDtos_DoNotContainPanPatterns()
        {
            // Representative DTOs to serialize and inspect
            var qr = new QRTicketResponse
            {
                // sample payload intentionally excludes PAN
                Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"token\":\"tkn_abc123\",\"cardId\":42,\"maskedCardNumber\":\"•••• 1111\"}")),
                Signature = "sig_dummy",
                CardId = 42,
                MaskedCardNumber = CardFormatter.MaskCardNumber("4111111111111111")
            };

            var payment = new PaymentResponse
            {
                Success = true,
                Message = "ok",
                Data = new PaymentData
                {
                    CardId = 42,
                    PassengerName = "Test Passenger",
                    MaskedCardNumber = CardFormatter.MaskCardNumber("4111111111111111"),
                    OriginTerminalId = 1,
                    DestinationTerminalId = 2,
                    LockedFare = 100m,
                    RegularFare = 100m,
                    FinalFare = 100m,
                    RemainingBalance = 50m,
                    PaymentTimestamp = DateTime.UtcNow
                }
            };

            // Serialize and assert no PAN-like numeric sequences
            var options = new JsonSerializerOptions { WriteIndented = false };

            string qrJson = JsonSerializer.Serialize(qr, options);
            Assert.DoesNotMatch(PanRegex, qrJson);

            // Also ensure decoded QR Data doesn't contain PAN
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(qr.Data));
            Assert.DoesNotMatch(PanRegex, decoded);

            string paymentJson = JsonSerializer.Serialize(payment, options);
            Assert.DoesNotMatch(PanRegex, paymentJson);
        }

        [Fact]
        public void SourceFiles_DoNotContainHardcodedPanLiterals()
        {
            // Locate repository root by walking up from test assembly location
            var dir = AppContext.BaseDirectory;
            var root = FindRepoRoot(dir);
            Assert.False(string.IsNullOrEmpty(root), "Repository root could not be located from test execution directory.");

            // Scan only DTO and Controller source files for PAN-like numeric literals (targets response contracts)
            var dtoDir = Path.Combine(root, "TransitPay.API", "DTOs");
            var controllersDir = Path.Combine(root, "TransitPay.API", "Controllers");

            var scanFiles = new System.Collections.Generic.List<string>();
            if (Directory.Exists(dtoDir)) scanFiles.AddRange(Directory.EnumerateFiles(dtoDir, "*.cs", SearchOption.AllDirectories));
            if (Directory.Exists(controllersDir)) scanFiles.AddRange(Directory.EnumerateFiles(controllersDir, "*.cs", SearchOption.AllDirectories));

            foreach (var file in scanFiles.Distinct())
            {
                var content = File.ReadAllText(file);
                if (PanRegex.IsMatch(content))
                {
                    var match = PanRegex.Match(content);
                    Assert.False(true, $"PAN-like literal found in DTO/Controller source file: {file}. Match: {match.Value}");
                }
            }
        }

        private static string? FindRepoRoot(string start)
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                // Look for a known repository marker (TransitPay.API.csproj)
                var candidate = Path.Combine(dir.FullName, "TransitPay.API", "TransitPay.API.csproj");
                if (File.Exists(candidate)) return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }
    }
}
