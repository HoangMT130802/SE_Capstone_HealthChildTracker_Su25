@echo off
echo 🔐 Encrypting Firebase Service Account Credentials...
echo ================================================

cd /d "%~dp0"

echo 📁 Current directory: %CD%
echo.

echo 🛠️  Compiling encryption tool...
csc /target:exe /out:Tools\EncryptCredentials.exe Tools\EncryptCredentials.cs

if %ERRORLEVEL% NEQ 0 (
    echo ❌ Compilation failed!
    pause
    exit /b 1
)

echo ✅ Compilation successful!
echo.

echo 🔐 Running encryption...
Tools\EncryptCredentials.exe

echo.
echo 🗑️  Cleaning up...
del Tools\EncryptCredentials.exe

echo.
echo ✅ Encryption process completed!
echo 📋 Next steps:
echo    1. Add config/ folder to .gitignore if not already done
echo    2. Verify that config/encrypted-firebase.dat exists
echo    3. The real credentials are now safely encrypted
echo.
pause
