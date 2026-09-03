# Backend Refactoring Task - TransitPay.API

## Executive Summary
This task addresses critical code quality, security vulnerabilities, and maintainability issues in the TransitPay.API backend. The issues range from security risks (payment manipulation) to poor code practices (magic strings, weak typing) that need immediate attention.

**Priority**: HIGH  
**Estimated Effort**: 9-13 hours  
**Risk Level**: Medium (refactoring with breaking changes to API contracts)

---

## Issues Identified

### 🔴 CRITICAL: Payment Logic Security Vulnerability
**Location**: `TransitPay.API/Services/PaymentService.cs:52`  
**Severity**: Critical  
**Risk**: Financial loss, fare manipulation

#### Current Code (VULNERABLE):
```csharp
var fareAmount = amount > 0 ? amount : fareRule?.FareAmount ?? 0m;
```

#### Problem:
- Client can override server-determined fare by passing `amount` parameter
- No server-side enforcement of fare rules
- Potential for fare manipulation (paying less than required)
- Line 30 in `PaymentController.cs` passes client's `request.Amount` directly to service

#### Attack Scenario:
1. Client sends `POST /api/payment/fare` with `{ "cardId": 1, "stationId": 2, "amount": 0.01 }`
2. Server uses client's amount (0.01) instead of fare rule amount (e.g., 12.50)
3. Client pays fraction of actual fare
4. No validation or logging of manipulation attempt

#### Required Fix:
```csharp
// PaymentService.cs - ALWAYS determine fare from rules
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
    _logger.LogWarning("No fare rule found for route {Origin} -> {Destination}", 
        originStationId, destinationStationId);
    throw new FareRuleNotFoundException("No active fare rule found for this route");
}

var fareAmount = fareRule.FareAmount; // Server ALWAYS determines fare

// Log manipulation attempts
if (amount > 0 && amount != fareAmount)
{
    _logger.LogWarning(
        "SECURITY: Client attempted fare override. Requested: {Requested}, Required: {Required}, CardId: {CardId}",
        amount, fareAmount, cardId);
}
```

#### Files to Modify:
- `Services/PaymentService.cs` (remove `amount` parameter or ignore it)
- `Controllers/PaymentController.cs` (remove Amount from PaymentRequest)
- `DTOs/PaymentRequest.cs` (create new DTO without amount)

---

### 🟡 MEDIUM: Inconsistent Return Types
**Location**: `AuthService.cs`, `PaymentService.cs`  
**Severity**: Medium  
**Impact**: Poor API contracts, no compile-time safety

#### Current Code Examples:

**AuthService.cs:**
```csharp
public async Task<object> RegisterAsync(...) // Line 22
public async Task<object> LoginAsync(...)     // Line 54
public async Task<object> RefreshTokenAsync(...) // Line 93

// Returns anonymous objects:
return new { success = false, message = "Role not found." };
return new { success = true, data = new { ... } };
```

**PaymentService.cs:**
```csharp
public async Task<object> ProcessPaymentAsync(...) // Line 17

return new { success = false, message = "Card not found." };
return new { success = true, data = new { ... } };
```

#### Problems:
- No strongly-typed response contracts
- Clients must parse dynamic objects
- No IntelliSense support
- Refactoring is error-prone
- No validation of response structure

#### Required Fix:

**Step 1: Create DTOs in `DTOs/` folder**

