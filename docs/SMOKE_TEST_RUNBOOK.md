TransitPay Phase 2 — Runtime Smoke Test Runbook

Purpose
-------
This runbook describes how to run an automated smoke test sequence that validates runtime API contracts for Phase 2 readiness. It does not modify application code.

Prerequisites
-------------
- A machine with network access to the PostgreSQL database used by the development environment.
- dotnet SDK installed (matching target framework net10.0).
- curl and jq installed.
- The repository checked out and built (optional; the script starts the API via dotnet run).
- Required environment variables set in the shell:
  - DB_PASSWORD — the database password used by DefaultConnection in appsettings.json
  - JWT_KEY — symmetric key used for JWT signing (same one used in the environment your dev server uses)
  - ADMIN_BOOTSTRAP_PASSWORD — bootstrap password used to seed the Admin account

Files included
--------------
- run_smoke_tests.ps1 (PowerShell script at repository root)
- postman/openapi_transitpay_phase2.json (OpenAPI snapshot)
- postman/TransitPay.postman_collection.json (Postman collection with updated MaskedCardNumber examples)
- docs/MIGRATION_PHASE1_QR_AND_CARD_MASKING.md
- docs/NOTIFY_CONSUMERS_PHASE1.md

How to run (example)
--------------------
1. In PowerShell set environment variables (example):
   $env:DB_PASSWORD = 'YourDbPassword'
   $env:JWT_KEY = '32+chars+at+least+32charslong123456'
   $env:ADMIN_BOOTSTRAP_PASSWORD = 'Secur3AdminP@ss!'

2. From repo root run:
   .\run_smoke_tests.ps1 -ApiUrl 'http://localhost:5000'

3. The script will:
   - Start the API with dotnet run (Development environment)
   - Wait for /health to respond
   - Perform authentication flows (register/login/refresh/logout)
   - Perform passenger flows (cards/me, get QR, decode QR Data and verify no PAN present)
   - Perform driver flows (stations, active trip, scan-physical sample)
   - Perform admin flows (admin login using seeded admin credentials and get drivers)
   - Print JSON responses and basic PAN detection results

What the script checks
----------------------
- JWT tokens are issued on login and refresh returns a new token
- Authorization header is accepted for protected endpoints
- QR Data (base64) decodes to JSON that does not contain contiguous 12-19 digit sequences (PAN regex)
- Responses that previously exposed CardNumber now expose MaskedCardNumber (clients should check for maskedCardNumber)
- Sample endpoints run without serialization exceptions

Interpreting results
--------------------
- Any occurrence of a 12-19 digit contiguous numeric sequence in responses or decoded QR payloads should be investigated immediately.
- HTTP responses and response bodies printed by the script can be used as evidence in the validation report.

If a defect is found
--------------------
- Do NOT introduce new features. Make the minimal code fix required to correct the runtime defect (e.g., ensure mapping uses MaskedCardNumber or QR payload omits PAN).
- Rebuild: dotnet build
- Rerun: dotnet test and run_smoke_tests.ps1

CI integration
--------------
- The PanDetectionTests are committed in TransitPay.API.Tests and will run with dotnet test in CI. Ensure your CI job runs dotnet test and fails the build on test failures.

Contact
-------
If you need assistance running the smoke tests, contact the API team (api-team@transitpay.example).
