# TransitPay Android Migration Audit Report

**Date:** 2026-08-10  
**Purpose:** Comprehensive audit of the TransitPay project to identify migration blockers, reusable components, and required changes for Android deployment  
**Scope:** Passenger App, Driver App, Admin Dashboard, Backend API, Authentication, QR/Camera, Storage, Security, Environment  

---

## Executive Summary

The TransitPay project consists of three React/Vite/TypeScript web applications (passenger-app, driver-app, admin-dashboard) and a .NET 10.0 backend API with PostgreSQL. The backend is production-ready with 129 passing tests (including 5 PostgreSQL integration tests). The frontend apps are functional but require significant modifications for Android deployment.

**Migration Approaches:**
1. **WebView Wrapper (Capacitor/Cordova)** - Fastest, reuses existing code, requires native plugin replacements for camera/QR
2. **React Native** - Cross-platform, reuses React knowledge, requires complete UI rewrite
3. **Native Android (Kotlin + Jetpack Compose)** - Best performance, requires full rewrite

**Recommended:** Start with Capacitor WebView wrapper for rapid deployment, then migrate to React Native or Native Android for long-term maintainability.

---

## 1. Passenger App Audit

### Current State
- **Framework:** React 19.2 + TypeScript + Vite + Tailwind CSS
- **Screens:** Splash, Welcome, Login, Register, Forgot Password, OTP, Home, Wallet, Top-up, QR Display, Profile, Discounts, Trip Planning, Transaction History
- **State Management:** React hooks (useState, useEffect)
- **Storage:** localStorage for tokens and user data

### Critical Issues for Android

#### 1.1 Browser Dependencies
- **qrcode.react** - Generates QR codes as SVG/Canvas elements. Works in WebView but requires native replacement for better performance.
- **localStorage** - Not secure for storing JWT tokens on mobile. Must migrate to:
  - **Android Keystore** (Native)
  - **SecureStorage** (Capacitor plugin)
  - **AsyncStorage with encryption** (React Native)

#### 1.2 Storage Security
```typescript
// CURRENT (insecure for mobile)
localStorage.setItem('transitpay_token', token);
localStorage.setItem('transitpay_refresh_token', refreshToken);

// REQUIRED: Use secure storage
import { SecureStorage } from '@capacitor/secure-storage';
await SecureStorage.set({ key: 'token', value: token });
```

#### 1.3 Network Layer
- Uses standard `fetch` API - works in WebView but lacks:
  - Automatic token refresh
  - Request retry logic
  - Offline queue
  - Certificate pinning

#### 1.4 UI/UX Considerations
- **Tailwind CSS** - Works in WebView via Capacitor, but may have performance issues
- **Responsive design** - Currently uses mobile-first CSS, suitable for Android
- **Touch targets** - Need to verify minimum 48x48dp for accessibility
- **Safe area insets** - Need to handle notches and system bars

### Reusable Components
- ✅ API service layer (`lib/api.ts`) - Can be reused with minor modifications
- ✅ Auth service logic (`lib/auth.ts`) - Business logic is reusable
- ✅ QR display logic (`lib/payment.ts`) - Can be reused with native QR library
- ✅ Wallet service (`lib/wallet.ts`) - Business logic is reusable
- ✅ Trip plan service (`lib/tripPlan.ts`) - Business logic is reusable
- ✅ Discount service (`lib/discount.ts`) - Business logic is reusable
- ✅ Card service (`lib/card.ts`) - Business logic is reusable

---

## 2. Driver App Audit

### Current State
- **Framework:** React 19.2 + TypeScript + Vite + Tailwind CSS
- **Screens:** Login, Home, Active Trip, QR Scanner, Scan Result, Trip History, Profile
- **State Management:** React hooks

### Critical Issues for Android

#### 2.1 Camera/QR Scanner (BLOCKER)
```typescript
// CURRENT: Uses html5-qrcode (browser-only)
import { Html5Qrcode } from 'html5-qrcode';
const scanner = new Html5Qrcode('qr-reader');
await scanner.start({ facingMode: 'environment' }, ...);
```