```csharp
// DTOs/Auth/RegisterResponse.cs
namespace TransitPay.API.DTOs.Auth;

public class RegisterResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RegisterData? Data { get; set; }
}

public class RegisterData
{
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}

// DTOs/Auth/LoginResponse.cs
public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public LoginData? Data { get; set; }
}

public class LoginData
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserInfo User { get; set; } = new();
}

public class UserInfo
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
}

// DTOs/Auth/RefreshTokenResponse.cs
public class RefreshTokenResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RefreshTokenData? Data { get; set; }
}

public class RefreshTokenData
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

// DTOs/Payment/PaymentResponse.cs
public class PaymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public PaymentData? Data { get; set; }
}

public class PaymentData
{
    public int CardId { get; set; }
    public int StationId { get; set; }
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public string? StationName { get; set; }
}

// DTOs/Common/ApiResponse.cs (generic wrapper)
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    
    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation successful")
    {
        return new ApiResponse<T> { Success = true, Message = message, Data = data };
    }
    
    public static ApiResponse<T> ErrorResponse(string message)
    {
        return new ApiResponse<T> { Success = false, Message = message };
    }
}
```

**Step 2: Update Service Methods**

```csharp
// AuthService.cs
public async Task<RegisterResponse> RegisterAsync(...)
{
    var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
    if (role == null)
    {
        return new RegisterResponse
        {
            Success = false,
            Message = "Role not found."
        };
    }
    
    // ... rest of logic
    
    return new RegisterResponse
    {
        Success = true,
        Message = "User registered successfully.",
        Data = new RegisterData
        {
            UserId = user.UserId,
            Role = role.RoleName
        }
    };
}

public async Task<LoginResponse> LoginAsync(...)
{
    // ... validation logic
    
    return new LoginResponse
    {
        Success = true,
        Message = "Login successful.",
        Data = new LoginData
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            User = new UserInfo
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                MobileNumber = user.MobileNumber,
                RoleId = user.RoleId,
                RoleName = role?.RoleName
            }
        }
    };
}
```

**Step 3: Update Controllers**

```csharp
// AuthController.cs
[HttpPost("register")]
public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed."));
    }
    
    var result = await _authService.RegisterAsync(...);
    if (!result.Success)
    {
        return BadRequest(result);
    }
    
    return Ok(result);
}
```

#### Files to Create:
- `DTOs/Auth/RegisterResponse.cs`
- `DTOs/Auth/LoginResponse.cs`
- `DTOs/Auth/RefreshTokenResponse.cs`
- `DTOs/Payment/PaymentResponse.cs`
- `DTOs/Common/ApiResponse.cs`
- `DTOs/Payment/PaymentRequest.cs` (remove Amount field)

#### Files to Modify:
- `Services/AuthService.cs`
- `Services/PaymentService.cs`
- `Controllers/AuthController.cs`
- `Controllers/PaymentController.cs`

---

### 🟡 MEDIUM: Missing Input Validation
**Location**: `PaymentService.cs`, `AuthService.cs`  
**Severity**: High  
**Impact**: Data integrity, business logic errors

#### Current Issues:

**PaymentService.cs:**
```csharp
// Line 17: No validation on parameters
public async Task<object> ProcessPaymentAsync(int cardId, int stationId, decimal amount)

// No checks for:
// - Negative amounts
// - Same origin/destination stations
// - Future effective dates
// - Minimum fare amounts
```

**AuthService.cs:**
```csharp
// Line 22: No validation on input parameters
public async Task<object> RegisterAsync(string firstName, string lastName, 
    string mobileNumber, string password, string roleName)

// No checks for:
// - Empty/null strings
// - Weak passwords
// - Invalid mobile number format
// - Invalid role names
```

#### Required Fix:

