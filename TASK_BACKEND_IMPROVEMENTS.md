# Backend Code Quality Improvements Task

## Overview
This task addresses critical code quality, security, and maintainability issues in the TransitPay.API backend services.

---

## Issues Identified

### 1. Inconsistent Return Types (AuthService.cs)
**Severity**: Medium  
**Location**: `TransitPay.API/Services/AuthService.cs`

**Problem**:
- Methods return `object` instead of strongly-typed DTOs
- Makes API contracts unclear and error-prone
- Loses compile-time type safety

**Current Code Example**:
```csharp
public async Task<object> LoginAsync(LoginRequest request)
{
    // ... returns anonymous object or dynamic type
}
```

**Required Fix**:
- Create proper response DTOs in `DTOs/` folder
- Update all AuthService methods to return specific types
- Update all controller actions to use strongly-typed responses

**Files to Create/Modify**:
- Create: `DTOs/AuthResponses.cs`
- Modify: `Services/AuthService.cs`
- Modify: `Controllers/AuthController.cs`

---

### 2. Missing Input Validation
**Severity**: High  
**Location**: `TransitPay.API/Services/PaymentService.cs`

**Problem**:
- No validation for negative amounts
- No check if origin/destination stations are the same
- No validation for future effective dates
- No check for minimum fare amounts

**Current Code Example** (Line 52):
```csharp
var fareAmount = amount > 0 ? amount : fareRule?.FareAmount ?? 0m;
```

**Required Fix**:
```csharp
// Add comprehensive validation
if (amount < 0)
    throw new ArgumentException("Amount cannot be negative", nameof(amount));

if (originStationId == destinationStationId)
    throw new ArgumentException("Origin and destination stations cannot be the same");

if (effectiveDate > DateTime.UtcNow)
    throw new ArgumentException("Effective date cannot be in the future");

if (fareAmount <= 0)
    throw new ArgumentException("Fare amount must be greater than zero");
```

**Files to Modify**:
- `Services/PaymentService.cs`
- `Services/AuthService.cs` (if applicable)

---

### 3. Magic Strings
**Severity**: Medium  
**Location**: Throughout the codebase

**Problem**:
- Status values like "ACTIVE", "PAYMENT", "FARE" scattered throughout
- Transaction types as raw strings
- Role names as raw strings
- Card status values as raw strings

**Current Code Examples**:
```csharp
// Models/Card.cs
public string Status { get; set; } = "ACTIVE";

// PaymentService.cs
if (transaction.TransactionType == "PAYMENT")

// AdminController.cs
var driverRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == "Driver");
```

**Required Fix**:
Create enums for all magic strings:

```csharp
// Enums/CardStatus.cs
public enum CardStatus
{
    ACTIVE,
    INACTIVE,
    SUSPENDED,
    EXPIRED
}

// Enums/TransactionType.cs
public enum TransactionType
{
    PAYMENT,
    TOP_UP,
    REFUND,
    FARE
}

// Enums/RoleName.cs
public enum RoleName
{
    Passenger,
    Driver,
    Admin
}
```

**Files to Create**:
- `Enums/CardStatus.cs`
- `Enums/TransactionType.cs`
- `Enums/RoleName.cs`
- `Enums/TransactionStatus.cs`
- `Enums/VehicleType.cs`
- `Enums/PassengerType.cs`

**Files to Modify**:
- `Models/Card.cs`
- `Models/Transaction.cs`
- `Models/User.cs`
- `Services/PaymentService.cs`
- `Services/AuthService.cs`
- `Controllers/AdminController.cs`
- All other controllers

---

### 4. Missing Error Handling
**Severity**: High  
**Location**: Throughout Services and Controllers

**Problem**:
- No try-catch blocks for database operations
- No logging of errors
- Generic error messages
- No correlation IDs for tracking

**Current Code Example**:
```csharp
public async Task<object> GetUserByIdAsync(int id)
{
    var user = await _dbContext.Users.FindAsync(id);
    return user; // No error handling if user is null
}
```

**Required Fix**:
```csharp
public async Task<User> GetUserByIdAsync(int id)
{
    try
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", id);
            throw new NotFoundException($"User with ID {id} not found");
        }
        return user;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving user: {UserId}", id);
        throw new DataAccessException("Failed to retrieve user", ex);
    }
}
```

**Files to Modify**:
- All files in `Services/` folder
- All files in `Controllers/` folder
- Add logging to `Program.cs` if not already configured

---

### 5. Payment Logic Security Issue (PaymentService.cs, Line 52)
**Severity**: Critical  
**Location**: `TransitPay.API/Services/PaymentService.cs:52`

**Problem**:
```csharp
var fareAmount = amount > 0 ? amount : fareRule?.FareAmount ?? 0m;
```

**Security Risk**:
- Allows client to override fare amount
- Potential fare manipulation vulnerability
- Client can pay less than required fare
- No server-side enforcement of fare rules

**Current Flow**:
1. Client sends payment request with `amount` parameter
2. Server uses client's amount if > 0
3. Client can send `amount: 0.01` to pay minimal fare
4. Server accepts it without validation

**Required Fix**:
```csharp
// Server should ALWAYS determine fare based on fare rules
var fareRule = await _dbContext.FareRules
    .FirstOrDefaultAsync(fr => 
        fr.OriginStationId == originStationId && 
        fr.DestinationStationId == destinationStationId &&
        fr.VehicleType == vehicleType &&
        fr.PassengerType == passengerType &&
        fr.IsActive && 
        fr.DeletedAt == null);

if (fareRule == null)
{
    throw new FareRuleNotFoundException(
        $"No active fare rule found for route {originStationId} -> {destinationStationId}");
}

var fareAmount = fareRule.FareAmount; // ALWAYS use server-determined fare

// Log if client tried to override
if (amount > 0 && amount != fareAmount)
{
    _logger.LogWarning(
        "Client attempted to override fare. Requested: {RequestedAmount}, Required: {FareAmount}",
        amount, fareAmount);
}
```

**Files to Modify**:
- `Services/PaymentService.cs`
- `Controllers/PaymentController.cs` (remove amount from request if present)

---

## Implementation Plan

### Phase 1: Create Enums and DTOs
1. Create all enum files in `Enums/` folder
2. Create response DTOs in `DTOs/` folder
3. Update model classes to use enums

### Phase 2: Fix Critical Issues
1. Fix payment logic security issue (Priority 1)
2. Add input validation (Priority 2)
3. Add error handling and logging (Priority 3)

### Phase 3: Code Cleanup
1. Replace magic strings with enums throughout
2. Update all return types to be strongly-typed
3. Update all controllers to use new DTOs

### Phase 4: Testing
1. Unit tests for validation logic
2. Integration tests for payment security
3. Test all enum conversions

---

## Success Criteria

- [ ] All methods return strongly-typed DTOs (no `object` returns)
- [ ] All user inputs are validated before processing
- [ ] No magic strings remain in codebase
- [ ] All database operations have try-catch blocks
- [ ] All errors are logged with context
- [ ] Payment amount is always server-determined
- [ ] All tests pass
- [ ] No security vulnerabilities in payment flow

---

## Estimated Effort

- Phase 1: 2-3 hours
- Phase 2: 3-4 hours
- Phase 3: 2-3 hours
- Phase 4: 2-3 hours

**Total**: 9-13 hours

---

## Notes

- This is a refactoring task - no database schema changes required
- Changes are backward compatible if done carefully
- Consider adding integration tests before starting
- Review all changes with security team for payment logic