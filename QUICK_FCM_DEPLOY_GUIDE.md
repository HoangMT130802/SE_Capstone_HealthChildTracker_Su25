# 🚀 HƯỚNG DẪN DEPLOY FCM NHANH

## 🎯 **3 BƯỚC ĐỂ DEPLOY FCM AN TOÀN:**

### **1️⃣ LẤY FIREBASE CREDENTIALS**
```bash
# Vào: https://console.firebase.google.com
# → Project "kidtrack-78a49" 
# → Settings → Service Accounts
# → Generate new private key
# → Download JSON file
```

### **2️⃣ SET ENVIRONMENT VARIABLE** 
```bash
# Trên production server, set biến môi trường:
export FIREBASE_SERVICE_ACCOUNT='PASTE_TOÀN_BỘ_JSON_CONTENT_VÀO_ĐÂY'

# Ví dụ:
export FIREBASE_SERVICE_ACCOUNT='{"type":"service_account","project_id":"kidtrack-78a49","private_key_id":"abc123","private_key":"-----BEGIN PRIVATE KEY-----\nMIIE...","client_email":"firebase-adminsdk-xyz@kidtrack-78a49.iam.gserviceaccount.com","client_id":"123","auth_uri":"https://accounts.google.com/o/oauth2/auth","token_uri":"https://oauth2.googleapis.com/token"}'
```

### **3️⃣ DEPLOY & TEST**
```bash
# Deploy code (credentials KHÔNG trong code)
git add .
git commit -m "Add FCM with environment variable support"
git push

# Test trên production:
curl https://your-api.com/api/TestPush/firebase-status

# Expected response:
{"status":"success","message":"Firebase initialized successfully"}
```

---

## 🔧 **PLATFORM-SPECIFIC SETUP:**

### **☁️ Azure App Service:**
```bash
az webapp config appsettings set --name your-app --resource-group your-rg --settings FIREBASE_SERVICE_ACCOUNT='{"type":"service_account",...}'
```

### **🐳 Docker:**
```bash
docker run -e FIREBASE_SERVICE_ACCOUNT='{"type":"service_account",...}' your-image
```

### **⚡ Heroku:**
```bash
heroku config:set FIREBASE_SERVICE_ACCOUNT='{"type":"service_account",...}' -a your-app
```

### **🖥️ VPS/Dedicated Server:**
```bash
# Thêm vào ~/.bashrc hoặc /etc/environment
echo 'export FIREBASE_SERVICE_ACCOUNT='"'"'{"type":"service_account",...}'"'"'' >> ~/.bashrc
source ~/.bashrc
```

---

## 🧪 **KIỂM TRA HOẠT ĐỘNG:**

### **1. Test Firebase Status:**
```bash
curl https://your-api.com/api/TestPush/firebase-status
```

### **2. Test Push thật với FCM Token:**
```bash
curl -X POST https://your-api.com/api/TestPush/test-real-token \
  -H "Content-Type: application/json" \
  -d '{"token":"YOUR_REAL_FCM_TOKEN_FROM_MOBILE_APP"}'
```

### **3. Test Registration API:**
```bash
curl -X POST https://your-api.com/api/devicetoken/register \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "token":"FCM_TOKEN_FROM_MOBILE",
    "deviceType":"android",
    "deviceInfo":"{\"model\":\"Samsung\",\"os\":\"Android 12\"}"
  }'
```

---

## ⚠️ **LƯU Ý QUAN TRỌNG:**

### **✅ AN TOÀN:**
- ✅ Credentials được lưu trong Environment Variable
- ✅ Code commit KHÔNG chứa sensitive data  
- ✅ GitHub/Git Guardian KHÔNG chặn
- ✅ `.gitignore` đã protect `*firebase*.json`

### **🔍 DEBUG:**
```bash
# Check environment variable được set chưa:
echo $FIREBASE_SERVICE_ACCOUNT

# Check logs khi start app:
tail -f /var/log/your-app.log | grep Firebase
```

### **📱 MOBILE APP:**
```javascript
// Frontend PHẢI dùng FCM Token (KHÔNG phải Expo Token)
const { data: fcmToken } = await Notifications.getDevicePushTokenAsync();
// ✅ fcmToken: "fGxqX-8kQB6m..."
// ❌ KHÔNG dùng: getExpoPushTokenAsync()
```

---

## 🎉 **HOÀN THÀNH!**

Sau khi setup xong, bạn có thể:
- ✅ Deploy code mà không lo Git Guardian chặn
- ✅ Gửi push notifications từ backend  
- ✅ Test FCM với mobile app thật
- ✅ Scale production một cách an toàn

**Mọi thứ sẽ hoạt động ngay! 🚀**
