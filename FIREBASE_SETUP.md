# 🔐 Firebase Credentials Security Setup

## Tổng quan
Hệ thống sử dụng Firebase Admin SDK với Service Account để xác thực. Để bảo mật credentials khỏi GitHub và các repository công khai, chúng ta sử dụng mã hóa AES.

## 🚀 Cách sử dụng

### 1. Chạy tool mã hóa credentials

**Windows:**
```bash
.\encrypt-firebase.bat
```

**Linux/Mac/PowerShell:**
```bash
pwsh encrypt-firebase.ps1
```

### 2. Kết quả
- File `config/encrypted-firebase.dat` sẽ được tạo với credentials đã mã hóa
- Credentials thật được ẩn khỏi GitHub
- Hệ thống tự động decrypt khi runtime

## 🔒 Bảo mật

### Những gì được bảo vệ:
- ✅ **Private Key**: Hoàn toàn được mã hóa
- ✅ **Client ID**: Không hiển thị trong source code  
- ✅ **Project ID**: An toàn khỏi GitHub scanners
- ✅ **Service Account Email**: Được ẩn

### Những gì GitHub thấy:
- ❌ **Không có private keys**
- ❌ **Không có client secrets**
- ❌ **Không có service account details**
- ✅ **Chỉ có placeholder fallback**

## 🛠️ Cách hoạt động

### 1. Encryption Process:
```
Real Firebase JSON → AES Encryption → encrypted-firebase.dat
```

### 2. Runtime Process:
```
Application Start → Read encrypted-firebase.dat → AES Decrypt → Use Firebase Admin SDK
```

### 3. Fallback Process:
```
Missing encrypted file → Use placeholder credentials (sẽ fail khi thực tế)
```

## 📁 Cấu trúc Files

```
project/
├── Services/Config/EncryptedConfig.cs     # Encryption logic
├── Tools/EncryptCredentials.cs            # Encryption tool
├── config/encrypted-firebase.dat          # Encrypted credentials (ignored by git)
├── encrypt-firebase.bat                   # Windows script
├── encrypt-firebase.ps1                   # PowerShell script
└── .gitignore                             # Ignores sensitive files
```

## ⚠️ Lưu ý quan trọng

1. **File `config/encrypted-firebase.dat` KHÔNG được commit lên GitHub**
2. **Tool encryption chỉ chạy locally**
3. **Mỗi lần deploy production cần chạy lại encryption**
4. **Backup encrypted file ở nơi an toàn**

## 🔧 Troubleshooting

### Lỗi: "Encrypted config file not found"
```bash
# Chạy lại tool encryption
.\encrypt-firebase.bat
```

### Lỗi: "Failed to decrypt Firebase credentials"
```bash
# Kiểm tra file có tồn tại không
ls config/encrypted-firebase.dat

# Chạy lại encryption nếu file bị corrupt
.\encrypt-firebase.bat
```

### Lỗi: Firebase Authentication failed
- Kiểm tra Service Account JSON có đúng không
- Verify Project ID trong Firebase Console
- Đảm bảo Service Account có quyền Admin SDK

## 🔑 Encryption Details

- **Algorithm**: AES-256
- **Key**: `KidTracker2024!@` (16 chars)
- **IV**: Zero IV (simplified for this use case)
- **Mode**: ECB (suitable for single-use credentials)

## 🚀 Production Deployment

1. Deploy application code (không có credentials)
2. Chạy encryption tool trên production server
3. Restart application để load encrypted credentials
4. Verify Firebase connection

## 📞 Support

Nếu có vấn đề về Firebase credentials:
1. Kiểm tra file `.gitignore` có ignore `config/` không
2. Verify encryption tool chạy thành công
3. Check application logs cho Firebase errors
