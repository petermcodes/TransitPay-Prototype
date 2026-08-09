# P0 & P1 Audit Report — TransitPay Prototype

**Date:** 8/9/2026
**Status:** ✅ All P0 and P1 items resolved

---

## P0 — Critical (Resolved)

### P0-1: Station/Terminal Naming Inconsistency — FIXED ✅
**Original Issue:** Backend uses `Terminal` model/controller but frontends used `Station` naming in 64+ references.

**Changes:**
- `admin-dashboard/src/lib/admin.ts` — Renamed all `Station*` to `Terminal*` types and functions
- `admin-dashboard/src/views/TripsView.tsx` — Updated to `Terminal` naming
- `admin-dashboard/src/components/TripModal.tsx` — Updated to `Terminal` naming
- `admin-dashboard/src/components/TerminalModal.tsx` — Created (renamed from StationModal.tsx)
- `admin-dashboard/src/components/StationModal.tsx` — Deleted (dead code)
- `admin-dashboard/src/AdminApp.tsx` — Updated comments and references
- `driver-app/src/lib/tripService.ts` — Renamed `getStations` → `getTerminals`, fixed camelCase mapping
- `driver-app/src/DriverApp.tsx` — Updated `stations` → `terminals`, `loadStations` → `loadTerminals`
- `TransitPay.API.Tests/TripServiceTests.cs` — Rewrote with `Terminal` model
- `TransitPay.API.Tests/SchemaUniquenessMetadataTests.cs` — Removed `Station` references

**Verification:** ✅ admin-dashboard builds, ✅ driver-app builds, ✅ backend builds

### P0-2: Driver App Terminal Mapping Bug — FIXED ✅
**Original Issue:** Driver app assumed PascalCase response property names (`TerminalId`, `TerminalName`) but API returns camelCase (`terminalId`, `terminalName`).

**Changes:**
- `driver-app/src/lib/tripService.ts` — Updated mapping to use camelCase property names matching actual API response

**Verification:** ✅ driver-app builds

### P0-3: Trip Interface API Contract — VERIFIED ✅
**Original Issue:** Potential mismatch in Trip interface between frontend and backend.

**Changes:** Verified the Trip interface already matches the backend response format. No code change required.

---

## P1 — High (Resolved)

### P1-4: PaymentSession Guid vs int Mismatch — FIXED ✅
**Original Issue:** `PaymentSessionId` was `Guid` while all other IDs were `int`, causing type confusion.

**Changes:** Since PaymentSessionService was dead code (see P1-5), the entire dead code path was removed, eliminating the Guid/int inconsistency entirely.

### P1-5: PaymentSessionService Dead Code — FIXED ✅
**Original Issue:** PaymentSessionService existed but was not exposed via endpoints, confusing developers about the canonical payment flow.

**Canonical Flow Decision:** ✅ **Conductor/Trip-Plan-based** flow is canonical. The session-based flow has been removed.

**Files Deleted (7):**
1. `TransitPay.API/Services/PaymentSessionService.cs`
2. `TransitPay.API/Interfaces/IPaymentSessionService.cs`
3. `TransitPay.API/Models/PaymentSession.cs`
4. `TransitPay.API/Enums/PaymentSessionStatus.cs`
5. `TransitPay.API/DTOs/Payment/CreatePaymentSessionRequest.cs`
6. `TransitPay.API/DTOs/Payment/PaymentSessionResponse.cs`
7. `TransitPay.API.Tests/PaymentServiceTests.cs`

**Files Modified (6):**
1. `TransitPay.API/Program.cs` — Removed DI registration
2. `TransitPay.API/Data/TransitPayDbContext.cs` — Removed DbSet, table mapping, relationships
3. `TransitPay.API/Models/Transaction.cs` — Removed `PaymentSessionId`, `PaymentSession` navigation
4. `TransitPay.API/Controllers/PaymentController.cs` — Updated comments
5. `TransitPay.API/DTOs/Payment/PaymentResponse.cs` — Removed `PaymentSessionId`
6. `TransitPay.API.Tests/PanDetectionTests.cs` — Removed PaymentSession references

### P1-6: Auth Service Tests — FIXED ✅
**Original Issue:** No tests existed for `AuthService` (login, registration, token refresh).

**New File:** `TransitPay.API.Tests/AuthServiceTests.cs` — 11 tests

**Coverage:**
- **Registration (4 tests):**
  - `RegisterAsync_ValidInput_ReturnsSuccessWithPassengerRole`
  - `RegisterAsync_DuplicateUsername_ReturnsFailure`
  - `RegisterAsync_DuplicateMobileNumber_ReturnsFailure`
  - `RegisterAsync_MissingPassengerRole_ReturnsFailure`

