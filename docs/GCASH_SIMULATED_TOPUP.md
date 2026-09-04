# Simulated GCash Top-Up (Passenger Android)

## Overview

Passengers can top up their TransitPay wallet via a **simulated GCash checkout**. The
flow mirrors a real GCash gateway integration (payment intent → authentication →
confirm → credit) end-to-end, but runs in **sandbox mode**: no real money moves and
the wallet is credited by the TransitPay API itself. The UI is clearly labelled as a
simulation ("Simulation Mode" banner on the Top Up screen, "SANDBOX" badge in the
GCash checkout screen).

Scope: `passenger-android` (Capacitor app). The web `passenger-app` still shows the
admin-credit notice and can adopt the same flow later — the backend endpoints are
shared.

## Flow

```
Passenger App                     TransitPay API                        Database
─────────────                     ──────────────                        ────────
Top Up screen
  ├─ amount + Pay with GCash ───► POST /api/topup/gcash/initiate ──►  Transaction (TOP_UP, PENDING, TRN)
  │                                (ownership + amount checks)         GcashTopUpSession (PENDING, expiry 15 min)
GCash checkout screen
  ├─ mobile number (09XX…)
  ├─ sandbox OTP 123456 ────────► POST /api/topup/gcash/confirm ───►  wallet.Balance += amount
  │                                (atomic credit)                     Transaction → COMPLETED (RemainingBalance)
  │                                                                    Session → COMPLETED (GC- reference)
  └─ receipt: TRN + GCash ref
     + new balance
```

Terminal states (never credit the wallet):

| Action | Session | Transaction |
|---|---|---|
| Wrong OTP ×3 | `FAILED` | `FAILED` |
| User cancels in checkout | `CANCELLED` | `CANCELLED` |
| Session older than 15 min (lazy expiry on next initiate/confirm/status) | `EXPIRED` | `CANCELLED` |

Confirming an already-`COMPLETED` session is **idempotent** — the API returns the
original receipt without crediting a second time (safe against network retries).

The pending `TOP_UP` transaction appears in transaction history while the checkout
is open (rendered with the existing `pending` status chip), giving an audit trail
that matches what a real gateway integration would produce.

## API endpoints (`TopUpController`, passenger JWT required, ownership enforced)

| Method | Route | Body | Notes |
|---|---|---|---|
| POST | `/api/topup/gcash/initiate` | `{ cardId, amount }` | Validates amount (₱1–₱10,000) and card ownership; returns session + TRN |
| POST | `/api/topup/gcash/confirm` | `{ sessionId, otp }` | Sandbox OTP is `123456`; 3 wrong attempts → `FAILED` |
| POST | `/api/topup/gcash/cancel` | `{ sessionId }` | Voids a pending session |
| GET | `/api/topup/gcash/status/{sessionId}` | — | Status polling; lazily expires stale sessions |

All endpoints return the standard `{ success, message, data }` envelope.
Postman requests live in `postman/collections/TransitPay API/Top Up/`.

## Configuration (`appsettings.json`)

```json
"Payments": {
  "Gcash": {
    "MinAmount": 1,
    "MaxAmount": 10000,
    "SessionExpiryMinutes": 15
  }
}
```

Bound to `PaymentSettings` (`TransitPay.API/Configuration/PaymentSettings.cs`).

## Implementation map

| Layer | File(s) |
|---|---|
| Model + enum | `TransitPay.API/Models/GcashTopUpSession.cs`, `Enums/GcashSessionStatus.cs` |
| Service | `TransitPay.API/Services/GcashTopUpService.cs` + `Interfaces/IGcashTopUpService.cs` |
| Controller + DTOs | `TransitPay.API/Controllers/TopUpController.cs`, `DTOs/TopUp/` |
| Migration | `TransitPay.API/Migrations/*_AddGcashTopUpSessions.cs` (auto-applied at startup) |
| App API client | `passenger-android/src/lib/gcash.ts` |
| App UI | `passenger-android/src/PassengerApp.tsx` (`TopUpScreen`, `GcashCheckoutScreen`) |
| Tests | `TransitPay.API.Tests/GcashTopUpServiceTests.cs` (13 unit tests) |

## Going live with a real PSP later

The simulation was deliberately shaped like a real gateway integration (PayMongo /
Xendit-style redirect checkout). To go live:

1. Replace `GcashTopUpService` with a real implementation of `IGcashTopUpService`
   (create a real checkout session, return its redirect URL, and complete the
   session from the provider's **webhook** instead of an in-app OTP confirm).
2. Add webhook authentication and keep `Confirm` idempotency.
3. The controller routes, DTOs, database records and most of the frontend flow stay
   unchanged; the in-app GCash screen is replaced by opening the provider's
   checkout URL (Custom Tab / `@capacitor/browser`) and polling
   `GET /api/topup/gcash/status/{sessionId}` or receiving a deep-link callback.