```csharp
// PaymentService.cs
public async Task<PaymentResponse> ProcessPaymentAsync(int cardId, int stationId)
{
    // Validate cardId
    if (cardId <= 0)
    {
        _logger.LogWarning("Invalid card ID: {CardId}", cardId);
        return PaymentResponse.ErrorResponse("Invalid card ID.");
    }
    
    // Validate stationId
    if (stationId <= 0)
    {
        _logger.LogWarning("Invalid station ID: {StationId}", stationId);
        return PaymentResponse.ErrorResponse("Invalid station ID.");
    }
    
    try
    {
        var card = await _dbContext.Cards.FindAsync(cardId);
        if (card == null)
        {
            _logger.LogWarning("Card not found: {CardId}", cardId);
            return PaymentResponse.ErrorResponse("Card not found.");
        }
        
        // Validate card status using enum
        if (card.Status != CardStatus.ACTIVE)
        {
            _logger.LogWarning("Card not active. CardId: {CardId}, Status: {Status}", 
                cardId, card.Status);
            return PaymentResponse.ErrorResponse("Card is not active.");
        }
        
        // ... rest of logic with validation
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing payment for card {CardId} at station {StationId}", 
            cardId, stationId);
        throw new PaymentProcessingException("Failed to process payment", ex);
    }
}

// AuthService.cs
public async Task<RegisterResponse> RegisterAsync(string firstName, string lastName, 
    string mobileNumber, string password, string roleName)
{
    // Validate inputs
    if (string.IsNullOrWhiteSpace(firstName) || firstName.Length < 2)
    {
        return new RegisterResponse 
        { 
            Success = false, 
            Message = "First name must be at least 2 characters." 
        };
    }
    
    if (string.IsNullOrWhiteSpace(lastName) || lastName.Length < 2)
    {
        return new RegisterResponse 
        { 
            Success = false, 
            Message = "Last name must be at least 2 characters." 
        };
    }
    
    if (string.IsNullOrWhiteSpace(mobileNumber) || mobileNumber.Length != 10)
    {
        return new RegisterResponse 
        { 
            Success = false, 
            Message = "Mobile number must be exactly 10 digits." 
        };
    }
    
    if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
    {
        return new RegisterResponse 
        { 
            Success = false, 
            Message = "Password must be at least 6 characters." 
        };
    }
    
    if (string.IsNullOrWhiteSpace(roleName))
    {
        return new RegisterResponse 
        { 
            Success = false, 
            Message = "Role name is required." 
        };
    }
    
    // ... rest of logic
}
```

#### Files to Modify:
- `Services/PaymentService.cs`
- `Services/AuthService.cs`
- Add validation to all request DTOs

---

### 🟡 MEDIUM: Magic Strings Throughout Codebase
**Location**: Models, Services, Controllers  
**Severity**: Medium  
**Impact**: Maintenance, typos, refactoring difficulty

#### Current Magic Strings Found:

**Models/Card.cs:**
```csharp
public string Status { get; set; } = "ACTIVE"; // Line 27
```

**Models/Transaction.cs:**
```csharp
public string TransactionType { get; set; } = string.Empty; // Line 25
// Used as: "PAYMENT", "TOP_UP", "REFUND"
```

**Models/Role.cs:**
```csharp
public string RoleName { get; set; } = string.Empty; // Line 14
// Used as: "Passenger", "Driver", "Admin"
```

**Models/FareRule.cs:**
```csharp
public string VehicleType { get; set; } = string.Empty; // Line 22
// Used as: "BUS", "TRAIN", "METRO"

public string PassengerType { get; set; } = string.Empty; // Line 24
// Used as: "Passenger", "Student", "Senior"
```

**Services/PaymentService.cs:**
```csharp
if (card.Status != "ACTIVE") // Line 25
TransactionType = "PAYMENT" // Line 70
```

**Services/AuthService.cs:**
```csharp
var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName); // Line 24
```

**Controllers/AdminController.cs:**
```csharp
var driverRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == "Driver"); // Line 55
t.TransactionType == "PAYMENT" // Line 231
```

**Program.cs:**
```csharp
new Role { RoleName = "Passenger" }, // Line 95
new Role { RoleName = "Driver" },    // Line 96
new Role { RoleName = "Admin" },     // Line 97
VehicleType = "BUS",                 // Line 116
PassengerType = "Passenger"          // Line 117
```

#### Required Fix:

**Step 1: Create Enums**