**Problem:** `html5-qrcode` relies on `navigator.mediaDevices.getUserMedia()` which is NOT available in:
- Android WebView (without complex configuration)
- Capacitor/Cordova without plugins
- Native Android apps

**Required Solutions:**
1. **Capacitor:** Use `@capacitor-community/camera-preview` or `@capacitor/qr-scanner`
2. **React Native:** Use `react-native-camera` or `expo-camera`
3. **Native Android:** Use ML Kit Barcode Scanning API

#### 2.2 Session Storage
```typescript
// CURRENT: Uses sessionStorage (browser-only)
sessionStorage.setItem('lastReceipt', JSON.stringify(result.data));
```

**Required:** Replace with:
- **Capacitor:** `@capacitor/preferences` or in-memory state
- **React Native:** `AsyncStorage` or in-memory state
- **Native:** ViewModel + SavedStateHandle

#### 2.3 Real-time Updates
- No WebSocket or push notification support
- Driver must manually refresh to see new scan results
- **Required:** Implement Firebase Cloud Messaging (FCM) or WebSocket

### Reusable Components
- ✅ Trip service logic (`lib/tripService.ts`)
- ✅ API service layer (`lib/api.ts`)
- ✅ Auth service logic (`lib/auth.ts`)
- ✅ UI components (with modifications for native camera)

---

## 3. Admin Dashboard Audit

### Current State
- **Framework:** React 19.2 + TypeScript + Vite + Tailwind CSS
- **Purpose:** Manage terminals, fare rules, users, discounts, view transactions
- **Access:** Admin role only

### Issues for Android
- **Lower priority** - Admin dashboard is typically used on desktop
- **Recommendation:** Keep as web app or build separate tablet-optimized web app
- **No mobile-specific features required**

### Reusable Components
- ✅ API service layer
- ✅ Auth service logic
- ✅ Business logic for fare rules, terminals, users

---

## 4. Backend API Audit

### Current State
- **Framework:** .NET 10.0
- **Database:** PostgreSQL (production-ready)
- **Authentication:** JWT with refresh tokens
- **Testing:** 129 tests (124 unit + 5 PostgreSQL integration)
- **Architecture:** Clean separation of concerns (Controllers → Services → Repositories)

### Production Readiness

#### 4.1 API Endpoints (Verified)
```
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/validate

GET  /api/cards/me
GET  /api/wallet/{cardId}
POST /api/wallet/topup

GET  /api/payment/qr/{cardId}
POST /api/payment/process-conductor
POST /api/payment/scan-physical

POST /api/trip-plan
GET  /api/trip-plan/{planId}
GET  /api/trip-plan/active

POST /api/Trip/start
POST /api/Trip/{tripId}/end
GET  /api/Trip/active
GET  /api/Trip/history

POST /api/admin/fare-rules
GET  /api/admin/fare-rules
POST /api/admin/terminals
GET  /api/admin/terminals

POST /api/discount/apply
GET  /api/discount/status
```

#### 4.2 Security
- ✅ JWT authentication with refresh tokens
- ✅ Role-based access control (Passenger, Driver, Admin)
- ✅ Password hashing (ASP.NET Core Identity)
- ✅ Rate limiting on auth endpoints
- ✅ HMAC-SHA256 QR code signatures
- ✅ SQL injection protection (EF Core parameterized queries)
- ✅ CORS configured (currently localhost only)

#### 4.3 Required Changes for Production
1. **CORS Configuration**
   ```csharp
   // CURRENT: localhost only
   // REQUIRED: Add Android app origin
   "https://yourdomain.com"
   "capacitor://localhost" // Capacitor
   ```

2. **HTTPS Enforcement**
   - Already implemented (`UseHttpsRedirection`)
   - Requires valid SSL certificate in production

3. **Environment Variables**
   ```
   DB_PASSWORD=production-password
   JWT_KEY=production-secret-key-64-chars
   ADMIN_BOOTSTRAP_PASSWORD=secure-admin-password
   ```

