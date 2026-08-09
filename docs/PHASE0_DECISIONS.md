# Phase 0 — Canonical Decisions

**Date:** 2025-08-08
**Status:** Approved
**Scope:** Decisions that block all subsequent audit-fix phases. No code changes are made in this phase — only the canonical direction is locked in.

---

## Decision 1: Canonical Payment Flow — **Conductor / Trip-Plan Based**

### Decision

The **conductor/trip-plan-based** payment flow is the canonical flow. The **session-based** flow is abandoned.

### Rationale / Evidence

1. **The conductor flow is fully wired end-to-end and works today:**
   - Passenger: `PassengerApp.tsx` → `tripPlanService.createTripPlan()` → `POST /api/trip-plan` → `TripPlanService.CreateTripPlanAsync` (fully implemented; `TripPlanController` exists).
   - Passenger: `qrService.getQR()` → `GET /api/payment/qr/{cardId}` (exists in `PaymentController`).
   - Driver: `DriverApp.tsx` → `cardService.processConductorPayment()` → `POST /api/payment/process-conductor` (exists; fully implemented in `PaymentService.ProcessConductorPaymentAsync`).
   - The conductor core (`ProcessConductorPaymentCoreAsync`) reads the destination from the **active TripPlan**, validates the route, locks the fare, applies the discount, deducts the wallet, creates the transaction, and marks the plan "Used" — all atomically with idempotency protection.

2. **The session-based flow is broken and unreachable:**
   - `PaymentSessionService` / `IPaymentSessionService` are registered in `Program.cs` but **no controller exposes them** — no `POST /api/payment/session` endpoint (Issue #6).
   - `ProcessQRPaymentAsync` (the session-consuming method) is **never called by any controller** — dead code (Issue #14).
   - `driver-app/src/lib/cards.ts` `scanQR()` targets `POST /api/payment/scan` which **does not exist** — and `scanQR()` is **never called anywhere** in the app. Only `processConductorPayment()` is used.
   - The `TripPlan.UsedPaymentSessionId` (int?) FK to `PaymentSession.PaymentSessionId` (Guid) mismatch (Issue #5) belongs to the broken session flow.

3. **The TripPlan is the source of truth.** The conductor flow requires the passenger to pre-select a route (creating a TripPlan), which is the natural UX for a fixed-route transit system. The driver's active trip + the passenger's TripPlan together define origin/destination.

### Implications for Related Issues

| Issue | Implication |
|-------|-------------|
| **#1** (`scanQR` 404) | Remove the dead `scanQR()` method from `driver-app/src/lib/cards.ts` (and its unused `ScanQRResponse` interface). |
| **#6** (missing session endpoint) | Do **NOT** build `POST /api/payment/session`. The session flow is abandoned. |
| **#14** (`ProcessQRPaymentAsync` dead code) | Remove `ProcessQRPaymentAsync` from `IPaymentService`/`PaymentService` and the private `ProcessSessionPaymentAsync` helper. |
| **#5** (`UsedPaymentSessionId` Guid/int mismatch) | Remove the `UsedPaymentSessionId` column from `TripPlan` and the dead `MarkTripPlanAsUsedAsync(int, int)` method. The conductor flow marks plans "Used" directly via `planId` without needing a session ID. |

---

## Decision 2: Enum Serialization Contract — **Strings Win**

### Decision

Keep `JsonStringEnumConverter` — enums are serialized as **strings** across the API. All three frontends must consume string enum values.

### Rationale / Evidence

1. `Program.cs` lines 28-30 contain an explicit, intentional comment: *"Serialize enums as strings (e.g., "PAYMENT" instead of 0) so the frontend can call .toLowerCase() on transaction types."* The `JsonStringEnumConverter` is deliberately configured.
2. The majority of enums already work as strings in the frontends: `CardStatus` ("ACTIVE"), `TransactionType` ("PAYMENT"), `TransactionStatus` ("COMPLETED"), `PassengerType` ("Passenger"), `RoleName`, `VehicleType`, `PaymentSessionStatus`.
3. The mismatches are isolated to a few places where frontends were written defensively/incorrectly: `DiscountApplication.status` (number), `Trip.tripStatus` (number union), `PassengerDiscount.status` (number).

### Implications for Related Issues

| Issue | Implication |
|-------|-------------|
| **#2** (`DiscountApplication.status`) | Update `passenger-app/src/lib/discount.ts`: `status` → `string`; `DISCOUNT_STATUS` keyed by status names; `getDiscountStatusName` accepts string. |
| **#3** (`Trip.tripStatus`) | Update `driver-app/src/lib/tripService.ts`: drop the `number` union from `tripStatus`; remove numeric dead-code checks in `DriverApp.tsx` (lines 77, 183, 213, 238, 799). |
| **#2** (`getCurrentDiscountType`) | Update `passenger-app/src/lib/discount.ts` line 127: `status !== 0` → `status !== 'Active'`. |

---

## Downstream Phases

These decisions unblock the following phases. Each phase will implement its code changes against the canonical direction locked in above:

- **Phase 1:** Payment flow cleanup — remove `scanQR()`, `ProcessQRPaymentAsync`, `UsedPaymentSessionId`, `MarkTripPlanAsUsedAsync`.
- **Phase 2:** Enum frontend fixes — string statuses in passenger and driver apps.
- **Subsequent phases:** Remaining audit issues (admin endpoints, naming, pagination, etc.).