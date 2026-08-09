# TransitPay Testing Guide

**Version:** 1.0  
**Date:** 2025-08-08  
**Purpose:** Comprehensive guide for testing TransitPay API endpoints

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Environment Setup](#environment-setup)
3. [Authentication](#authentication)
4. [API Endpoints](#api-endpoints)
5. [Testing Workflows](#testing-workflows)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software
- **dotnet SDK** 10.0 or higher
- **PowerShell** 5.1 or higher (Windows) / PowerShell Core 7+ (cross-platform)
- **Postman** or similar API testing tool (optional)

### Required Environment Variables
```powershell
$env:DB_PASSWORD = 'YourDbPassword'
$env:JWT_KEY = '32+chars+at+least+32charslong123456'
$env:ADMIN_BOOTSTRAP_PASSWORD = 'Secur3AdminP@ss!'
```

---

## Environment Setup

### 1. Start the API

```powershell
cd TransitPay-Prototype/TransitPay.API
dotnet run
```

The API will start at `http://localhost:5132` by default.

### 2. Verify API is Running

```powershell
curl http://localhost:5132/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2025-08-08T10:00:00Z"
}
```

---

## Authentication

### Register a New User

**Endpoint:** `POST /api/auth/register`

**Request:**
```json
{
  "firstName": "Test",
  "lastName": "User",
  "mobileNumber": "09171234567",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Registration successful.",
  "data": {
    "userId": 1,
    "firstName": "Test",
    "lastName": "User",
    "mobileNumber": "09171234567",
    "role": "Passenger"
  }
}
```

### Login

**Endpoint:** `POST /api/auth/login`

**Request:**
```json
{
  "mobileNumber": "09171234567",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "user": {
      "userId": 1,
      "username": "Test User",
      "firstName": "Test",
      "lastName": "User",
      "mobileNumber": "09171234567",
      "role": "Passenger"
    }
  }
}
```

**Save the token for subsequent requests:**
```powershell
$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Admin Login

**Endpoint:** `POST /api/auth/login`

**Request:**
```json
{
  "mobileNumber": "0000000000",
  "password": "Secur3AdminP@ss!"
}
```

**Response:** Same as regular login, but with admin role

**Save the admin token:**
```powershell
$adminToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

## API Endpoints

### Authentication Endpoints

#### POST /api/auth/register
**Description:** Register a new user  
**Auth Required:** No  
**Request Body:**
```json
{
  "firstName": "string (2-50 chars, required)",
  "lastName": "string (2-50 chars, required)",
  "mobileNumber": "string (09XXXXXXXXX format, required)",
  "password": "string (min 12 chars, required)"
}
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "Registration successful.",
  "data": {
    "userId": 123,
    "firstName": "Test",
    "lastName": "User",
    "mobileNumber": "09171234567",
    "role": "Passenger"
  }
}
```

**Error Response (400):**
```json
{
  "success": false,
  "message": "Validation failed.",
  "errors": ["Mobile number must be a valid Philippine number..."]
}
```

---

#### POST /api/auth/login
**Description:** Authenticate user and get JWT token  
**Auth Required:** No  
**Request Body:**
```json
{
  "mobileNumber": "09171234567",
  "password": "SecurePass123!"
}
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "token": "jwt_token_here",
    "refreshToken": "refresh_token_here",
    "user": {
      "userId": 123,
      "username": "Test User",
      "firstName": "Test",
      "lastName": "User",
      "mobileNumber": "09171234567",
      "role": "Passenger"
    }
  }
}
```

**Error Response (400):**
```json
{
  "success": false,
  "message": "Invalid mobile number or password."
}
```

---

#### POST /api/auth/refresh
**Description:** Refresh JWT token  
**Auth Required:** No  
**Request Body:**
```json
{
  "userId": 123,
  "refreshToken": "refresh_token_here"
}
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "Token refreshed successfully.",
  "data": {
    "token": "new_jwt_token_here"
  }
}
```

---

#### POST /api/auth/logout
**Description:** Invalidate refresh token  
**Auth Required:** Yes  
**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "Logout successful."
}
```

---

### Card Endpoints

#### GET /api/cards/me
**Description:** Get authenticated user's card  
**Auth Required:** Yes  
**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "Card retrieved successfully.",
  "data": {
    "cardId": 1,
    "cardNumber": "4111-****-****-1111",
    "status": "ACTIVE",
    "passengerType": "Passenger",
    "issueDate": "2025-01-01T00:00:00Z",
    "expiryDate": "2027-01-01T00:00:00Z"
  }
}
```

**Error Response (404):**
```json
{
  "success": false,
  "message": "No Transit Card found for this user."
}
```

---

### Payment Endpoints

#### GET /api/payment/qr/{cardId}
**Description:** Generate QR code for payment  
**Auth Required:** Yes  
**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Success Response (200):**
```json
{
  "success": true,
  "data": {
    "qrId": 1,
    "cardId": 1,
    "data": "eyJ0aW1lIjogMTczODgwMDAwLjAwMDAwLCAic2VydmVyIjogIlRyYXNpdFBheSJ9",
    "signature": "base64_encoded_signature",
    "issuedAt": "2025-01-01T00:00:00Z",
    "expiresAt": "2026-01-01T00:00:00Z"
  }
}
```

**Decoded QR Payload:**
```json
{
  "time": 1738800000.000000,
  "server": "TransitPay"
}
```

---

#### POST /api/payment/scan-physical
**Description:** Process payment from physical QR scan  
**Auth Required:** Yes  
**Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "CardNumber": "4111111111111111",
  "OriginStationId": 1,
  "DestinationStationId": 2
}
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "Payment processed successfully.",
  "data": {
    "transactionId": 123,
    "amount": 12.50,
    "transactionType": "PAYMENT",
    "referenceNumber": "TXN-XXXXXXXXXX"
  }
}
```

---

### Trip Endpoints

#### GET /api/Trip/active
**Description:** Get driver's active trip  
**Auth Required:** Yes (Driver or Admin)  
**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Success Response (200) - No Active Trip:**
```json
{
  "success": true,
  "message": "No active trip found.",
  "data": null
}
```

**Success Response (200) - Active Trip:**
```json
{
  "success": true,
  "message": "Active trip retrieved successfully.",
  "data": {
    "tripId": 1,
    "driverId": 5,
    "originStationId": 1,
    "originStationName": "Central Station",
    "finalDestinationStationId": 2,
    "finalDestinationStationName": "Airport Station",
    "currentBoardingOriginStationId": 1,
    "currentBoardingOriginStationName": "Central Station",
    "boardingOriginUpdatedAt": "2025-01-01T00:00:00Z",
    "routeName": "Central Station → Airport Station",
    "tripStatus": "Active",
    "startedAt": "2025-01-01T00:00:00Z",
    "passengerCount": 0,
    "totalRevenue": 0.00
  }
}
```

---

### Admin Endpoints

#### GET /api/admin/terminals
**Description:** Get all terminals  
**Auth Required:** Yes (Admin only)  
**Headers:**
```
Authorization: Bearer {admin_jwt_token}
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "Terminals retrieved successfully.",
  "data": [
    {
      "terminalId": 1,
      "terminalName": "Central Terminal",
      "isActive": true
    }
  ]
}
```

---

#### GET /api/admin/drivers
**Description:** Get all drivers  
**Auth Required:** Yes (Admin only)  
**Headers:**
```
Authorization: Bearer {admin_jwt_token}
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "Drivers retrieved successfully.",
  "data": [
    {
      "userId": 5,
      "username": "driver1",
      "firstName": "John",
      "lastName": "Doe",
      "mobileNumber": "09171234567",
      "isActive": true
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "total": 1,
    "totalPages": 1
  }
}
```

---

## Testing Workflows

### Passenger Flow

1. **Register** a new passenger user
2. **Login** to get JWT token
3. **Get card** using `GET /api/cards/me`
4. **Generate QR** using `GET /api/payment/qr/{cardId}`
5. **Decode QR** payload to verify data

### Driver Flow

1. **Login** as driver (or use existing driver account)
2. **Check active trip** using `GET /api/Trip/active`
3. **Start trip** (if no active trip)
4. **Scan QR** using `POST /api/payment/scan-physical`
5. **View trip history** (if implemented)

### Admin Flow

1. **Login** as admin (mobile: `0000000000`)
2. **Get terminals** using `GET /api/admin/terminals`
3. **Get drivers** using `GET /api/admin/drivers`
4. **Manage fare rules** (if implemented)

---

## Troubleshooting

### Common Issues

#### 1. 404 Not Found
**Cause:** Incorrect endpoint URL  
**Solution:** Verify endpoint path in this guide

#### 2. 403 Forbidden
**Cause:** Missing or invalid JWT token  
**Solution:** 
- Ensure `Authorization: Bearer {token}` header is present
- Verify token is not expired
- Check user has required role

#### 3. 400 Bad Request
**Cause:** Invalid request body or validation failure  
**Solution:**
- Check request body matches schema
- Verify required fields are present
- Check field formats (e.g., mobile number pattern)

#### 4. 500 Internal Server Error
**Cause:** Server-side error  
**Solution:**
- Check API logs for details
- Verify database is running
- Ensure environment variables are set

### Debug Tips

1. **Enable detailed errors in development:**
   ```powershell
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   ```

2. **Check API logs:**
   ```powershell
   cd TransitPay-Prototype/TransitPay.API
   dotnet run 2>&1 | Select-String -Pattern "ERROR|WARN"
   ```

3. **Verify database connection:**
   ```powershell
   $env:DB_PASSWORD = 'YourDbPassword'
   dotnet ef database update --project TransitPay.API
   ```

4. **Test with curl:**
   ```powershell
   curl -H "Authorization: Bearer $token" http://localhost:5132/api/cards/me
   ```

---

## Smoke Test Script

For automated testing, use the smoke test script:

```powershell
cd TransitPay-Prototype
.\run_smoke_tests.ps1 -ApiUrl 'http://localhost:5132'
```

**Features:**
- Automated endpoint testing
- Comprehensive logging
- Performance metrics
- Fail-fast behavior
- Exit codes for CI/CD

See `SMOKE_TEST_REFACTORING_SUMMARY.md` for details.

---

## Additional Resources

- **API Documentation:** See controller source code in `TransitPay.API/Controllers/`
- **Database Schema:** See `TransitPay.API/Models/`
- **Migration History:** See `TransitPay.API/Migrations/`
- **Smoke Test Reports:** See `SMOKE_TEST_*.md` files

---

## Support

For issues or questions:
1. Check this guide first
2. Review API logs
3. Consult `SMOKE_TEST_API_COMPATIBILITY_REPORT.md`
4. Contact development team

---

*Last updated: 2025-08-08*