4. **API Versioning**
   - Add `/api/v1/` prefix for future-proofing
   - Allows backward compatibility during app updates

5. **Rate Limiting**
   - Currently per-IP
   - Mobile users on shared NATs may hit limits
   - Consider per-user rate limiting after authentication

### No Changes Required
- ✅ Database schema is production-ready
- ✅ Transaction handling is correct (verified with PostgreSQL tests)
- ✅ TRN generation is atomic and unique
- ✅ QR cryptographic flow is secure and working
- ✅ Error handling is comprehensive

---

## 5. Authentication & Authorization Audit

### Current Flow
1. User registers with mobile number, password, name
2. Login returns JWT access token (15min) + refresh token (7 days)
3. Tokens stored in localStorage
4. Refresh token endpoint exchanges old refresh token for new access token
5. Logout revokes refresh token server-side

### Mobile-Specific Issues

#### 5.1 Token Storage (CRITICAL)
```typescript
// CURRENT: Insecure for mobile
localStorage.setItem('transitpay_token', token);

// REQUIRED: Secure storage
// Capacitor
import { SecureStorage } from '@capacitor/secure-storage';
await SecureStorage.set({ key: 'token', value: token });

// React Native
import * as SecureStore from 'expo-secure-store';
await SecureStore.setItemAsync('token', token);

// Native Android
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
```

#### 5.2 Biometric Authentication
- **Not implemented** - Optional but recommended for mobile
- **Capacitor:** `@capacitor/local-authentication`
- **React Native:** `expo-local-authentication`
- **Native:** BiometricManager API

#### 5.3 Token Refresh
- Current implementation is manual (app must call refresh endpoint)
- **Required:** Automatic refresh before expiration
- **Required:** Handle refresh failures gracefully (force logout)

---

## 6. API Contracts Audit

### Current State
- All endpoints return consistent format:
  ```json
  {
    "success": boolean,
    "message": string,
    "data": object
  }
  ```
- Enums serialize as strings (e.g., "ACTIVE", "COMPLETED")
- Dates in ISO 8601 format

