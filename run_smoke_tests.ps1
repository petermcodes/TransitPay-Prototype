<#
Run this script from the repository root to perform a guided runtime smoke test of TransitPay API.

Prerequisites (run on the machine where the API can access the PostgreSQL DB):
- dotnet SDK (matching project target)
- PowerShell 7+ recommended but works in Windows PowerShell with minor differences

IMPORTANT: This script does NOT modify source code. It starts the API using environment variables you must provide.
Do NOT run this against production database unless you understand the consequences.

Usage (examples):
# Use existing running API
.\run_smoke_tests.ps1 -ApiUrl 'http://localhost:5000' -SkipStartApi

# Auto-start API if not running
$env:DB_PASSWORD = 'YourDbPassword'
$env:JWT_KEY = '32+chars+at+least+32charslong123456'
$env:ADMIN_BOOTSTRAP_PASSWORD = 'Secur3AdminP@ss!'
.\run_smoke_tests.ps1 -ApiUrl 'http://localhost:5000'

The script will:
- Detect if API is already running and reuse it, or start it automatically
- Wait for /health to respond
- Execute representative requests for Auth, Passenger, Driver, Admin flows
- Decode QR payloads and assert no PAN-like sequences exist
- Print HTTP status codes and sample responses
- Generate a comprehensive pass/fail report with timing metrics

#>
param(
    [string] $ApiUrl = 'http://localhost:5000',
    [string] $ProjectPath = '.\TransitPay.API',
    [int] $HealthTimeoutSeconds = 180,
    [switch] $SkipStartApi,
    [int] $RequestTimeoutSeconds = 30
)

# ============================================================================
# GLOBAL STATE
# ============================================================================

$script:ApiProcess = $null
$script:ApiOutputLog = $null
$script:ApiErrorLog = $null
$script:TestResults = @{
    Passed = 0
    Failed = 0
    Skipped = 0
    Warnings = @()
    RequestDurations = @()
    SlowestEndpoint = @{ Name = ''; Duration = 0 }
    FastestEndpoint = @{ Name = ''; Duration = [long]::MaxValue }
}
$script:StartTime = Get-Date
$script:CurrentStep = 0
$script:TotalSteps = 10
$script:AuthToken = $null
$script:RefreshToken = $null
$script:AdminToken = $null
$script:TestMobileNumber = $null

# ============================================================================
# UTILITY FUNCTIONS
# ============================================================================

function Write-SectionHeader {
    param([string] $Title)
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
}

function Write-StepHeader {
    param(
        [int] $StepNumber,
        [string] $Operation,
        [string] $Endpoint,
        [string] $Method
    )
    
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Yellow
    Write-Host "[Step $StepNumber/$script:TotalSteps] $Operation" -ForegroundColor Yellow
    Write-Host "Endpoint: $Endpoint" -ForegroundColor Gray
    Write-Host "Method: $Method" -ForegroundColor Gray
    Write-Host "==================================================" -ForegroundColor Yellow
}

function Write-Success {
    param(
        [int] $StatusCode,
        [long] $DurationMs,
        [string] $Summary = ''
    )
    
    Write-Host "✓ PASS" -ForegroundColor Green
    Write-Host "Status Code: $StatusCode" -ForegroundColor Green
    Write-Host "Duration: $DurationMs ms" -ForegroundColor Green
    if ($Summary) {
        Write-Host "Summary: $Summary" -ForegroundColor Green
    }
}

function Write-Failure {
    param(
        [int] $StatusCode,
        [long] $DurationMs,
        [string] $Reason,
        [string] $Response = '',
        [string] $Recommendation = ''
    )
    
    Write-Host "✗ FAIL" -ForegroundColor Red
    Write-Host "Status Code: $StatusCode" -ForegroundColor Red
    Write-Host "Duration: $DurationMs ms" -ForegroundColor Red
    Write-Host "Reason: $Reason" -ForegroundColor Red
    if ($Response) {
        Write-Host "Response: $Response" -ForegroundColor Red
    }
    if ($Recommendation) {
        Write-Host "Recommendation: $Recommendation" -ForegroundColor Yellow
    }
}

function Update-Metrics {
    param(
        [string] $EndpointName,
        [long] $DurationMs,
        [bool] $Passed
    )
    
    $script:TestResults.RequestDurations += $DurationMs
    
    if ($Passed) {
        $script:TestResults.Passed++
    } else {
        $script:TestResults.Failed++
    }
    
    # Track slowest
    if ($DurationMs -gt $script:TestResults.SlowestEndpoint.Duration) {
        $script:TestResults.SlowestEndpoint = @{ Name = $EndpointName; Duration = $DurationMs }
    }
    
    # Track fastest
    if ($DurationMs -lt $script:TestResults.FastestEndpoint.Duration) {
        $script:TestResults.FastestEndpoint = @{ Name = $EndpointName; Duration = $DurationMs }
    }
}

