TransitPay Phase 2 — Smoke Test Evidence Template

Use this template to collect and record evidence during the Phase 2 runtime smoke test. Fill in each section with command outputs, HTTP status codes, sample responses, decoded QR payloads, timestamps, and any defects found.

Environment
-----------
- Date (UTC): 
- Executor: 
- Hostname / CI job: 
- Repository commit SHA: 
- API base URL: 
- Database (host:port/db): (do not include passwords)
- JWT_KEY: (referenced, not included)
- ADMIN_BOOTSTRAP_PASSWORD: (referenced, not included)

Commands executed
-----------------
- dotnet build: (command + output file reference)
- dotnet test: (command + output file reference)
- Started API: (dotnet run args / process id)
- Smoke-test script: run_smoke_tests.ps1 -ApiUrl '...' (include timestamp)

Authentication
--------------
- Register (POST /api/auth/register)
  - Request: { firstName, lastName, mobileNumber, password }
  - HTTP status: 
  - Sample response body (truncated):
  ```json
  {}
  ```
  - Pass/Fail: 
  - Notes:

- Login (POST /api/auth/login)
  - Request: { mobileNumber, password }
  - HTTP status: 
  - Sample response: (include token length but not token value)
  - JWT issued: Yes/No
  - Refresh token present: Yes/No
  - Pass/Fail: 

- Refresh (POST /api/auth/refresh)
  - HTTP status: 
  - Sample response: (new token present?)
  - Pass/Fail:

- Logout (POST /api/auth/logout)
  - HTTP status: 
  - Pass/Fail:

Passenger APIs
--------------
For each endpoint include: HTTP method, Route, Authorization, Request DTO, Response DTO, Status code, Sample response (trimmed), PAN check result.

- GET /api/cards/me
  - HTTP status:
  - Sample response:
  ```json
  {}
  ```
  - maskedCardNumber present: Yes/No
  - Raw CardNumber present: Yes/No
  - Pass/Fail:

- GET /api/payment/qr/{cardId}
  - HTTP status:
  - Sample response (QRTicketResponse):
  ```json
  {
    "data": "<base64>",
    "signature": "...",
    "cardId": ...,
    "maskedCardNumber": "•••• 1111"
  }
  ```
  - Decoded QR JSON (base64 -> UTF-8 JSON):
  ```json
  {}
  ```
  - Raw PAN in decoded payload: Yes/No
  - Fields present (only approved): (list)
  - Signature verification: (describe how verified)
  - Pass/Fail:

- Payment endpoints (scan, process-conductor, scan-physical)
  - For each: HTTP status, sample response, maskedCardNumber present, raw PAN present, Pass/Fail

Driver APIs
-----------
- GET /api/station
  - HTTP status, sample response, Pass/Fail
- GET /api/Trip/active
  - HTTP status, sample response, Pass/Fail
- POST /api/payment/scan (QR flow)
  - HTTP status, sample response, maskedCardNumber present, raw PAN present, Pass/Fail
- POST /api/payment/scan-physical
  - HTTP status, sample response, maskedCardNumber present in response: Yes/No, raw PAN in response: Yes/No, Pass/Fail

Admin APIs
----------
- POST /api/auth/login (admin)
  - HTTP status, sample response
- GET /api/driver
  - HTTP status, sample response (list)
  - maskedCardNumber present where applicable: Yes/No
- POST /api/driver (create driver)
  - HTTP status, sample response, verify no hardcoded default password returned, Pass/Fail
- Other admin endpoints exercised: list each with HTTP status and Pass/Fail

Runtime Contract Verification Checklist
---------------------------------------
For every tested endpoint confirm:
- [ ] HTTP method matches Swagger
- [ ] Route matches Swagger
- [ ] Authorization required/optional matches Swagger
- [ ] Request DTO shape matches Swagger
- [ ] Response DTO shape matches Swagger
- [ ] Status codes are as documented
- [ ] Serialization correct (no exceptions)
- [ ] Nullable values handled as documented

Regression Verification
-----------------------
- [ ] Card creation (manual or API) works
- [ ] Card retrieval works
- [ ] Driver creation works
- [ ] Authentication flows work (login, refresh, logout)
- [ ] No serialization exceptions

PAN detection and QR validation
-------------------------------
- PAN regex used: contiguous 12-19 digits (\b\d{12,19}\b)
- For each response and decoded QR payload record result: Passed/Failed
- If failed include exact string match and file/endpoint where found (do NOT include PAN values in public reports)

Collected logs and artifacts
---------------------------
- dotnet build output: (path)
- dotnet test output: (path)
- Smoke-test script stdout capture: (path)
- Sample response captures: (folder)
- Decoded QR payload captures: (folder)

Final assessment
----------------
- Overall result: Pass / Conditional / Fail
- Issues discovered (if any): list with reproduction steps and minimal recommended fix
- Recommendation for Phase 3 readiness: (approve / conditionally approve / reject)

Sign-off
--------
- Tester name, date, signature