### Mobile-Specific Considerations
- ✅ API contract is consistent and well-documented
- ✅ Error messages are user-friendly
- ⚠️ No API versioning - add `/api/v1/` prefix
- ⚠️ No request/response logging for debugging mobile issues
- ⚠️ No pagination metadata standardization (some endpoints return `pagination` object, others don't)

### Required Changes
1. Add API versioning
2. Standardize pagination response format
3. Add request correlation IDs for debugging
4. Document API with OpenAPI/Swagger

---

## 7. QR Code & Camera Functionality Audit

### Current Implementation

#### 7.1 QR Generation (Passenger App)
```typescript
// CURRENT: Uses qrcode.react (SVG-based)
import { QRCodeSVG } from 'qrcode.react';
<QRCodeSVG value={qrData} size={256} />
```
- **Status:** Works in WebView
- **Mobile:** Replace with native QR library for better performance
  - **Capacitor:** `@capacitor-community/qr-scanner` (display only)
  - **React Native:** `react-native-qrcode-svg`
  - **Native:** `com.google.zxing:core`

#### 7.2 QR Scanning (Driver App) (BLOCKER)
```typescript
// CURRENT: Browser-only
import { Html5Qrcode } from 'html5-qrcode';
const scanner = new Html5Qrcode('qr-reader');
await scanner.start({ facingMode: 'environment' }, ...);
```

**Problem:** `html5-qrcode` uses `navigator.mediaDevices.getUserMedia()` which is NOT available in:
- Android WebView
- Capacitor/Cordova
- Native apps

**Required Solutions:**

**Option A: Capacitor**
```typescript
// Install: npm install @capacitor-community/camera-preview
import { CameraPreview } from '@capacitor-community/camera-preview';

// Start camera
await CameraPreview.start({
  position: 'rear',
  parent: 'camera-preview',
  className: 'camera-preview'
});

// Scan QR codes manually or use ML Kit
```

**Option B: React Native**
```typescript
// Install: expo install expo-camera expo-barcode-scanner
import { Camera } from 'expo-camera';
import { BarCodeScanner } from 'expo-barcode-scanner';

<BarCodeScanner
  onBarCodeScanned={handleScannedQR}
  type={Camera.Constants.Type.back}
/>
```

**Option C: Native Android (Kotlin)**
```kotlin
// Use ML Kit Barcode Scanning API
val options = BarcodeScannerOptions.Builder()
    .setBarcodeFormats(Barcode.FORMAT_QR_CODE)
    .build()
val scanner = BarcodeScanning.getClient(options)
```

#### 7.3 QR Data Format
```json
{
  "CardId": 123,
  "Token": "base64url-encoded-random-token"
}
```
- **Status:** Secure and minimal
- ✅ No sensitive data in QR payload
- ✅ HMAC-SHA256 signature prevents tampering
- ✅ Token-based (not card number) for privacy

---

## 8. Navigation & UI Audit

### Current State
- **Navigation:** Screen-based state machine (`useState` for current screen)
- **UI Library:** Custom components with Tailwind CSS
- **Icons:** Lucide React (SVG-based)
- **Fonts:** Poppins (Google Fonts)

### Mobile-Specific Issues

#### 8.1 Navigation
```typescript
// CURRENT: Simple state-based navigation
const [screen, setScreen] = useState<Screen>('splash');
<button onClick={() => setScreen('home')}>Home</button>
```
- **Works in WebView** but lacks:
  - Deep linking
  - Back button handling
  - Navigation history

**Required:**
- **Capacitor:** Use `@capacitor/app` for deep linking and back button
- **React Native:** Use `@react-navigation/native`
- **Native:** Use Android Navigation Component

#### 8.2 Safe Area Handling
- **Current:** No safe area insets handling
- **Required:** Handle notches, system bars, and gesture navigation
  - **Capacitor:** `@capacitor/status-bar` + `@capacitor/safe-area`
  - **React Native:** `react-native-safe-area-context`
  - **Native:** `WindowInsetsControllerCompat`

#### 8.3 Performance
- **Tailwind CSS:** Works in WebView but may cause jank on low-end devices
- **Solution:** Use `tailwindcss-no-scroll` or migrate to native styles
- **Animations:** CSS animations may be slow in WebView
  - **Solution:** Use native animations (Lottie, Reanimated)

---

## 9. Browser-Specific Dependencies

### Dependencies to Replace

| Package | Current Use | Android Replacement |
|---------|-------------|---------------------|
| `qrcode.react` | QR generation | Native QR library or keep in WebView |
| `html5-qrcode` | QR scanning | ML Kit Barcode Scanning (native) or Capacitor plugin |
| `localStorage` | Token storage | SecureStorage (Capacitor) / Keystore (Native) |
| `sessionStorage` | Receipt storage | In-memory state or AsyncStorage |
| `window.location` | Navigation | Deep linking plugin |
| `document.cookie` | Not used | N/A |
| `fetch` | HTTP requests | Keep (works in WebView) or use Axios |
| `lucide-react` | Icons | Keep (SVG) or use native icons |

### Dependencies to Keep
- ✅ React (via Capacitor/React Native)
- ✅ TypeScript
- ✅ Tailwind CSS (via Capacitor)
- ✅ Axios or fetch (works in WebView)

---

## 10. Environment & Configuration

### Current State
```env
# passenger-app/.env
VITE_APP_NAME=TransitPay
VITE_APP_ENV=development
VITE_API_URL=http://localhost:5132

# driver-app/.env
VITE_APP_NAME=TransitPay Driver
VITE_APP_ENV=development
VITE_API_URL=
```

### Required Changes

#### 10.1 Environment Configuration
- **Current:** Vite environment variables (`import.meta.env`)
- **Required:** Native environment configuration
  - **Capacitor:** Keep Vite env vars + `capacitor.config.ts`
  - **React Native:** `react-native-config` or `expo-constants`
  - **Native:** `BuildConfig` (Kotlin) or `strings.xml`

#### 10.2 API URL Configuration
```typescript
// CURRENT
const API_BASE = import.meta.env.VITE_API_URL || '';

// REQUIRED: Support multiple environments
const API_BASE = {
  development: 'http://localhost:5132',
  staging: 'https://staging-api.transitpay.ph',
  production: 'https://api.transitpay.ph'
}[import.meta.env.VITE_APP_ENV || 'development'];
```

#### 10.3 Build Configuration
- **Current:** Vite build outputs to `dist/`
- **Capacitor:** Copy `dist/` to `android/app/src/main/assets/public/`
- **React Native:** Metro bundler
- **Native:** N/A (full rewrite)

---

## 11. Security Audit

### Current Security Posture

#### 11.1 Backend (✅ Strong)
- JWT authentication with refresh tokens
- Password hashing (ASP.NET Core Identity)
- Rate limiting on auth endpoints
- HMAC-SHA256 QR signatures
- SQL injection protection (EF Core)
- CORS configured
- HTTPS enforcement

#### 11.2 Frontend (⚠️ Weaknesses)

**Critical Issues:**
1. **Token Storage in localStorage**
   - Vulnerable to XSS attacks
   - Accessible to JavaScript
   - **Fix:** Use secure storage (Keystore/SecureStorage)

2. **No Certificate Pinning**
   - Vulnerable to man-in-the-middle attacks
   - **Fix:** Implement certificate pinning
     - **Capacitor:** `@capacitor-community/http`
     - **React Native:** `react-native-ssl-pinning`
     - **Native:** `CertificatePinner` (OkHttp)

3. **No Request Signing**
   - API requests are not signed
   - **Fix:** Add HMAC request signing (optional but recommended)

4. **No Offline Data Protection**
   - Sensitive data stored in plain text
   - **Fix:** Encrypt sensitive data before storage

### Security Recommendations
1. **Immediate:**
   - Migrate from localStorage to secure storage
   - Implement certificate pinning
   - Add request timeout and retry logic

2. **Short-term:**
   - Implement biometric authentication
   - Add device fingerprinting
   - Implement request signing

3. **Long-term:**
   - Add anomaly detection (unusual login locations)
   - Implement app attestation (Play Integrity API)
   - Add jailbreak/root detection

---

## 12. Additional Considerations

### 12.1 Offline Support
- **Current:** No offline support
- **Required:**
  - Cache recent transactions
  - Queue payments when offline
  - Sync when connection restored
  - **Capacitor:** `@capacitor/network` + `@capacitor/preferences`
  - **React Native:** `@react-native-community/netinfo` + `AsyncStorage`
  - **Native:** WorkManager + Room database

### 12.2 Push Notifications
- **Current:** No push notifications
- **Required:**
  - Payment confirmations
  - Trip updates
  - Discount approvals
  - **Solution:** Firebase Cloud Messaging (FCM)
    - **Capacitor:** `@capacitor/push-notifications`
    - **React Native:** `expo-notifications` or `react-native-push-notification`
    - **Native:** Firebase Messaging SDK

### 12.3 Background Tasks
- **Current:** No background tasks
- **Required:**
  - Token refresh in background
  - Sync data in background
  - **Capacitor:** `@capacitor/background-task`
  - **React Native:** `expo-task-manager`
  - **Native:** WorkManager

### 12.4 App Store Requirements
- **Privacy Policy:** Required
- **Terms of Service:** Required
- **Permissions:**
  - Camera (for QR scanning)
  - Internet (for API calls)
  - Vibration (optional, for scan feedback)
- **Target SDK:** Android 14 (API 34) or higher
- **Min SDK:** Android 8.0 (API 26) or higher

### 12.5 Performance
- **WebView Performance:**
  - Initial load time: ~2-3 seconds
  - Memory usage: ~150-200MB
  - **Optimization:** Lazy load screens, compress assets
- **Native Performance:**
  - Initial load time: <1 second
  - Memory usage: ~80-120MB
  - Better battery life

---

## 13. Recommended Migration Plan

### Phase 1: Capacitor WebView Wrapper (2-4 weeks)
**Goal:** Rapid Android deployment with minimal code changes

**Tasks:**
1. Set up Capacitor project
2. Replace `html5-qrcode` with `@capacitor-community/camera-preview` + ML Kit
3. Replace `localStorage` with `@capacitor/secure-storage`
4. Replace `sessionStorage` with in-memory state
5. Add Capacitor plugins for:
   - Status bar (`@capacitor/status-bar`)
   - Safe area (`@capacitor/safe-area`)
   - Network detection (`@capacitor/network`)
   - Push notifications (`@capacitor/push-notifications`)
   - Deep linking (`@capacitor/app`)
6. Configure Android build (min SDK 26, target SDK 34)
7. Test on real devices
8. Deploy to Google Play Store (internal testing)

**Pros:**
- Fastest time to market (2-4 weeks)
- Reuses 80% of existing code
- Easy maintenance

**Cons:**
- Performance not as good as native
- Limited access to native features
- WebView vulnerabilities

### Phase 2: React Native Migration (3-6 months)
**Goal:** Cross-platform app with better performance

**Tasks:**
1. Set up React Native project (Expo or bare workflow)
2. Migrate Passenger App screens to React Native
3. Migrate Driver App screens to React Native
4. Implement native QR scanning (`expo-camera` + `expo-barcode-scanner`)
5. Implement secure storage (`expo-secure-store`)
6. Implement navigation (`@react-navigation/native`)
7. Implement push notifications (`expo-notifications`)
8. Test on iOS and Android
9. Deploy to app stores

**Pros:**
- Cross-platform (iOS + Android)
- Better performance than WebView
- Large ecosystem

**Cons:**
- Requires complete UI rewrite
- Learning curve for team
- Longer development time

### Phase 3: Native Android (6-12 months)
**Goal:** Best performance and user experience

**Tasks:**
1. Set up Android project (Kotlin + Jetpack Compose)
2. Implement MVVM architecture
3. Implement native QR scanning (ML Kit)
4. Implement secure storage (Keystore)
5. Implement navigation (Navigation Component)
6. Implement push notifications (FCM)
7. Implement offline support (Room + WorkManager)
8. Test on multiple devices
9. Deploy to Google Play Store

**Pros:**
- Best performance
- Best user experience
- Full access to native APIs

**Cons:**
- Requires complete rewrite
- Separate iOS app needed
- Longest development time

---

## 14. Reusable Assets

### Code (80% Reusable)
- ✅ API service layer (`lib/api.ts`)
- ✅ Auth business logic (`lib/auth.ts`)
- ✅ QR generation logic (backend endpoint)
- ✅ Wallet service logic (`lib/wallet.ts`)
- ✅ Trip plan service logic (`lib/tripPlan.ts`)
- ✅ Discount service logic (`lib/discount.ts`)
- ✅ Card service logic (`lib/card.ts`)
- ✅ Backend API (100% reusable)
- ✅ Database schema (100% reusable)

### Design (60% Reusable)
- ✅ Color scheme and branding
- ✅ UI component logic (buttons, inputs, cards)
- ⚠️ Tailwind CSS classes need conversion to native styles
- ✅ Icon set (Lucide icons available for React Native)

### Documentation (100% Reusable)
- ✅ API documentation (from code)
- ✅ Database schema
- ✅ Business logic documentation

---

## 15. Cost & Timeline Estimates

### Phase 1: Capacitor (Recommended for MVP)
- **Timeline:** 2-4 weeks
- **Team:** 1-2 developers
- **Cost:** Low (minimal code changes)
- **Risk:** Low

### Phase 2: React Native
- **Timeline:** 3-6 months
- **Team:** 2-3 developers
- **Cost:** Medium (complete UI rewrite)
- **Risk:** Medium

### Phase 3: Native Android
- **Timeline:** 6-12 months
- **Team:** 3-4 developers (Android + iOS)
- **Cost:** High (two separate apps)
- **Risk:** Low (but long timeline)

---

## 16. Critical Blockers

### Must Fix Before Android Deployment
1. **QR Scanner:** Replace `html5-qrcode` with native camera plugin (BLOCKER)
2. **Token Storage:** Replace `localStorage` with secure storage (BLOCKER)
3. **CORS:** Update to allow Android app origin (BLOCKER)
4. **HTTPS:** Deploy API to production with valid SSL certificate (BLOCKER)
5. **Environment:** Add production API URL (REQUIRED)

### Should Fix Before Production
6. **Push Notifications:** Implement FCM for real-time updates
7. **Offline Support:** Cache data and queue payments
8. **Certificate Pinning:** Prevent MITM attacks
9. **Biometric Auth:** Add fingerprint/FaceID login
10. **Deep Linking:** Handle app links from notifications

### Nice to Have
11. **Animations:** Replace CSS animations with native animations
12. **Analytics:** Add crash reporting and analytics (Firebase)
13. **App Updates:** Implement forced updates for critical fixes

---

## 17. Testing Requirements

### Current Test Coverage
- ✅ Backend: 129 tests (124 unit + 5 PostgreSQL integration)
- ❌ Frontend: No automated tests
- ❌ E2E: No E2E tests

### Required for Android
1. **Unit Tests:**
   - React Native: Jest
   - Native: JUnit + MockK

2. **Integration Tests:**
   - React Native: Detox
   - Native: Espresso

3. **E2E Tests:**
   - Backend: Keep existing tests
   - Frontend: Add Playwright (web) or Appium (mobile)

---

## 18. Conclusion

The TransitPay backend is production-ready and requires no changes for Android deployment. The frontend apps require significant modifications for Android, primarily:

1. **Replace browser-only dependencies** (`html5-qrcode`, `localStorage`, `sessionStorage`)
2. **Implement secure storage** for tokens
3. **Add native camera/QR scanning**
4. **Configure CORS** for Android app origin
5. **Deploy API to production** with HTTPS

**Recommended Approach:** Start with Capacitor WebView wrapper for rapid deployment (2-4 weeks), then migrate to React Native or Native Android for long-term maintainability.

**Reusability:** 80% of frontend business logic and 100% of backend code can be reused in the Android app.

**Risk:** Low to Medium, depending on chosen migration approach.

---

## Appendix A: File Inventory

### Backend
- `TransitPay.API/` - Main API project
- `TransitPay.API.Tests/` - Test project
- `database/` - Database scripts

### Frontend
- `passenger-app/` - Passenger React app
- `driver-app/` - Driver React app
- `admin-dashboard/` - Admin React app

### Key Files to Modify
1. `passenger-app/src/lib/auth.ts` - Replace localStorage
2. `passenger-app/src/lib/payment.ts` - Replace qrcode.react
3. `driver-app/src/DriverApp.tsx` - Replace html5-qrcode
4. `driver-app/src/lib/tripService.ts` - Replace sessionStorage
5. `TransitPay.API/Program.cs` - Update CORS

---

## Appendix B: Dependencies to Add/Remove

### Remove
```json
{
  "qrcode.react": "^4.2.0", // Replace with native QR library
  "html5-qrcode": "^2.3.8"  // Replace with native camera plugin
}
```

### Add (Capacitor)
```json
{
  "@capacitor/android": "^6.0.0",
  "@capacitor/app": "^6.0.0",
  "@capacitor/camera": "^6.0.0",
  "@capacitor/network": "^6.0.0",
  "@capacitor/push-notifications": "^6.0.0",
  "@capacitor/secure-storage": "^6.0.0",
  "@capacitor/status-bar": "^6.0.0",
  "@capacitor-community/camera-preview": "^6.0.0"
}
```

### Add (React Native)
```json
{
  "expo-camera": "^14.0.0",
  "expo-barcode-scanner": "^12.0.0",
  "expo-secure-store": "^13.0.0",
  "expo-notifications": "^14.0.0",
  "expo-local-authentication": "^14.0.0",
  "@react-navigation/native": "^6.0.0",
  "react-native-screens": "^4.0.0",
  "react-native-safe-area-context": "^4.0.0"
}
```

---

**End of Audit Report**