function Write-ExecutionPlan {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║        TransitPay Smoke Test - Execution Plan                 ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "[1] Pre-flight Validation" -ForegroundColor White
    Write-Host "[2] API Detection / Startup" -ForegroundColor White
    Write-Host "[3] Health Check" -ForegroundColor White
    Write-Host "[4] Authentication Tests (register, login, refresh, logout)" -ForegroundColor White
    Write-Host "[5] Passenger Flow (cards/me, QR generation)" -ForegroundColor White
    Write-Host "[6] Driver Flow (stations, active trip, scan-physical)" -ForegroundColor White
    Write-Host "[7] Admin Flow (drivers list)" -ForegroundColor White
    Write-Host "[8] QR Validation (decode and PAN check)" -ForegroundColor White
    Write-Host "[9] Cleanup" -ForegroundColor White
    Write-Host "[10] Summary Report" -ForegroundColor White
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
}

# ============================================================================
# CENTRALIZED HTTP REQUEST HANDLER
# ============================================================================

function Invoke-ApiRequest {
    param(
        [Parameter(Mandatory)][ValidateSet('GET','POST','PUT','DELETE')][string] $Method,
        [Parameter(Mandatory)][string] $Url,
        [string] $Token = $null,
        [object] $Body = $null,
        [string] $StepName = '',
        [int] $TimeoutSeconds = 30
    )
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $headers = @{}
    
    if ($Token) {
        $headers['Authorization'] = "Bearer $Token"
    }
    
    $bodyJson = $null
    if ($Body) {
        $bodyJson = $Body | ConvertTo-Json -Depth 10
    }
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $headers
            UseBasicParsing = $true
            TimeoutSec = $TimeoutSeconds
            ErrorAction = 'Stop'
        }
        
        if ($bodyJson) {
            $params.Body = $bodyJson
            $params.ContentType = 'application/json'
        }
        
        $response = Invoke-WebRequest @params
        $stopwatch.Stop()
        
        $content = $response.Content
        $parsed = $null
        try {
            if ($content) {
                $parsed = $content | ConvertFrom-Json
            }
        } catch {
            $parsed = $content
        }
        
        $result = [PSCustomObject]@{
            StatusCode = $response.StatusCode.Value__
            Body = $parsed
            Raw = $content
            DurationMs = $stopwatch.ElapsedMilliseconds
            Success = $true
        }
        
        return $result
        
    } catch {
        $stopwatch.Stop()
        $err = $_.Exception
        
        # Try to get response details
        $statusCode = 0
        $responseBody = ''
        
        if ($err -is [System.Net.WebException] -and $err.Response) {
            try {
                $stream = $err.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($stream)
                $responseBody = $reader.ReadToEnd()
                $statusCode = [int]$err.Response.StatusCode
            } catch {
                $responseBody = 'Unable to read response'
            }
        } elseif ($err -is [System.Net.Http.HttpRequestException]) {
            $statusCode = 0
            $responseBody = $err.Message
        } else {
            $statusCode = 0
            $responseBody = $err.Message
        }
        
        $result = [PSCustomObject]@{
            StatusCode = $statusCode
            Body = $responseBody
            Raw = $responseBody
            DurationMs = $stopwatch.ElapsedMilliseconds
            Success = $false
            Error = $err
        }
        
        return $result
    }
}

# ============================================================================
# PRE-FLIGHT VALIDATION
# ============================================================================

function Test-Prerequisites {
    param([string] $ApiUrl)
    
    $script:CurrentStep++
    Write-SectionHeader "[Step $script:CurrentStep/$script:TotalSteps] PRE-FLIGHT VALIDATION"
    
    $allPassed = $true
    
    # Check dotnet
    Write-Host "Checking dotnet SDK..." -NoNewline
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        Write-Host " ✓" -ForegroundColor Green
    } else {
        Write-Host " ✗" -ForegroundColor Red
        Write-Host "ERROR: dotnet SDK not found. Please install .NET SDK." -ForegroundColor Red
        $allPassed = $false
    }
    
    # Check environment variables
    $requiredEnv = @('DB_PASSWORD', 'JWT_KEY', 'ADMIN_BOOTSTRAP_PASSWORD')
    foreach ($name in $requiredEnv) {
        Write-Host "Checking environment variable `$name..." -NoNewline
        $value = [System.Environment]::GetEnvironmentVariable($name)
        if ($value) {
            Write-Host " ✓" -ForegroundColor Green
        } else {
            Write-Host " ✗" -ForegroundColor Red
            Write-Host "ERROR: Environment variable `$name is not set." -ForegroundColor Red
            $allPassed = $false
        }
    }
    
    # Check API reachability
    Write-Host "Checking API reachability at $ApiUrl..." -NoNewline
    try {
        $healthUrl = "$ApiUrl/health"
        $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host " ✓ (HTTP $($response.StatusCode))" -ForegroundColor Green
            Write-Host "INFO: Existing API detected. Using running instance. Skipping startup." -ForegroundColor Green
        } else {
            Write-Host " ✗ (HTTP $($response.StatusCode))" -ForegroundColor Red
            Write-Host "ERROR: API health check returned non-200 status." -ForegroundColor Red
            $allPassed = $false
        }
    } catch {
        Write-Host " ✗" -ForegroundColor Yellow
        Write-Host "INFO: API not reachable. Will attempt to start." -ForegroundColor Yellow
    }
    
    if (-not $allPassed) {
        Write-Host ""
        Write-Host "Pre-flight validation failed. Aborting." -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    Write-Host "Pre-flight validation passed. ✓" -ForegroundColor Green
}

