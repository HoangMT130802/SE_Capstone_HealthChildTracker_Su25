# Test script cho Children Controller
$baseUrl = "https://localhost:7001"

Write-Host "=== 1. Test Login để lấy token ===" -ForegroundColor Green
$loginBody = @{
    AccountName = "mthhoang"
    Password = "123456"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Authentication/login" -Method POST -ContentType "application/json" -Body $loginBody
$token = $loginResponse.token

Write-Host "Token length: $($token.Length)" -ForegroundColor Yellow
Write-Host "Token preview: $($token.Substring(0, [Math]::Min(50, $token.Length)))..." -ForegroundColor Yellow

# Decode JWT để xem claims (optional)
Write-Host "`n=== 2. Test No-Auth endpoint ===" -ForegroundColor Green
try {
    $noAuthResponse = Invoke-RestMethod -Uri "$baseUrl/api/Children/debug/no-auth" -Method GET
    Write-Host "No-Auth Response: $($noAuthResponse | ConvertTo-Json -Depth 2)" -ForegroundColor Cyan
} catch {
    Write-Host "No-Auth Error: $_" -ForegroundColor Red
}

Write-Host "`n=== 3. Test Simple Auth endpoint ===" -ForegroundColor Green
try {
    $headers = @{ Authorization = "Bearer $token" }
    $simpleAuthResponse = Invoke-RestMethod -Uri "$baseUrl/api/Children/debug/simple-auth" -Method GET -Headers $headers
    Write-Host "Simple-Auth Response: $($simpleAuthResponse | ConvertTo-Json -Depth 2)" -ForegroundColor Cyan
} catch {
    Write-Host "Simple-Auth Error: $_" -ForegroundColor Red
}

Write-Host "`n=== 4. Test Full Auth endpoint (test-auth) ===" -ForegroundColor Green
try {
    $headers = @{ Authorization = "Bearer $token" }
    $testAuthResponse = Invoke-RestMethod -Uri "$baseUrl/api/Children/debug/test-auth" -Method GET -Headers $headers
    Write-Host "Test-Auth Response: $($testAuthResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Cyan
} catch {
    Write-Host "Test-Auth Error: $_" -ForegroundColor Red
}

Write-Host "`n=== 5. Test Get My Children ===" -ForegroundColor Green
try {
    $headers = @{ Authorization = "Bearer $token" }
    $childrenResponse = Invoke-RestMethod -Uri "$baseUrl/api/Children/my-children" -Method GET -Headers $headers
    Write-Host "My-Children Response: $($childrenResponse | ConvertTo-Json -Depth 2)" -ForegroundColor Cyan
} catch {
    Write-Host "My-Children Error: $_" -ForegroundColor Red
}

Write-Host "`n=== 6. Test Create Child ===" -ForegroundColor Green
$childData = @{
    FirstName = "Test"
    LastName = "Child"
    DateOfBirth = "2023-01-01T00:00:00Z"
    Gender = "Male"
    BloodType = "O+"
    MedicalNotes = "Test child creation"
} | ConvertTo-Json

try {
    $headers = @{ Authorization = "Bearer $token" }
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/Children" -Method POST -ContentType "application/json" -Headers $headers -Body $childData
    Write-Host "Create-Child Response: $($createResponse | ConvertTo-Json -Depth 2)" -ForegroundColor Cyan
} catch {
    Write-Host "Create-Child Error: $_" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Yellow
}

Write-Host "`n=== Test Completed ===" -ForegroundColor Green 