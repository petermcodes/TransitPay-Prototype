# TransitPay Testing Guide

## Overview
Yes, you can test the project despite the identified issues! This guide will help you test both the backend API and frontend applications.

---

## Prerequisites

### Required Software
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Postman](https://www.postman.com/downloads/) or similar API testing tool (optional)

### System Requirements
- Windows 10/11, macOS, or Linux
- At least 4GB RAM available
- Ports 5000-5005, 5173-5175, and 5432 available

---

## Quick Start Testing

### 1. Start the Database

```bash
# From the project root
cd TransitPay-Prototype
docker-compose up -d

# Verify PostgreSQL is running
docker-compose ps
```

Expected output: `transitpay-db` should show as "Up"

### 2. Start the Backend API

```bash
cd TransitPay-Prototype/TransitPay.API
dotnet restore
dotnet run
```

The API will start at: `https://localhost:5001` (or `http://localhost:5000`)

**Access points:**
- API Base URL: `https://localhost:5001/api`
- Swagger UI: `https://localhost:5001/swagger`
- Health Check: `https://localhost:5001/health`

### 3. Test the Frontend Apps (Optional)

Open separate terminal windows:

```bash
# Passenger App
cd TransitPay-Prototype/passenger-app
npm install
npm run dev
# Access at: http://localhost:5173

# Driver App
cd TransitPay-Prototype/driver-app
npm install
npm run dev
# Access at: http://localhost:5174

# Admin Dashboard
cd TransitPay-Prototype/admin-dashboard
npm install
npm run dev
# Access at: http://localhost:5175
```

---

## Testing the Backend API

### Method 1: Using Swagger UI (Easiest)

1. Navigate to `https://localhost:5001/swagger`
2. You'll see all available API endpoints
3. Click on any endpoint to expand it
4. Click "Try it out"
5. Fill in the required parameters
6. Click "Execute"

### Method 2: Using Postman/Insomnia

Import the following endpoints into your API client:

#### Authentication Endpoints

**1. Register a New User**
```
POST https://localhost:5001/api/auth/register
Content-Type: application/json

{
  "firstName": "Juan",
  "lastName": "Dela Cruz",
  "mobileNumber": "09171234567",
  "password": "Test123!",
  "roleName": "Passenger"
}
```

**2. Login**
```
POST https://localhost:5001/api/auth/login
Content-Type: application/json

{
  "mobileNumber": "09171234567",
  "password": "Test123!"
}
```

**Response will include:**
- `token` - JWT access token
- `refreshToken` - Refresh token
- `user` - User details

**3. Refresh Token**
```
POST https://localhost:5001/api/auth/refresh
Content-Type: application/json

{
  "userId": 1,
  "refreshToken": "your-refresh-token-here"
}
```

#### Payment Endpoints (Requires Authentication)

Add the JWT token to requests:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**1. Preview Fare**
```
GET https://localhost:5001/api/payment/fare/1/2
```

**2. Process Payment**
```
POST https://localhost:5001/api/payment/fare
Content-Type: application/json

{
  "cardId": 1,
  "stationId": 2,
  "amount": 0
}
```

#### Admin Endpoints (Requires Admin Role)

**Default Admin Credentials:**
- Username: `Admin`
- Password: `Admin`

**1. Get All Users**
```
GET https://localhost:5001/api/admin/users
Authorization: Bearer {admin-token}
```

**2. Get All Stations**
```
GET https://localhost:5001/api/admin/stations
Authorization: Bearer {admin-token}
```

**3. Get All Towns**
```
GET https://localhost:5001/api/admin/towns
Authorization: Bearer {admin-token}
```

**4. Get Fare Rules**
```
GET https://localhost:5001/api/admin/fare-rules
Authorization: Bearer {admin-token}
```

**5. Get Transactions**
```
GET https://localhost:5001/api/admin/transactions?page=1&pageSize=20
Authorization: Bearer {admin-token}
```

**6. Get Report Summary**
```
GET https://localhost:5001/api/admin/reports/summary
Authorization: Bearer {admin-token}
```

### Method 3: Using curl Commands

```bash
# Register a user
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"firstName":"Juan","lastName":"Dela Cruz","mobileNumber":"09171234567","password":"Test123!","roleName":"Passenger"}'

# Login
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"mobileNumber":"09171234567","password":"Test123!"}'

# Health check
curl https://localhost:5001/health
```

---

## Testing the Frontend Apps

### Passenger App Testing

**Test Flow:**
1. Open `http://localhost:5173`
2. Navigate through screens:
   - Splash → Welcome → Login → Home
   - Test QR Code display
   - Test Top Up flow
   - Test Payment flow
   - Test Profile screen

**What to verify:**
- ✅ UI animations work smoothly
- ✅ Navigation between screens
- ✅ Form inputs respond correctly
- ✅ Buttons show loading states
- ✅ Responsive design on mobile viewport

**Note:** Currently uses mock data - no real API calls

### Driver App Testing

**Test Flow:**
1. Open `http://localhost:5174`
2. Login screen → Dashboard
3. Test QR Scanner simulation
4. Test Scan Result screen
5. Test Trip History

**What to verify:**
- ✅ Scanner animation works
- ✅ Screen transitions
- ✅ Data display

**Note:** Currently uses mock data - no real API calls

### Admin Dashboard Testing

**Test Flow:**
1. Open `http://localhost:5175`
2. Navigate through sidebar sections:
   - Dashboard
   - Passengers
   - Drivers
   - Stations
   - Towns
   - Fare Matrix
   - Transactions
   - Reports
   - Settings

**What to verify:**
- ✅ Sidebar navigation
- ✅ Data tables display correctly
- ✅ Filter and search UI works
- ✅ Responsive layout
- ✅ Charts and KPIs render

**Note:** Currently uses mock data - no real API calls

---

## Running Automated Tests

```bash
cd TransitPay-Prototype/TransitPay.API.Tests
dotnet test
```

**Current test coverage:**
- 1 test for PaymentService
- Uses InMemory database for isolation

**Expected output:**
```
Total tests: 1
Passed: 1
Failed: 0
```

---

## Database Testing

### Connect to PostgreSQL

```bash
# Using psql (if installed)
psql -h localhost -p 5432 -U postgres -d TransitPayDB
# Password: Akosipm123!

# Or using Docker exec
docker exec -it transitpay-db psql -U postgres -d TransitPayDB
```

### Verify Seeded Data

```sql
-- Check tables
\dt

-- View users
SELECT * FROM users;

-- View cards and wallets
SELECT * FROM cards;
SELECT * FROM wallets;

-- View stations and towns
SELECT * FROM towns;
SELECT * FROM stations;

-- View fare rules
SELECT * FROM fare_rules;

-- Check admin user exists
SELECT username, first_name, last_name FROM users WHERE username = 'Admin';
```

---

## Common Testing Scenarios

### Scenario 1: Complete Payment Flow

1. **Register a new passenger**
   ```json
   POST /api/auth/register
   {
     "firstName": "Maria",
     "lastName": "Santos",
     "mobileNumber": "09281234567",
     "password": "Maria123!",
     "roleName": "Passenger"
   }
   ```

2. **Login and get token**
   ```json
   POST /api/auth/login
   {
     "mobileNumber": "09281234567",
     "password": "Maria123!"
   }
   ```
   Copy the `token` from response

3. **Preview fare**
   ```
   GET /api/payment/fare/1/2
   ```
   (Assumes card ID 1, station ID 2 exist)

4. **Process payment**
   ```json
   POST /api/payment/fare
   Authorization: Bearer {token}
   {
     "cardId": 1,
     "stationId": 2,
     "amount": 0
   }
   ```

5. **Verify transaction**
   ```json
   GET /api/admin/transactions
   Authorization: Bearer {admin-token}
   ```

### Scenario 2: Admin Management

1. **Login as admin**
   ```json
   POST /api/auth/login
   {
     "mobileNumber": "0000000000",
     "password": "Admin"
   }
   ```

2. **Create a new town**
   ```json
   POST /api/admin/towns
   Authorization: Bearer {admin-token}
   {
     "townName": "Makati"
   }
   ```

3. **Create a station**
   ```json
   POST /api/admin/stations
   Authorization: Bearer {admin-token}
   {
     "townId": 4,
     "stationName": "Ayala Station"
   }
   ```

4. **Create fare rule**
   ```json
   POST /api/admin/fare-rules
   Authorization: Bearer {admin-token}
   {
     "originStationId": 1,
     "destinationStationId": 6,
     "vehicleType": "BUS",
     "passengerType": "Passenger",
     "fareAmount": 25.00,
     "effectiveDate": "2026-01-01T00:00:00Z"
   }
   ```

---

## Troubleshooting

### Database Connection Issues

**Problem:** API can't connect to PostgreSQL

**Solutions:**
```bash
# Check if PostgreSQL is running
docker-compose ps

# View PostgreSQL logs
docker-compose logs postgres

# Restart PostgreSQL
docker-compose restart postgres

# Verify port 5432 is not in use
netstat -ano | findstr :5432  # Windows
lsof -i :5432                 # macOS/Linux
```

### API Won't Start

**Problem:** dotnet run fails

**Solutions:**
```bash
# Clean and rebuild
dotnet clean
dotnet build

# Restore packages
dotnet restore

# Check if ports are available
netstat -ano | findstr :5000  # Windows
lsof -i :5000                 # macOS/Linux
```

### Frontend App Issues

**Problem:** npm run dev fails

**Solutions:**
```bash
# Delete and reinstall node_modules
rm -rf node_modules package-lock.json
npm install

# Clear npm cache
npm cache clean --force

# Check Node version
node --version  # Should be 18+
```

### JWT Token Issues

**Problem:** 401 Unauthorized errors

**Solutions:**
- Ensure you're sending the token in the Authorization header
- Check token hasn't expired (default: 15 minutes)
- Use refresh token endpoint to get new access token

---

## Performance Testing

### Load Testing with Apache Bench

```bash
# Test health endpoint
ab -n 1000 -c 10 https://localhost:5001/health

# Test login endpoint
ab -n 100 -c 5 -p login.json -T application/json https://localhost:5001/api/auth/login
```

### Database Performance

```sql
-- Check active connections
SELECT count(*) FROM pg_stat_activity;

-- Check query performance
EXPLAIN ANALYZE SELECT * FROM users WHERE mobile_number = '09171234567';

-- Check indexes
\d users
```

---

## Security Testing Notes

### ⚠️ Important Warnings

1. **This is a PROTOTYPE** - Do not expose to the public internet
2. **Hardcoded credentials** exist in the codebase
3. **No rate limiting** on authentication endpoints
4. **Weak default passwords** (Admin/Admin)

### Safe Testing Practices

✅ Test only on localhost or private network  
✅ Use test data, not real user data  
✅ Don't commit any API keys or tokens  
✅ Clear test data regularly  
✅ Don't test from public networks  

---

## Next Steps for Production

Before deploying to production, you must:

1. **Fix Security Issues**
   - Move all secrets to environment variables
   - Implement proper password policies
   - Add rate limiting
   - Enable HTTPS only

2. **Complete API Integration**
   - Connect frontend apps to backend
   - Implement proper error handling
   - Add loading states

3. **Increase Test Coverage**
   - Add unit tests for all services
   - Add integration tests
   - Add E2E tests

4. **Performance Optimization**
   - Add database indexes
   - Implement caching
   - Optimize queries

---

## Getting Help

If you encounter issues:

1. Check the console logs for error messages
2. Verify all prerequisites are installed
3. Ensure ports are not in use
4. Check Docker is running
5. Review the troubleshooting section above

---

## Testing Checklist

Use this checklist to track your testing progress:

### Backend API
- [ ] Database starts successfully
- [ ] API starts without errors
- [ ] Swagger UI loads
- [ ] Health check endpoint works
- [ ] User registration works
- [ ] User login works
- [ ] JWT tokens are generated
- [ ] Protected endpoints require authentication
- [ ] Payment processing works
- [ ] Admin endpoints work with admin token
- [ ] CRUD operations work for stations, towns, fare rules

### Frontend Apps
- [ ] Passenger app loads
- [ ] Driver app loads
- [ ] Admin dashboard loads
- [ ] All screens navigate correctly
- [ ] Forms accept input
- [ ] UI is responsive
- [ ] Animations work smoothly

### Database
- [ ] PostgreSQL is running
- [ ] Can connect with psql
- [ ] Seeded data exists
- [ ] Migrations applied successfully
- [ ] Can query tables

### Automated Tests
- [ ] dotnet test runs successfully
- [ ] All tests pass

---

## Conclusion

Yes, you can definitely test the project! The backend is fully functional, the database works, and the frontend demonstrates the complete user experience. While there are security and integration issues that need to be addressed before production, the prototype is excellent for:

- Demonstrating the concept
- Testing the UI/UX
- Evaluating the architecture
- Understanding the business logic
- Planning production improvements

Happy testing! 🚀