# ============================================================================
# API STARTUP
# ============================================================================

function Start-Api {
    param(
        [string] $ProjectPath,
        [string] $ApiUrl
    )
    
    $script:CurrentStep++
    Write-SectionHeader "[Step $script:CurrentStep/$script:TotalSteps] API STARTUP"
    
    # Resolve project path
    $scriptRoot = $PSScriptRoot
    if (-not $scriptRoot) {
        $scriptRoot = (Get-Location -PSProvider FileSystem).Path
    }
    
    $resolvedProjectPath = $ProjectPath
    if (-not [System.IO.Path]::IsPathRooted($resolvedProjectPath)) {
        $resolvedProjectPath = Join-Path $scriptRoot $resolvedProjectPath
    }
    
    try {
        $resolvedProjectPath = (Resolve-Path -Path $resolvedProjectPath -ErrorAction Stop).Path
    } catch {
        Write-Host "ERROR: Project path not found: $resolvedProjectPath" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Starting TransitPay API (project: $resolvedProjectPath)..." -ForegroundColor Yellow
    Write-Host "URL: $ApiUrl" -ForegroundColor Gray
    
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = 'dotnet'
    $startInfo.Arguments = "run --project `"$resolvedProjectPath`" --urls $ApiUrl"
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.EnvironmentVariables['ASPNETCORE_ENVIRONMENT'] = 'Development'
    $startInfo.EnvironmentVariables['DB_PASSWORD'] = $env:DB_PASSWORD
    $startInfo.EnvironmentVariables['JWT_KEY'] = $env:JWT_KEY
    $startInfo.EnvironmentVariables['ADMIN_BOOTSTRAP_PASSWORD'] = $env:ADMIN_BOOTSTRAP_PASSWORD
    $startInfo.WorkingDirectory = $scriptRoot
    
    $script:ApiOutputLog = New-Object System.Text.StringBuilder
    $script:ApiErrorLog = New-Object System.Text.StringBuilder
    
    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $startInfo
    $proc.EnableRaisingEvents = $true
    
    $proc.add_OutputDataReceived({ 
        param($sender, $e) 
        if ($e.Data) { 
            $null = $script:ApiOutputLog.AppendLine($e.Data) 
        }
    })
    $proc.add_ErrorDataReceived({ 
        param($sender, $e) 
        if ($e.Data) { 
            $null = $script:ApiErrorLog.AppendLine($e.Data) 
        }
    })
    
    $null = $proc.Start()
    $proc.BeginOutputReadLine()
    $proc.BeginErrorReadLine()
    
    $script:ApiProcess = $proc
    Write-Host "API process started (PID: $($proc.Id))" -ForegroundColor Green
}

# ============================================================================
# HEALTH CHECK
# ============================================================================

function Wait-For-Health {
    param(
        [string] $Url,
        [int] $TimeoutSeconds,
        [System.Diagnostics.Process] $Process = $null
    )
    
    $script:CurrentStep++
    Write-SectionHeader "[Step $script:CurrentStep/$script:TotalSteps] HEALTH CHECK"
    
    $end = (Get-Date).AddSeconds($TimeoutSeconds)
    $attempt = 0
    $healthUrl = "$Url/health"
    
    Write-Host "Waiting for API to become healthy (timeout: ${TimeoutSeconds}s)..." -ForegroundColor Yellow
    
    while ((Get-Date) -lt $end) {
        $attempt++
        
        # Check if process exited
        if ($Process -and $Process.HasExited) {
            Write-Host ""
            Write-Host "ERROR: API process exited prematurely. Exit code: $($Process.ExitCode)" -ForegroundColor Red
            Write-Host ""
            Write-Host "Last 50 lines of output:" -ForegroundColor Yellow
            $out = $script:ApiOutputLog.ToString().Split("`n") | Select-Object -Last 50
            $err = $script:ApiErrorLog.ToString().Split("`n") | Select-Object -Last 50
            if ($out) { $out | ForEach-Object { Write-Host $_ } }
            if ($err) { $err | ForEach-Object { Write-Host $_ } }
            return $false
        }
        
        try {
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $resp = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            $stopwatch.Stop()
            
            if ($resp.StatusCode -eq 200) {
                Write-Host ""
                Write-Host "✓ Health check passed (HTTP 200)" -ForegroundColor Green
                Write-Host "Attempts: $attempt" -ForegroundColor Gray
                Write-Host "Duration: $($stopwatch.ElapsedMilliseconds) ms" -ForegroundColor Gray
                return $true
            }
        } catch {
            # Ignore and retry
        }
        
        # Exponential backoff: 2s, 3s, 5s, then 5s thereafter
        $delay = if ($attempt -le 2) { 2 } elseif ($attempt -le 4) { 3 } else { 5 }
        Write-Host "Attempt $attempt failed, retrying in $delay seconds..." -ForegroundColor Yellow
        Start-Sleep -Seconds $delay
    }
    
    Write-Host ""
    Write-Host "ERROR: Health check timed out after $TimeoutSeconds seconds" -ForegroundColor Red
    return $false
}

