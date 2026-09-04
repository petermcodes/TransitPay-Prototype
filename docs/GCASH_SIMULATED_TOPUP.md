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

### Resuming an interrupted payment

If the app is closed or crashes mid-checkout, the session stays `PENDING`
server-side. The next time the passenger opens the Top Up screen, the app queries
`GET /api/topup/gcash/active/{cardId}` and shows a **resume banner** with
*Continue payment* (re-opens the checkout for the original session, keeping the
original TRN) and *Cancel payment* (voids the session → `CANCELLED`).

Two invariants keep this clean:

- **Lazy expiry** — a session that expired while the app was closed is marked
  `EXPIRED` (transaction → `CANCELLED`) on the next active-session lookup, so it is
  never offered for resume.
- **Single active session per card** — starting a fresh top-up auto-cancels any
  still-open session, so at most one checkout per card is ever open.

Wallet stats on the My Wallet screen ("Total Top Up" / "Total Spent") count only
`COMPLETED` transactions, so abandoned or failed checkouts never inflate the totals
(see `computeWalletStats` in `passenger-android/src/lib/wallet.ts` and
`passenger-app/src/lib/wallet.ts`).

## API endpoints (`TopUpController`, passenger JWT required, ownership enforced)

| Method | Route | Body | Notes |
|---|---|---|---|
| POST | `/api/topup/gcash/initiate` | `{ cardId, amount }` | Validates amount (₱1–₱10,000) and card ownership; returns session + TRN |
| POST | `/api/topup/gcash/confirm` | `{ sessionId, otp }` | Sandbox OTP is `123456`; 3 wrong attempts → `FAILED` |
| POST | `/api/topup/gcash/cancel` | `{ sessionId }` | Voids a pending session |
| GET | `/api/topup/gcash/status/{sessionId}` | — | Status polling; lazily expires stale sessions |
| GET | `/api/topup/gcash/active/{cardId}` | — | Open session for a card — used to **resume** an interrupted payment; null data when none |

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