- **Login (4 tests):**
  - `LoginAsync_ValidCredentials_ReturnsSuccessWithTokens`
  - `LoginAsync_InvalidPassword_ReturnsFailure`
  - `LoginAsync_NonExistentUser_ReturnsFailure`
  - `LoginAsync_MultipleFailedAttempts_ReturnsFailure`

- **Token Refresh (3 tests):**
  - `RefreshTokenAsync_ValidToken_ReturnsNewRefreshToken`
  - `RefreshTokenAsync_InvalidToken_ReturnsFailure`
  - `RefreshTokenAsync_ExpiredToken_ReturnsFailure`

**Test infrastructure:**
- Sets up `JWT_KEY` environment variable for token signing
- Uses in-memory database
- Seeds roles for test scenarios
- Uses production-compliant password that passes the password policy

---

## Test Results

### Before P0/P1 Work
- Total: 78 tests (prior to PaymentSession dead code removal)

### After P0/P1 Work
- **Total: 74 tests**
- **Passing: 74 ✅**
- **Failing: 0**

### Password Policy Test Fix
The 3 pre-existing `PasswordPolicyTests` failures were caused by stale tests asserting the **old** stricter password policy (min 12 chars + uppercase/lowercase required). The current policy intentionally requires only:
- Minimum length: **8** characters
- At least one **digit**
- At least one **special character**

The tests were updated to reflect this relaxed policy (Option A):
- `Validate_ReturnsInvalid_WhenPasswordTooShort` — Now tests passwords under 8 chars (e.g., `"Short1!"`, `"123456!"`)
- `Validate_ReturnsValid_WhenPasswordIsAllUppercase` — Confirms `"UPPERCASE123!"` is valid
- `Validate_ReturnsValid_WhenPasswordIsAllLowercase` — Confirms `"alllowercase123!"` is valid
- All other password policy tests (common passwords, personal info, digit/special requirements, null/whitespace) unchanged and passing

### Build Verification
- ✅ `TransitPay.API` — Build succeeds
- ✅ `admin-dashboard` — TypeScript build succeeds
- ✅ `driver-app` — TypeScript build succeeds
- ✅ `TransitPay.API.Tests` — Compiles successfully

---

## Canonical Decisions Documented

### Decision 1: Canonical Payment Flow
**Decision:** ✅ **Conductor/Trip-Plan-based flow** is the single canonical payment flow.

**Rationale:**
- `ProcessConductorPayment` and `ScanPhysicalCard` endpoints are fully implemented and tested
- TripPlan model supports the end-to-end journey (passenger creates plan → driver scans QR → payment processed)
- Passenger app uses trip plans for QR-based boarding
- Session-based flow was never exposed via endpoints

**Removed:** All PaymentSession dead code (service, interface, model, enums, DTOs)

### Decision 2: Enum Serialization Contract
**Decision:** ✅ **String-based enum serialization** via `JsonStringEnumConverter`

**Rationale:**
- Already in place in the codebase
- Transaction types need `.toLowerCase()` for consistency
- Frontends consume string values
- Documented as the standard contract for all enums

**Contract:** All enums serialize to their string names (e.g., `"COMPLETED"`, `"ACTIVE"`). Frontends should handle string values, not numeric values.

---

## P2 — Medium (Stabilization) Progress

### P2-7: Remove TripModal Dead Code — FIXED ✅
**Original Issue:** `TripModal.tsx` unused component in admin-dashboard.

**Changes:**
- Deleted `admin-dashboard/src/components/TripModal.tsx` (confirmed 0 imports before deletion)
- ✅ admin-dashboard build succeeds

### P2-8: Complete Test Mocks — ALREADY RESOLVED ✅
**Original Issue:** Mock implementations throwing `NotImplementedException`.

**Changes:** Search confirmed **0 results** for `NotImplementedException` in the test project. This was cleaned up when `PaymentServiceTests.cs` (which contained the incomplete mocks) was deleted during P1 dead-code removal.

### P2-9: Squash Database Migrations — FIXED ✅
**Original Issue:** ~30 migration pairs made deployment pipeline complex.

**Changes:**
- Deleted all 30 migration pairs + `TransitPayDbContextModelSnapshot.cs`
- Generated a single fresh baseline: `20260809022622_InitialCreate`
- Removed dev-era data hacks (e.g., `UPDATE trip_plans SET user_id = 12`)
- Removed orphan `payment_sessions` table creation from migration history
- ✅ `dotnet ef migrations list` → 1 migration
- ✅ Backend build succeeds
- ✅ All 74 tests pass

### P2-10: Add Integration Tests — PENDING (user decision)
**Status:** Deferred — user will decide later.

---

## Remaining Work (P2-10 + P3)
1. **P2-10:** Add integration tests (deferred by user)
2. **P3:** E2E tests, load testing, security audit, documentation