# ============================================================================
# AUTHENTICATION TESTS
# ============================================================================

function Test-AuthFlows {
    param(
        [string] $ApiUrl,
        [int] $TimeoutSeconds
    )
    
    $script:CurrentStep++
    Write-SectionHeader "[Step $script:CurrentStep/$script:TotalSteps] AUTHENTICATION TESTS"
    
    # Step 1: Register new passenger
    $script:CurrentStep++
    $mobile = "+999$(Get-Random -Minimum 100000000 -Maximum 999999999)"
    $script:TestMobileNumber = $mobile
    $endpoint = "$ApiUrl/api/auth/register"
    
    Write-StepHeader -StepNumber $script:CurrentStep -Operation "Register new passenger" -Endpoint $endpoint -Method "POST"
    
    $registerPayload = @{
        firstName = 'Smoke'
        lastName = 'Tester'
        mobileNumber = $mobile
        password = 'Sm0keT3st!Abc'
    }
    
    $result = Invoke-ApiRequest -Method POST -Url $endpoint -Body $registerPayload -StepName "Register" -TimeoutSeconds $TimeoutSeconds
    $duration = $result.DurationMs
    
    if ($result.Success -and $result.StatusCode -eq 200) {
        Write-Success -StatusCode $result.StatusCode -DurationMs $duration -Summary "Passenger registered successfully"
        Update-Metrics -EndpointName "POST /api/auth/register" -DurationMs $duration -Passed $true
    } else {
        $reason = if ($result.Body) { $result.Body.message } else { 'Unknown error' }
        Write-Failure -StatusCode $result.StatusCode -DurationMs $duration -Reason $reason -Response $result.Raw -Recommendation "Check registration validation rules"
        Update-Metrics -EndpointName "POST /api/auth/register" -DurationMs $duration -Passed $false
        return $false
    }
    
    # Step 2: Login
    $script:CurrentStep++
    $endpoint = "$ApiUrl/api/auth/login"
    
    Write-StepHeader -StepNumber $script:CurrentStep -Operation "Login passenger" -Endpoint $endpoint -Method "POST"
    
    $loginPayload = @{
        mobileNumber = $mobile
        password = 'Sm0keT3st!Abc'
    }
    
    $result = Invoke-ApiRequest -Method POST -Url $endpoint -Body $loginPayload -StepName "Login" -TimeoutSeconds $TimeoutSeconds
    $duration = $result.DurationMs
    
    if ($result.Success -and $result.StatusCode -eq 200 -and $result.Body.data.token) {
        Write-Success -StatusCode $result.StatusCode -DurationMs $duration -Summary "JWT token acquired"
        $script:AuthToken = $result.Body.data.token
        $script:RefreshToken = $result.Body.data.refreshToken
        Update-Metrics -EndpointName "POST /api/auth/login" -DurationMs $duration -Passed $true
    } else {
        $reason = if ($result.Body) { $result.Body.message } else { 'Login failed' }
        Write-Failure -StatusCode $result.StatusCode -DurationMs $duration -Reason $reason -Response $result.Raw -Recommendation "Verify credentials and user exists"
        Update-Metrics -EndpointName "POST /api/auth/login" -DurationMs $duration -Passed $false
        return $false
    }
    
    # Step 3: Refresh token
    $script:CurrentStep++
    $endpoint = "$ApiUrl/api/auth/refresh"
    
    Write-StepHeader -StepNumber $script:CurrentStep -Operation "Refresh JWT token" -Endpoint $endpoint -Method "POST"
    
    $refreshPayload = @{
        userId = $result.Body.data.user.userId
        refreshToken = $script:RefreshToken
    }
    
    $result = Invoke-ApiRequest -Method POST -Url $endpoint -Body $refreshPayload -StepName "Refresh" -TimeoutSeconds $TimeoutSeconds
    $duration = $result.DurationMs
    
    if ($result.Success -and $result.StatusCode -eq 200 -and $result.Body.data.token) {
        Write-Success -StatusCode $result.StatusCode -DurationMs $duration -Summary "Token refreshed successfully"
        $script:AuthToken = $result.Body.data.token
        Update-Metrics -EndpointName "POST /api/auth/refresh" -DurationMs $duration -Passed $true
    } else {
        $reason = if ($result.Body) { $result.Body.message } else { 'Token refresh failed' }
        Write-Failure -StatusCode $result.StatusCode -DurationMs $duration -Reason $reason -Response $result.Raw -Recommendation "Check refresh token validity"
        Update-Metrics -EndpointName "POST /api/auth/refresh" -DurationMs $duration -Passed $false
        return $false
    }
    
    # Step 4: Logout
    $script:CurrentStep++
    $endpoint = "$ApiUrl/api/auth/logout"
    
    Write-StepHeader -StepNumber $script:CurrentStep -Operation "Logout passenger" -Endpoint $endpoint -Method "POST"
    
    $result = Invoke-ApiRequest -Method POST -Url $endpoint -Token $script:AuthToken -StepName "Logout" -TimeoutSeconds $TimeoutSeconds
    $duration = $result.DurationMs
    
    if ($result.Success -and $result.StatusCode -eq 200) {
        Write-Success -StatusCode $result.StatusCode -DurationMs $duration -Summary "Logout successful"
        Update-Metrics -EndpointName "POST /api/auth/logout" -DurationMs $duration -Passed $true
    } else {
        $reason = if ($result.Body) { $result.Body.message } else { 'Logout failed' }
        Write-Failure -StatusCode $result.StatusCode -DurationMs $duration -Reason $reason -Response $result.Raw
        Update-Metrics -EndpointName "POST /api/auth/logout" -DurationMs $duration -Passed $false
        # Don't fail on logout - it's not critical
    }
    
    return $true
}