```csharp
// Enums/CardStatus.cs
namespace TransitPay.API.Enums;

public enum CardStatus
{
    ACTIVE,
    INACTIVE,
    SUSPENDED,
    EXPIRED
}

// Enums/TransactionType.cs
namespace TransitPay.API.Enums;

public enum TransactionType
{
    PAYMENT,
    TOP_UP,
    REFUND,
    FARE
}

// Enums/TransactionStatus.cs
namespace TransitPay.API.Enums;

public enum TransactionStatus
{
    PENDING,
    COMPLETED,
    FAILED,
    CANCELLED
}

// Enums/RoleName.cs
namespace TransitPay.API.Enums;

public enum RoleName
{
    Passenger,
    Driver,
    Admin
}

// Enums/VehicleType.cs
namespace TransitPay.API.Enums;

public enum VehicleType
{
    BUS,
    TRAIN,
    METRO,
    FERRY
}

// Enums/PassengerType.cs
namespace TransitPay.API.Enums;

public enum PassengerType
{
    Passenger,
    Student,
    Senior,
    DISABLED
}
```

**Step 2: Update Models**

```csharp
// Models/Card.cs
using TransitPay.API.Enums;

[Column("status")]
public CardStatus Status { get; set; } = CardStatus.ACTIVE;

// Models/Transaction.cs
using TransitPay.API.Enums;

[Column("transaction_type")]
public TransactionType TransactionType { get; set; }

// Models/Role.cs
using TransitPay.API.Enums;

[Column("role_name")]
public RoleName RoleName { get; set; }

// Models/FareRule.cs
using TransitPay.API.Enums;

[Column("vehicle_type")]
public VehicleType VehicleType { get; set; }

[Column("passenger_type")]
public PassengerType PassengerType { get; set; }
```

**Step 3: Update Services**

```csharp
// PaymentService.cs
using TransitPay.API.Enums;

if (card.Status != CardStatus.ACTIVE)
{
    return PaymentResponse.ErrorResponse("Card is not active.");
}

// Add transaction
_dbContext.Transactions.Add(new Transaction
{
    // ...
    TransactionType = TransactionType.PAYMENT,
    // ...
});

// AuthService.cs
using TransitPay.API.Enums;

var role = await _dbContext.Roles
    .FirstOrDefaultAsync(r => r.RoleName == RoleName.Passenger);
```

**Step 4: Update Controllers**

```csharp
// AdminController.cs
using TransitPay.API.Enums;

var driverRole = await _dbContext.Roles
    .FirstOrDefaultAsync(r => r.RoleName == RoleName.Driver);

var totalRevenue = await _dbContext.Transactions
    .Where(t => t.DeletedAt == null && t.TransactionType == TransactionType.PAYMENT)
    .SumAsync(t => (decimal?)t.Amount) ?? 0m;
```

**Step 5: Update Program.cs**

```csharp
// Program.cs
using TransitPay.API.Enums;

db.Roles.AddRange(
    new Role { RoleName = RoleName.Passenger },
    new Role { RoleName = RoleName.Driver },
    new Role { RoleName = RoleName.Admin });

db.FareRules.Add(new FareRule
{
    // ...
    VehicleType = VehicleType.BUS,
    PassengerType = PassengerType.Passenger,
    // ...
});
```

**Step 6: Database Migration**

```bash
dotnet ef migrations add ReplaceMagicStringsWithEnums
dotnet ef database update
```

**Note**: Since enums are stored as strings/integers in the database, you may need to:
- Option A: Use string enums (EF Core will store as strings)
- Option B: Use integer enums and create a migration to update existing data
- Option C: Keep string properties but add enum properties for code usage

**Recommended**: Use string enums for simplicity:
```csharp
public enum CardStatus : string
{
    ACTIVE = "ACTIVE",
    INACTIVE = "INACTIVE",
    SUSPENDED = "SUSPENDED",
    EXPIRED = "EXPIRED"
}
```

