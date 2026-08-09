Subject: TransitPay Phase 1 Security Update — QRTicketResponse & MaskedCardNumber (Action Required)

Summary
-------
As part of Phase 1 security hardening, TransitPay removed full card numbers (PANs) from API responses and QR payloads. This is a security-driven change and requires client updates.

What changed (breaking/security-driven)
--------------------------------------
- The QRTicketResponse no longer exposes CardNumber. The property has been removed.
- Responses that previously contained CardNumber for display now provide MaskedCardNumber (e.g., "•••• 4821").
- QR Data (base64 JSON) MUST NOT contain raw PANs. It contains only minimal fields (token, timestamp, expiration, cardId, etc.).

Required client actions
-----------------------
1. Update any client code that reads response.CardNumber to use response.MaskedCardNumber for display.
2. Do NOT attempt to extract a PAN from QR Data — QR payloads do not contain PANs.
3. Update Postman collections and automated tests to assert MaskedCardNumber in responses instead of CardNumber.
4. For flows that require submitting a PAN (driver physical-scan), continue to send CardNumber in the request body; ensure the server does not echo the PAN back.

Artifacts provided
------------------
- Updated Postman collection: postman/TransitPay.postman_collection.json
- OpenAPI snapshot: postman/openapi_transitpay_phase2.json
- Migration guide: docs/MIGRATION_PHASE1_QR_AND_CARD_MASKING.md

Timeline & support
------------------
- Migration window: 3 weeks from this notification date (recommended).
- After the window, server-side compatibility shims (if any) will be removed.
- For help updating clients, contact api-team@transitpay.example.

Notes
-----
- This change improves security posture by ensuring PANs are never present in responses, logs, or QR payloads.
- The backend includes a PAN-detection test and QR security regression tests to prevent regressions in CI.