# ============================================================================
# PASSENGER FLOW TESTS
# ============================================================================

function Test-PassengerFlows {
    param(
        [string] $ApiUrl,
        [int] $TimeoutSeconds
    )
    
    $script:CurrentStep++
    Write-SectionHeader "[Step $script:CurrentStep/$script:TotalSteps] PASSENGER FLOW TESTS"
    
    # Step 5: Get cards/me
    $script:CurrentStep++
    $endpoint = "$ApiUrl/api/cards/me"
    
    Write-StepHeader -StepNumber $script:CurrentStep -Operation "Get authenticated user's card" -Endpoint $endpoint -Method "GET"
    
    $result = Invoke-ApiRequest -Method GET -Url $endpoint -Token $script:AuthToken -StepName "Cards/Me" -TimeoutSeconds $TimeoutSeconds
    $duration = $result.DurationMs
    
    if ($result.Success -and $result.StatusCode -eq 200) {
        Write-Success -StatusCode $result.StatusCode -DurationMs $duration -Summary "Card retrieved"
        Update-Metrics -EndpointName "GET /api/cards/me" -DurationMs $duration -Passed $true
        
        # PAN check
        if ($result.Raw -match '\b\d{12,19}\b') {
            Write-Host "WARNING: PAN-like sequence detected in cards/me response" -ForegroundColor Yellow
            $script:TestResults.Warnings += "PAN-like sequence in cards/me response"
        }
    } else {
        $reason = if ($result.Body) { $result.Body.message } else { 'Failed to get card' }
        Write-Failure -StatusCode $result.StatusCode -DurationMs $duration -Reason $reason -Response $result.Raw -Recommendation "Ensure user has a card assigned"
        Update-Metrics -EndpointName "GET /api/cards/me" -DurationMs $duration -Passed $false
        return $false
    }
    
    # Step 6: Get QR for card
    $script:CurrentStep++
    $endpoint = "$ApiUrl/api/payment/qr/1"
    
    Write-StepHeader -StepNumber $script:CurrentStep -Operation "Get QR code for card" -Endpoint $endpoint -Method "GET"
    
    $result = Invoke-ApiRequest -Method GET -Url $endpoint -Token $script:AuthToken -StepName "Get QR" -TimeoutSeconds $TimeoutSeconds
    $duration = $result.DurationMs
    
    if ($result.Success -and $result.StatusCode -eq 200) {
        Write-Success -StatusCode $result.StatusCode -DurationMs $duration -Summary "QR code retrieved"
        Update-Metrics -EndpointName "GET /api/payment/qr/{cardId}" -DurationMs $duration -Passed $true
        
        # Decode and validate QR
        if ($result.Body.data -and $result.Body.data.data) {
            try {
                $decoded = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($result.Body.data.data))
                Write-Host "QR payload decoded successfully" -ForegroundColor Gray
                
                # PAN check on decoded QR
                if ($decoded -match '\b\d{12,19}\b') {
                    Write-Host "WARNING: PAN-like sequence detected in QR payload" -ForegroundColor Yellow
                    $script:TestResults.Warnings += "PAN-like sequence in QR payload"
                }
            } catch {
                Write-Host "WARNING: Could not decode QR payload" -ForegroundColor Yellow
                $script:TestResults.Warnings += "QR decode failed"
            }
        }
    } else {
        $reason = if ($result.Body) { $result.Body.message } else { 'Failed to get QR' }
        Write-Failure -StatusCode $result.StatusCode -DurationMs $duration -Reason $reason -Response $result.Raw -Recommendation "Ensure card with ID 1 exists"
        Update-Metrics -EndpointName "GET /api/payment/qr/{cardId}" -DurationMs $duration -Passed $false
        return $false
    }
    
    return $true
}