#### Files to Create:
- `Enums/CardStatus.cs`
- `Enums/TransactionType.cs`
- `Enums/TransactionStatus.cs`
- `Enums/RoleName.cs`
- `Enums/VehicleType.cs`
- `Enums/PassengerType.cs`

#### Files to Modify:
- `Models/Card.cs`
- `Models/Transaction.cs`
- `Models/Role.cs`
- `Models/FareRule.cs`
- `Models/User.cs` (if applicable)
- `Models/Wallet.cs` (if applicable)
- `Services/PaymentService.cs`
- `Services/AuthService.cs`
- `Controllers/AdminController.cs`
- `Controllers/AuthController.cs`
- `Controllers/PaymentController.cs`
- `Controllers/TransactionsController.cs`
- `Program.cs`

---

### 🟡 MEDIUM: Missing Error Handling and Logging
**Location**: All Services and Controllers  
**Severity**: High  
**Impact**: Debugging difficulty, poor observability

#### Current Issues:

**No try-catch blocks:**
```csharp
// PaymentService.cs
public async Task<object> ProcessPaymentAsync(...)
{
    var card = await _dbContext.Cards.FindAsync(cardId); // No error handling
    // ...
    await _dbContext.SaveChangesAsync(); // No error handling
}

// AuthService.cs
public async Task<object> RegisterAsync(...)
{
    var role = await _dbContext.Roles.FirstOrDefaultAsync(...); // No error handling
    // ...
    await _dbContext.SaveChangesAsync(); // No error handling
}
```

**No logging:**
- No error logging
- No warning logs for suspicious activity
- No info logs for audit trail
- No correlation IDs

#### Required Fix:

**Step 1: Add Logging to Services**

```csharp
// Services/PaymentService.cs
using Microsoft.Extensions.Logging;

public class PaymentService : IPaymentService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly ILogger<PaymentService> _logger;
    
    public PaymentService(TransitPayDbContext dbContext, 
        ILogger<PaymentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<PaymentResponse> ProcessPaymentAsync(int cardId, int stationId)
    {
        _logger.LogInformation("Processing payment for card {CardId} at station {StationId}", 
            cardId, stationId);
        
        try
        {
            var card = await _dbContext.Cards.FindAsync(cardId);
            if (card == null)
            {
                _logger.LogWarning("Card not found: {CardId}", cardId);
                return PaymentResponse.ErrorResponse("Card not found.");
            }
            
            // ... business logic
            
            _logger.LogInformation("Payment successful. CardId: {CardId}, Amount: {Amount}, NewBalance: {Balance}",
                cardId, fareAmount, wallet.Balance);
            
            return new PaymentResponse
            {
                Success = true,
                Message = "Payment completed successfully.",
                Data = new PaymentData { ... }
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error processing payment for card {CardId}", cardId);
            throw new PaymentProcessingException("Failed to process payment due to database error", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing payment for card {CardId}", cardId);
            throw new PaymentProcessingException("Failed to process payment", ex);
        }
    }
}
```

**Step 2: Add Logging to AuthService**

```csharp
// Services/AuthService.cs
using Microsoft.Extensions.Logging;

public class AuthService : IAuthService
{
    private readonly TransitPayDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    
    public AuthService(TransitPayDbContext dbContext, 
        PasswordHasher<User> passwordHasher, 
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }
    
    public async Task<LoginResponse> LoginAsync(string mobileNumber, string password)
    {
        _logger.LogInformation("Login attempt for mobile number: {MobileNumber}", mobileNumber);
        
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber);
            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found: {MobileNumber}", mobileNumber);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid credentials."
                };
            }
            
            var verificationResult = _passwordHasher.VerifyHashedPassword(null!, user.PasswordHash, password);
            if (verificationResult != PasswordVerificationResult.Success)
            {
                _logger.LogWarning("Login failed - invalid password for user: {UserId}", user.UserId);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid credentials."
                };
            }
            
            var role = await _dbContext.Roles.FindAsync(user.RoleId);
            var token = await _tokenService.CreateTokenAsync(user);
            var refreshToken = await _tokenService.CreateRefreshTokenAsync(user.UserId);
            
            _logger.LogInformation("Login successful for user: {UserId}", user.UserId);
            
            return new LoginResponse
            {
                Success = true,
                Message = "Login successful.",
                Data = new LoginData { ... }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for mobile number: {MobileNumber}", mobileNumber);
            throw new AuthenticationException("Login failed due to system error", ex);
        }
    }
}
```

