# 🔥 Firebase Production Setup - AN TOÀN

## ❌ **VẤN ĐỀ:** 
GitHub/Git Guardian sẽ chặn nếu commit Firebase credentials vào code!

## ✅ **GIẢI PHÁP AN TOÀN:**

---

## 🔒 **METHOD 1: Environment Variables (KHUYẾN NGHỊ)**

### **Bước 1: Lấy Firebase Service Account JSON**
1. Vào [Firebase Console](https://console.firebase.google.com)
2. Chọn project `kidtrack-78a49`
3. **Project Settings** → **Service Accounts**
4. **Generate new private key** → Download JSON

### **Bước 2: Setup Production Server**

**🖥️ Trên Production Server:**
```bash
# Tạo environment variable với toàn bộ nội dung JSON
export FIREBASE_SERVICE_ACCOUNT='{"type":"service_account","project_id":"kidtrack-78a49","private_key_id":"...","private_key":"-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n","client_email":"firebase-adminsdk-xxx@kidtrack-78a49.iam.gserviceaccount.com",...}'

# Hoặc đặt vào file .env
echo 'FIREBASE_SERVICE_ACCOUNT={"type":"service_account",...}' >> /app/.env
```

**⚙️ Với Docker:**
```dockerfile
# Dockerfile
ENV FIREBASE_SERVICE_ACCOUNT=""

# Khi chạy container
docker run -e FIREBASE_SERVICE_ACCOUNT='{"type":"service_account",...}' your-app
```

**☁️ Với Cloud Platforms:**

**Azure App Service:**
```bash
# Thêm Application Setting
az webapp config appsettings set --name your-app --resource-group your-rg --settings FIREBASE_SERVICE_ACCOUNT='{"type":"service_account",...}'
```

**AWS Elastic Beanstalk:**
```bash
# Thêm Environment Variable qua AWS Console
# Configuration → Software → Environment properties
# Key: FIREBASE_SERVICE_ACCOUNT
# Value: {"type":"service_account",...}
```

**Heroku:**
```bash
heroku config:set FIREBASE_SERVICE_ACCOUNT='{"type":"service_account",...}' -a your-app
```

### **Bước 3: Kiểm tra hoạt động**
```bash
# Test endpoint
curl https://your-api.com/api/TestPush/firebase-status

# Expected response:
{
  "status": "success", 
  "message": "Firebase initialized successfully",
  "initMethod": "environment_variable"
}
```

---

## 🔒 **METHOD 2: Secure File Upload**

### **Bước 1: Setup .gitignore**
```gitignore
# Thêm vào .gitignore
**/firebase-credentials.json
**/firebase-*.json
**/*firebase*key*.json
```

### **Bước 2: Upload file riêng lẻ**
```bash
# Trên production server, tạo file credentials
sudo mkdir -p /app/Repositories
sudo nano /app/Repositories/firebase-credentials.json

# Paste nội dung JSON file, save và exit
# Set quyền chỉ đọc
sudo chmod 600 /app/Repositories/firebase-credentials.json
sudo chown app:app /app/Repositories/firebase-credentials.json
```

### **Bước 3: Update Production Config**
```json
// appsettings.Production.json
{
  "Firebase": {
    "CredentialsPath": "/app/Repositories/firebase-credentials.json",
    "ServiceAccountJson": "",
    "ProjectId": "kidtrack-78a49"
  }
}
```

---

## 🔒 **METHOD 3: Azure Key Vault / AWS Secrets**

### **Azure Key Vault:**
```csharp
// Trong Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-vault.vault.azure.net/"),
    new DefaultAzureCredential()
);

// Tạo secret "firebase-service-account" trong Key Vault
// Code sẽ tự động load từ configuration["firebase-service-account"]
```

### **AWS Secrets Manager:**
```json
// Tạo secret với key "firebase-service-account"
// Value là toàn bộ JSON content
```

---

## 🧪 **TESTING**

### **Local Development:**
```bash
# Tạo file local (không commit)
cp downloaded-firebase-key.json Repositories/firebase-credentials.json

# Test
dotnet run
curl http://localhost:7000/api/TestPush/firebase-status
```

### **Production:**
```bash
# Method 1: Environment Variable
export FIREBASE_SERVICE_ACCOUNT='...'
dotnet run --environment Production

# Method 2: File path  
dotnet run --environment Production
curl https://your-api.com/api/TestPush/firebase-status
```

---

## 🚨 **SECURITY CHECKLIST:**

- [ ] ❌ **KHÔNG BAO GIỜ** commit file `.json` chứa private key
- [ ] ✅ Thêm `firebase-credentials.json` vào `.gitignore`
- [ ] ✅ Sử dụng Environment Variables cho production
- [ ] ✅ Set file permissions `600` nếu dùng file approach
- [ ] ✅ Rotate keys định kỳ (6 tháng)
- [ ] ✅ Monitor Firebase usage để phát hiện leak
- [ ] ✅ Sử dụng different service accounts cho dev/prod

---

## 🎯 **WORKFLOW DEPLOY:**

```bash
# 1. Code commit (KHÔNG có credentials)
git add .
git commit -m "Add FCM support"
git push origin main

# 2. Trên production server
export FIREBASE_SERVICE_ACCOUNT='{"type":"service_account",...}'
systemctl restart your-app

# 3. Verify
curl https://your-api.com/api/TestPush/firebase-status
```

**✅ Với cách này, code sẽ deploy an toàn mà không bị GitHub chặn!**