# ============================================================================
# DRIVER FLOW TESTS
# ============================================================================

function Test-DriverFlows {
    param(
        [string] $ApiUrl,
        [int] $TimeoutSeconds
    )
    
    $script:CurrentStep++
    Write-SectionHeader "[Step $script:CurrentStep/$script:TotalSteps] DRIVER FLOW TESTS"
    
    # Step 7: Get stations (admin endpoint, accessible with passenger token for testing)
    $script:CurrentStep++
    $endpoint = "$ApiUrl/api/admin/stations"
    
    Write-StepHeader -StepNumber $script:CurrentStep -Operation "Get stations list" -Endpoint $endpoint -Method "GET"
    
    $result = Invoke-ApiRequest -Method GET -Url $endpoint -Token $script:AuthToken -StepName "Get Stations" -TimeoutSeconds $TimeoutSeconds
    $duration = $result.DurationMs
    
    if ($result.Success -and $result.StatusCode -eq 200) {
        Write-Success -StatusCode $result.StatusCode -DurationMs $duration -Summary "Stations retrieved"
        Update-Metrics -EndpointName "GET /api/admin/stations" -DurationMs $duration -Passed $true
    } else {
        $reason = if ($result.Body) { $result.Body.message } else { 'Failed to get stations' }
        Write-Failure -StatusCode $result.StatusCode -DurationMs $duration -Reason $reason -Response $result.Raw -Recommendation "Check admin authorization"
        Update-Metrics -EndpointName "GET /api/admin/stations" -DurationMs $duration -Passed $false
        return $false
    }
    
    # Step 8: Get active trip
    $script:CurrentStep++
    $endpoint = "$ApiUrl/api/Trip/active"
    
    Write-StepHeader -StepNumber $script:CurrentStep -Operation "Get active trip" -Endpoint $endpoint -Method "GET"
    
    $result = Invoke-ApiRequest -Method GET -Url $endpoint -Token $script:AuthToken -StepName "Active Trip" -TimeoutSeconds $TimeoutSeconds
    $duration = $result.DurationMs
    
    if ($result.Success) {
        Write-Success -StatusCode $result.StatusCode -DurationMs $duration -Summary "Active trip check completed"
        Update-Metrics -EndpointName "GET /api/Trip/active" -DurationMs $duration -Passed $true
    } else {
        $reason = if ($result.Body) { $result.Body.message } else { 'Failed to get active trip' }
        Write-Failure -StatusCode $result.StatusCode -DurationMs $duration -Reason $reason -Response $result.Raw
        Update-Metrics -EndpointName "GET /api/Trip/active" -DurationMs $duration -Passed $false
        return $false
    }
    
    # Step 9: Scan physical card
    $script:CurrentStep++
    $endpoint = "$ApiUrl/api/payment/scan-physical"
    
    Write-StepHeader -StepNumber $script:CurrentStep -Operation "Scan physical card payment" -Endpoint $endpoint -Method "POST"
    
    $scanPayload = @{
        CardNumber = '4111111111111111'
        OriginStationId = 1
        DestinationStationId = 2
    }
    
    $result = Invoke-ApiRequest -Method POST -Url $endpoint -Token $script:AuthToken -Body $scanPayload -StepName "Scan Physical" -TimeoutSeconds $TimeoutSeconds
    $duration = $result.DurationMs
    
    if ($result.Success) {
        Write-Success -StatusCode $result.StatusCode -DurationMs $duration -Summary "Physical card scan processed"
        Update-Metrics -EndpointName "POST /api/payment/scan-physical" -DurationMs $duration -Passed $true
        
        # PAN check
        if ($result.Raw -match '\b\d{12,19}\b') {
            Write-Host "WARNING: PAN-like sequence detected in scan-physical response" -ForegroundColor Yellow
            $script:TestResults.Warnings += "PAN-like sequence in scan-physical response"
        }
    } else {
        $reason = if ($result.Body) { $result.Body.message } else { 'Physical card scan failed' }
        Write-Failure -StatusCode $result.StatusCode -DurationMs $duration -Reason $reason -Response $result.Raw -Recommendation "Check card validation and payment processing"
        Update-Metrics -EndpointName "POST /api/payment/scan-physical" -DurationMs $duration -Passed $false
        return $false
    }
    
    return $true
}

# ============================================================================
# ADMIN FLOW TESTS
# ============================================================================