**Step 3: Add Global Exception Handler**

```csharp
// Program.cs
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = 
            context.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.MapGet("/error", (HttpContext context) =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    
    logger.LogError(exception, "Unhandled exception occurred");
    
    return Results.Problem(
        title: "An error occurred while processing your request.",
        detail: app.Environment.IsDevelopment() ? exception?.Message : null,
        statusCode: 500,
        extensions: new Dictionary<string, object?>
        {
            ["traceId"] = context.TraceIdentifier
        });
});
```

**Step 4: Update Program.cs for Logging**

```csharp
// Logging is already configured (lines 76-83), just ensure all services use it
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

// Add logging scopes for better tracing
builder.Logging.AddFilter("TransitPay.API", LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
```

#### Files to Modify:
- `Services/PaymentService.cs` (add ILogger)
- `Services/AuthService.cs` (add ILogger)
- `Services/TokenService.cs` (add ILogger)
- `Controllers/AuthController.cs` (add logging)
- `Controllers/PaymentController.cs` (add logging)
- `Controllers/AdminController.cs` (add logging)
- `Program.cs` (add global exception handler)

---

## Implementation Plan

### Phase 1: Foundation (2-3 hours)
**Priority**: Create enums and DTOs first (no breaking changes yet)

1. **Create Enums** (30 mins)
   - Create `Enums/CardStatus.cs`
   - Create `Enums/TransactionType.cs`
   - Create `Enums/TransactionStatus.cs`
   - Create `Enums/RoleName.cs`
   - Create `Enums/VehicleType.cs`
   - Create `Enums/PassengerType.cs`

2. **Create DTOs** (1 hour)
   - Create `DTOs/Common/ApiResponse.cs`
   - Create `DTOs/Auth/RegisterResponse.cs`
   - Create `DTOs/Auth/LoginResponse.cs`
   - Create `DTOs/Auth/RefreshTokenResponse.cs`
   - Create `DTOs/Payment/PaymentRequest.cs` (remove Amount)
   - Create `DTOs/Payment/PaymentResponse.cs`

3. **Update Models** (30 mins)
   - Update `Models/Card.cs` to use CardStatus enum
   - Update `Models/Transaction.cs` to use TransactionType enum
   - Update `Models/Role.cs` to use RoleName enum
   - Update `Models/FareRule.cs` to use VehicleType/PassengerType enums

4. **Create Database Migration** (30 mins)
   - Run `dotnet ef migrations add AddEnums`
   - Run `dotnet ef database update`

### Phase 2: Critical Security Fix (1 hour)
**Priority**: Fix payment vulnerability FIRST

1. **Fix Payment Logic** (30 mins)
   - Update `PaymentService.cs` to remove amount parameter
   - Implement server-side fare determination
   - Add logging for manipulation attempts

2. **Update PaymentController** (15 mins)
   - Remove Amount from PaymentRequest
   - Update controller to use new DTOs

3. **Test Payment Flow** (15 mins)
   - Test legitimate payment
   - Test manipulation attempt (should fail)
   - Verify logging

### Phase 3: Service Layer Refactoring (2-3 hours)
**Priority**: Strong typing and validation

1. **Update AuthService** (1 hour)
   - Change return types to DTOs
   - Add input validation
   - Add logging
   - Add error handling

