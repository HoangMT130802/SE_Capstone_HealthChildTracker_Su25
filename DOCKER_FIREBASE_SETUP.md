# 🐳 Docker Firebase Setup - SIÊU ĐỠN GIẢN

## 🎯 **CHO NGƯỜI DEPLOY:**

### **Bước 1: Lấy Firebase JSON**
1. Vào [Firebase Console](https://console.firebase.google.com) → Project `kidtrack-78a49`
2. **Project Settings** → **Service Accounts** → **Generate new private key**
3. Download file JSON và **copy toàn bộ nội dung**

### **Bước 2: Chạy với Docker**

**🚀 Cách 1: Docker run**
```bash
# Thay <FIREBASE_JSON_CONTENT> bằng nội dung file JSON
docker run -d \
  -p 8080:80 \
  -e FIREBASE_ACCOUNT_SERVICE='<FIREBASE_JSON_CONTENT>' \
  your-app:latest
```

**🚀 Cách 2: Docker Compose**
```yaml
# docker-compose.yml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "8080:80"
    environment:
      - FIREBASE_ACCOUNT_SERVICE=<FIREBASE_JSON_CONTENT>
    restart: unless-stopped
```

```bash
# Chạy
docker-compose up -d
```

### **Bước 3: Kiểm tra**
```bash
curl http://localhost:8080/api/TestPush/firebase-status
```

**✅ Expected response:**
```json
{
  "status": "success", 
  "message": "Firebase initialized successfully",
  "initMethod": "environment_variable"
}
```

---

## 🔥 **VÍ DỤ CỤ THỂ:**

**File JSON sẽ trông như này:**
```json
{
  "type": "service_account",
  "project_id": "kidtrack-78a49",
  "private_key_id": "abc123...",
  "private_key": "-----BEGIN PRIVATE KEY-----\nMIIEvQ...\n-----END PRIVATE KEY-----\n",
  "client_email": "firebase-adminsdk-xyz@kidtrack-78a49.iam.gserviceaccount.com",
  "client_id": "123456789",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/firebase-adminsdk-xyz%40kidtrack-78a49.iam.gserviceaccount.com"
}
```

**Command cuối cùng:**
```bash
docker run -d \
  -p 8080:80 \
  -e FIREBASE_ACCOUNT_SERVICE='{"type":"service_account","project_id":"kidtrack-78a49","private_key_id":"abc123...","private_key":"-----BEGIN PRIVATE KEY-----\nMIIEvQ...\n-----END PRIVATE KEY-----\n","client_email":"firebase-adminsdk-xyz@kidtrack-78a49.iam.gserviceaccount.com",...}' \
  your-app:latest
```

---

## ⚠️ **LƯU Ý:**
- **KHÔNG** commit file JSON vào Git
- **LUÔN** dùng environment variable
- **CHỈ** cần set biến `FIREBASE_ACCOUNT_SERVICE` → app tự hoạt động!

**🎉 Xong! Firebase push notification sẽ hoạt động ngay!**