function Test-AdminFlows {
    param(
        [string] $ApiUrl,
        [int] $TimeoutSeconds
    )
    
    $script:CurrentStep++
    Write-SectionHeader "[Step $script:CurrentStep/$script:TotalSteps] ADMIN FLOW TESTS"
    
    # Step 10: Admin login
    $script:CurrentStep++
    $endpoint = "$ApiUrl/api/auth/login"
    
    Write-StepHeader -StepNumber $script:CurrentStep -Operation "Login as admin" -Endpoint $endpoint -Method "POST"
    
    $adminLoginPayload = @{
        mobileNumber = '0000000000'
        password = $env:ADMIN_BOOTSTRAP_PASSWORD
    }
    
    $result = Invoke-ApiRequest -Method POST -Url $endpoint -Body $adminLoginPayload -StepName "Admin Login" -TimeoutSeconds $TimeoutSeconds
    $duration = $result.DurationMs
    
    if ($result.Success -and $result.StatusCode -eq 200 -and $result.Body.data.token) {
        Write-Success -StatusCode $result.StatusCode -DurationMs $duration -Summary "Admin JWT acquired"
        $script:AdminToken = $result.Body.data.token
        Update-Metrics -EndpointName "POST /api/auth/login (admin)" -DurationMs $duration -Passed $true
    } else {
        $reason = if ($result.Body) { $result.Body.message } else { 'Admin login failed' }
        Write-Failure -StatusCode $result.StatusCode -DurationMs $duration -Reason $reason -Response $result.Raw -Recommendation "Check ADMIN_BOOTSTRAP_PASSWORD and admin user seed state"
        Update-Metrics -EndpointName "POST /api/auth/login (admin)" -DurationMs $duration -Passed $false
        $script:TestResults.Warnings += "Admin login failed - admin endpoints skipped"
        return $false
    }
    
    # Step 11: Get drivers list
    $script:CurrentStep++
    $endpoint = "$ApiUrl/api/admin/drivers"
    
    Write-StepHeader -StepNumber $script:CurrentStep -Operation "Get drivers list (admin)" -Endpoint $endpoint -Method "GET"
    
    $result = Invoke-ApiRequest -Method GET -Url $endpoint -Token $script:AdminToken -StepName "Get Drivers" -TimeoutSeconds $TimeoutSeconds
    $duration = $result.DurationMs
    
    if ($result.Success -and $result.StatusCode -eq 200) {
        Write-Success -StatusCode $result.StatusCode -DurationMs $duration -Summary "Drivers list retrieved"
        Update-Metrics -EndpointName "GET /api/admin/drivers" -DurationMs $duration -Passed $true
        
        # PAN check
        if ($result.Raw -match '\b\d{12,19}\b') {
            Write-Host "WARNING: PAN-like sequence detected in drivers list" -ForegroundColor Yellow
            $script:TestResults.Warnings += "PAN-like sequence in drivers list"
        }
    } else {
        $reason = if ($result.Body) { $result.Body.message } else { 'Failed to get drivers' }
        Write-Failure -StatusCode $result.StatusCode -DurationMs $duration -Reason $reason -Response $result.Raw -Recommendation "Check admin authorization"
        Update-Metrics -EndpointName "GET /api/admin/drivers" -DurationMs $duration -Passed $false
        return $false
    }
    
    return $true
}

# ============================================================================
# SUMMARY REPORT
# ============================================================================