2. **Update PaymentService** (1 hour)
   - Change return types to DTOs
   - Add comprehensive validation
   - Add logging
   - Add error handling

3. **Update Controllers** (30 mins)
   - Update `AuthController.cs` to use new DTOs
   - Update `PaymentController.cs` to use new DTOs
   - Update return type annotations

### Phase 4: Replace Magic Strings (2-3 hours)
**Priority**: Code maintainability

1. **Update Services** (1 hour)
   - Replace all magic strings in `PaymentService.cs`
   - Replace all magic strings in `AuthService.cs`
   - Replace all magic strings in `TokenService.cs`

2. **Update Controllers** (1 hour)
   - Replace all magic strings in `AdminController.cs`
   - Replace all magic strings in `AuthController.cs`
   - Replace all magic strings in `PaymentController.cs`
   - Replace all magic strings in other controllers

3. **Update Program.cs** (30 mins)
   - Replace magic strings in seed data

4. **Update All Other Files** (30 mins)
   - Search for remaining magic strings
   - Replace with enums

### Phase 5: Error Handling & Logging (1-2 hours)
**Priority**: Observability

1. **Add Logging to All Services** (1 hour)
   - Add ILogger to all service constructors
   - Add info/warning/error logs
   - Add context data to logs

2. **Add Error Handling** (30 mins)
   - Add try-catch blocks to all database operations
   - Create custom exceptions
   - Add global exception handler

3. **Add Correlation IDs** (30 mins)
   - Implement correlation ID middleware
   - Add to all log entries

### Phase 6: Testing (2-3 hours)
**Priority**: Quality assurance

1. **Unit Tests** (1 hour)
   - Test validation logic
   - Test payment security (no fare override)
   - Test enum conversions

2. **Integration Tests** (1 hour)
   - Test complete payment flow
   - Test authentication flow
   - Test error scenarios

3. **Manual Testing** (30 mins)
   - Test API endpoints with Postman/Thunder Client
   - Verify error responses
   - Verify logging output

4. **Security Testing** (30 mins)
   - Attempt fare manipulation (should fail)
   - Test invalid inputs
   - Test SQL injection attempts

---

## Success Criteria

### Functional Requirements
- [ ] All service methods return strongly-typed DTOs (no `object` returns)
- [ ] All user inputs are validated before processing
- [ ] No magic strings remain in codebase (all replaced with enums)
- [ ] All database operations have proper error handling
- [ ] All errors are logged with context (user ID, card ID, etc.)
- [ ] Payment amount is ALWAYS server-determined (no client override)
- [ ] Fare manipulation attempts are logged as warnings
- [ ] All enums are used consistently throughout codebase

### Security Requirements
- [ ] Payment logic cannot be manipulated by clients
- [ ] All authentication attempts are logged
- [ ] Failed validation attempts are logged
- [ ] No sensitive data in logs (passwords, tokens)
- [ ] Correlation IDs for request tracing

### Code Quality Requirements
- [ ] All methods have XML documentation comments
- [ ] All parameters are validated
- [ ] All database calls are in try-catch blocks
- [ ] All services use ILogger
- [ ] No compiler warnings
- [ ] All tests pass

### Performance Requirements
- [ ] No N+1 query problems introduced
- [ ] Logging doesn't impact performance (use structured logging)
- [ ] Database queries are optimized

---

## Detailed File Changes

### Files to Create (11 new files)
1. `Enums/CardStatus.cs`
2. `Enums/TransactionType.cs`
3. `Enums/TransactionStatus.cs`
4. `Enums/RoleName.cs`
5. `Enums/VehicleType.cs`
6. `Enums/PassengerType.cs`
7. `DTOs/Common/ApiResponse.cs`
8. `DTOs/Auth/RegisterResponse.cs`
9. `DTOs/Auth/LoginResponse.cs`
10. `DTOs/Auth/RefreshTokenResponse.cs`
11. `DTOs/Payment/PaymentRequest.cs` (new, without Amount)
12. `DTOs/Payment/PaymentResponse.cs`

