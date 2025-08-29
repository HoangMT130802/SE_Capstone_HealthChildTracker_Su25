#!/usr/bin/env pwsh

Write-Host "🔐 Encrypting Firebase Service Account Credentials..." -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host

$currentDir = Get-Location
Write-Host "📁 Current directory: $currentDir" -ForegroundColor Yellow
Write-Host

try {
    Write-Host "🛠️  Compiling encryption tool..." -ForegroundColor Yellow
    
    # Create Tools directory if it doesn't exist
    if (-not (Test-Path "Tools")) {
        New-Item -ItemType Directory -Path "Tools" | Out-Null
    }
    
    # Compile the C# tool
    csc /target:exe /out:Tools/EncryptCredentials.exe Tools/EncryptCredentials.cs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Compilation failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "✅ Compilation successful!" -ForegroundColor Green
    Write-Host
    
    Write-Host "🔐 Running encryption..." -ForegroundColor Yellow
    & "Tools/EncryptCredentials.exe"
    
    Write-Host
    Write-Host "🗑️  Cleaning up..." -ForegroundColor Yellow
    Remove-Item "Tools/EncryptCredentials.exe" -ErrorAction SilentlyContinue
    
    Write-Host
    Write-Host "✅ Encryption process completed!" -ForegroundColor Green
    Write-Host "📋 Next steps:" -ForegroundColor Cyan
    Write-Host "   1. Add config/ folder to .gitignore if not already done" -ForegroundColor White
    Write-Host "   2. Verify that config/encrypted-firebase.dat exists" -ForegroundColor White
    Write-Host "   3. The real credentials are now safely encrypted" -ForegroundColor White
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host
Read-Host "Press Enter to continue"