function Write-SummaryReport {
    $script:CurrentStep++
    Write-SectionHeader "[Step $script:CurrentStep/$script:TotalSteps] SUMMARY REPORT"
    
    $endTime = Get-Date
    $totalRuntime = $endTime - $script:StartTime
    $totalSeconds = [math]::Round($totalRuntime.TotalSeconds, 1)
    
    $totalRequests = $script:TestResults.Passed + $script:TestResults.Failed
    $avgDuration = 0
    if ($totalRequests -gt 0 -and $script:TestResults.RequestDurations.Count -gt 0) {
        $avgDuration = [math]::Round(($script:TestResults.RequestDurations | Measure-Object -Average).Average, 0)
    }
    
    $slowestName = $script:TestResults.SlowestEndpoint.Name
    $slowestDuration = $script:TestResults.SlowestEndpoint.Duration
    $fastestName = $script:TestResults.FastestEndpoint.Name
    $fastestDuration = $script:TestResults.FastestEndpoint.Duration
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "         TransitPay Smoke Test Summary" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Passed:       $($script:TestResults.Passed)" -ForegroundColor Green
    
    if ($script:TestResults.Failed -gt 0) {
        Write-Host "Failed:       $($script:TestResults.Failed)" -ForegroundColor Red
    } else {
        Write-Host "Failed:       $($script:TestResults.Failed)" -ForegroundColor Green
    }
    
    Write-Host "Skipped:      $($script:TestResults.Skipped)" -ForegroundColor Yellow
    
    if ($script:TestResults.Warnings.Count -gt 0) {
        Write-Host "Warnings:     $($script:TestResults.Warnings.Count)" -ForegroundColor Yellow
    } else {
        Write-Host "Warnings:     $($script:TestResults.Warnings.Count)" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Total Runtime:    $totalSeconds s" -ForegroundColor Cyan
    Write-Host "Average Request:  $avgDuration ms" -ForegroundColor Cyan
    if ($slowestName) {
        Write-Host "Slowest:          $slowestName ($slowestDuration ms)" -ForegroundColor Yellow
    }
    if ($fastestName -and $fastestDuration -ne [long]::MaxValue) {
        Write-Host "Fastest:          $fastestName ($fastestDuration ms)" -ForegroundColor Green
    }
    Write-Host ""
    
    if ($script:TestResults.Failed -gt 0) {
        Write-Host "Failed Tests:" -ForegroundColor Red
        Write-Host "(See detailed output above for failure reasons)" -ForegroundColor Red
        Write-Host ""
    }
    
    if ($script:TestResults.Warnings.Count -gt 0) {
        Write-Host "Warnings:" -ForegroundColor Yellow
        foreach ($warning in $script:TestResults.Warnings) {
            Write-Host "  • $warning" -ForegroundColor Yellow
        }
        Write-Host ""
    }
    
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    
    if ($script:TestResults.Failed -eq 0) {
        Write-Host "Result: PASS ✓" -ForegroundColor Green
    } else {
        Write-Host "Result: FAIL ✗" -ForegroundColor Red
    }
    
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
    # Return exit code
    if ($script:TestResults.Failed -gt 0) {
        exit 1
    } else {
        exit 0
    }
}

# ============================================================================
# CLEANUP
# ============================================================================

function Invoke-Cleanup {
    $script:CurrentStep++
    Write-SectionHeader "[Step $script:CurrentStep/$script:TotalSteps] CLEANUP"
    
    if ($script:ApiProcess -and -not $script:ApiProcess.HasExited) {
        Write-Host "Stopping API process (PID: $($script:ApiProcess.Id))..." -ForegroundColor Yellow
        try {
            $script:ApiProcess.Kill()
            $script:ApiProcess.WaitForExit(5000) | Out-Null
            Write-Host "API process stopped." -ForegroundColor Green
        } catch {
            Write-Host "WARNING: Could not stop API process gracefully." -ForegroundColor Yellow
        }
    }
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

function Main {
    param(
        [string] $ApiUrl,
        [string] $ProjectPath,
        [int] $HealthTimeoutSeconds,
        [switch] $SkipStartApi,
        [int] $RequestTimeoutSeconds
    )
    
    # Display execution plan
    Write-ExecutionPlan
    
    # Pre-flight validation
    Test-Prerequisites -ApiUrl $ApiUrl
    
    # Check if we need to start API
    $apiAlreadyRunning = $false
    try {
        $healthUrl = "$ApiUrl/health"
        $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $apiAlreadyRunning = $true
        }
    } catch {
        $apiAlreadyRunning = $false
    }
    
    # Start API if needed
    if (-not $apiAlreadyRunning) {
        if ($SkipStartApi) {
            Write-Host ""
            Write-Host "ERROR: API not reachable at $ApiUrl and -SkipStartApi specified." -ForegroundColor Red
            Write-Host "Cannot proceed without API. Aborting." -ForegroundColor Red
            exit 2
        }
        
        Start-Api -ProjectPath $ProjectPath -ApiUrl $ApiUrl
        
        # Wait for health
        $healthy = Wait-For-Health -Url $ApiUrl -TimeoutSeconds $HealthTimeoutSeconds -Process $script:ApiProcess
        if (-not $healthy) {
            Write-Host ""
            Write-Host "ERROR: API failed to become healthy in time." -ForegroundColor Red
            Invoke-Cleanup
            exit 2
        }
    } else {
        Write-Host ""
        Write-Host "Using existing API instance at $ApiUrl" -ForegroundColor Green
    }
    
    # Run test suites
    $authPassed = Test-AuthFlows -ApiUrl $ApiUrl -TimeoutSeconds $RequestTimeoutSeconds
    if (-not $authPassed) {
        Write-Host ""
        Write-Host "CRITICAL: Authentication failed. Cannot proceed with dependent tests." -ForegroundColor Red
        Invoke-Cleanup
        Write-SummaryReport
    }
    
    $passengerPassed = Test-PassengerFlows -ApiUrl $ApiUrl -TimeoutSeconds $RequestTimeoutSeconds
    
    $driverPassed = Test-DriverFlows -ApiUrl $ApiUrl -TimeoutSeconds $RequestTimeoutSeconds
    
    $adminPassed = Test-AdminFlows -ApiUrl $ApiUrl -TimeoutSeconds $RequestTimeoutSeconds
    
    # Cleanup
    Invoke-Cleanup
    
    # Summary report
    Write-SummaryReport
}

# ============================================================================
# SCRIPT ENTRY POINT
# ============================================================================

try {
    Main -ApiUrl $ApiUrl -ProjectPath $ProjectPath -HealthTimeoutSeconds $HealthTimeoutSeconds -SkipStartApi:$SkipStartApi -RequestTimeoutSeconds $RequestTimeoutSeconds
} catch {
    Write-Host ""
    Write-Host "FATAL ERROR: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Invoke-Cleanup
    exit 99
}