### Files to Modify (15+ files)
1. `Models/Card.cs` - Use CardStatus enum
2. `Models/Transaction.cs` - Use TransactionType enum
3. `Models/Role.cs` - Use RoleName enum
4. `Models/FareRule.cs` - Use VehicleType/PassengerType enums
5. `Services/AuthService.cs` - Strong typing, validation, logging, error handling
6. `Services/PaymentService.cs` - Strong typing, validation, logging, error handling, security fix
7. `Services/TokenService.cs` - Add logging
8. `Controllers/AuthController.cs` - Use DTOs, add logging
9. `Controllers/PaymentController.cs` - Remove Amount, use DTOs, add logging
10. `Controllers/AdminController.cs` - Use enums, add logging
11. `Controllers/TransactionsController.cs` - Use enums
12. `Controllers/CardsController.cs` - Use enums
13. `Controllers/FareRuleController.cs` - Use enums
14. `Program.cs` - Update seed data, add exception handler
15. `DTOs/` - Create all DTOs

---

## Breaking Changes Warning

### API Contract Changes
This refactoring will change API response structures:

**Before:**
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "token": "...",
    "refreshToken": "...",
    "user": { ... }
  }
}
```

**After:**
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "token": "...",
    "refreshToken": "...",
    "user": {
      "userId": 1,
      "firstName": "John",
      // ... strongly typed
    }
  }
}
```

**Action Required**: 
- Notify frontend teams of API contract changes
- Update frontend apps to parse new response structure
- Consider versioning API (e.g., `/api/v2/auth/login`)

---

## Rollback Plan

If issues arise after deployment:

1. **Database Rollback**:
   ```bash
   dotnet ef database update <previous-migration>
   ```

2. **Code Rollback**:
   ```bash
   git revert HEAD
   dotnet build
   dotnet publish
   ```

3. **API Versioning** (if implemented):
   - Keep old API version running
   - Switch traffic back to v1

---

## Testing Checklist

### Payment Security
- [ ] Legitimate payment with correct fare succeeds
- [ ] Attempt to override fare with `amount: 0.01` fails
- [ ] Attempt to override fare with `amount: 1000` fails
- [ ] Server logs manipulation attempts
- [ ] Correct fare is always charged

### Input Validation
- [ ] Negative amounts rejected
- [ ] Same origin/destination rejected
- [ ] Invalid card IDs rejected
- [ ] Invalid station IDs rejected
- [ ] Weak passwords rejected
- [ ] Invalid mobile numbers rejected

### Return Types
- [ ] All endpoints return strongly-typed responses
- [ ] No `object` types in responses
- [ ] Response structure is consistent
- [ ] Frontend can parse all responses

### Enums
- [ ] All magic strings replaced
- [ ] Enums work correctly in database
- [ ] Enum values are correct
- [ ] No string comparisons remain

### Error Handling
- [ ] Database errors are caught
- [ ] Errors are logged
- [ ] User-friendly error messages
- [ ] No stack traces exposed to clients

### Logging
- [ ] All errors are logged
- [ ] All warnings are logged
- [ ] Sensitive operations are logged
- [ ] Correlation IDs work
- [ ] Logs are structured

---

## Notes

- This is a refactoring task - no new features
- Database schema changes are minimal (enum conversions)
- Changes are backward compatible if done carefully
- Consider feature flags for gradual rollout
- Review all changes with security team for payment logic
- Update API documentation after changes
- Notify frontend teams before deployment

---

## References

- Current Task File: `TASK_BACKEND_IMPROVEMENTS.md`
- Project Root: `TransitPay-Prototype/`
- API Project: `TransitPay-Prototype/TransitPay.API/`
- Database Migrations: `TransitPay.API/Migrations/`