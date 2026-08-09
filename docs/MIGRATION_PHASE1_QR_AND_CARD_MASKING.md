TransitPay — Phase 1 Migration & Deprecation Note

Summary
-------
This note describes the approved Phase 1 security remediation that removes full card numbers (PANs) from API responses and QR payloads. It documents the contract changes that client applications must adopt and provides guidance for decoding QR payloads and updating Postman collections.

What changed (approved security remediation)
-----------------------------------------
1. QRTicketResponse - CardNumber removed from response and QR payload.
   - Property removed: CardNumber (raw PAN)
   - New property: MaskedCardNumber (string, e.g., "•••• 4821")
   - QR Data payload (base64-encoded JSON) no longer contains the full PAN under any key. QR payload contains minimal fields such as: token, timestamp, expiration, cardId (internal id), signature is separate.

2. Centralized masking
   - All masking of card numbers for display is handled via TransitPay.API.Utilities.CardFormatter.MaskCardNumber(string).
   - Frontends must display MaskedCardNumber where previously they consumed CardNumber for display purposes.

3. Driver and server-side request flows
   - Endpoints that accept PANs in requests for legitimate flows (e.g., POST /api/payment/scan-physical) still accept raw CardNumber in request payloads. Server MUST NOT echo the PAN back in any responses, logs, or QR payloads.

Why this change was made
------------------------
Base64 encoding is not encryption — any sensitive data included in base64 payloads is trivially recoverable. To comply with security best practices and reduce PCI exposure, PANs must not be present in API responses, logs, or QR payloads.

Client migration steps
----------------------
1. Replace response field accesses
   - Where frontend code reads response.CardNumber for display, replace with response.MaskedCardNumber.
   - Example: transaction.cardNumber -> transaction.maskedCardNumber

2. QR payload handling
   - Previously: Some clients decoded QR Data and extracted a full card number.
   - Now: QR Data should be decoded (base64 -> UTF8 JSON) and clients should use the token/metadata fields only; do not expect or use a raw PAN.
   - Example QR payload (decoded): { "token": "tkn_abc123", "cardId": 42, "timestamp": "2026-08-05T09:00:00Z" }

3. Update Postman / OpenAPI examples
   - Update all response examples to include MaskedCardNumber and remove CardNumber from examples.
   - Update any test assertions that previously checked for CardNumber to use MaskedCardNumber.

4. Verify driver flow
   - Driver app: When scanning physical cards, the request may still include CardNumber in the request body. Verify that responses only contain MaskedCardNumber in data objects.

5. Search-and-update
   - Run a repository-wide search for ".CardNumber" and "CardNumber =" and update only consumer code that parses responses (do not change server-side request DTOs that legitimately accept PANs).

Backward compatibility and deprecation window
--------------------------------------------
- This is a breaking security-driven change to responses. Consumers must update client code prior to the enforcement date.
- Recommended deprecation approach:
  1. Publish this migration note and updated Postman collection.
  2. Communicate a 3-week migration window to internal teams and external consumers.
  3. After the window, remove any server-side compatibility shims (none currently present).

Testing and verification
------------------------
- Server-side: TransitPay.API.Tests includes QRSecurityTests and PanDetectionTests that assert QR payloads and serialized responses do not contain 12-19 digit numeric sequences.
- Client-side: Update Postman tests and run Newman to validate example runs against environments.

Contact and support
-------------------
For questions or assistance updating clients, contact the API team (api-team@transitpay.example) with subject: [Migration] Phase1 Card Masking.

Revision history
----------------
- 2026-08-05: Initial migration note created by engineering during Phase 2 work.