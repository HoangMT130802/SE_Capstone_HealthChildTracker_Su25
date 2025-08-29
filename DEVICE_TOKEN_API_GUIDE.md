# 📱 Hướng dẫn Device Token API

## 🔗 **Base URL**
```
https://localhost:7008/api/devicetoken
```

## 🛡️ **Authentication**
Tất cả API đều yêu cầu JWT token trong header:
```
Authorization: Bearer <your-jwt-token>
```

---

## 📋 **1. Đăng ký Device Token**

### **POST** `/register`

Đăng ký device token cho user hiện tại.

**Request Body:**
```json
{
  "token": "firebase-device-token-here",
  "deviceType": "Android",
  "deviceInfo": "Samsung Galaxy S21, Android 12"
}
```

**Response Success (200):**
```json
{
  "success": true,
  "message": "Device token registered successfully",
  "data": {
    "deviceTokenId": 1,
    "token": "firebase-device-token-here",
    "deviceType": "Android",
    "deviceInfo": "Samsung Galaxy S21, Android 12",
    "isActive": true,
    "createdAt": "2025-01-27T10:30:00Z",
    "lastUsedAt": "2025-01-27T10:30:00Z"
  }
}
```

**Response Conflict (409):**
```json
{
  "success": false,
  "message": "This device is already registered to another account",
  "data": {
    "conflictAccountIds": [2, 5]
  }
}
```

### **⚠️ Chính sách "1 Device = 1 Account"**
- Nếu device token đã được đăng ký bởi account khác → trả về lỗi 409
- Client cần xử lý conflict bằng API `/check-conflict` và `/transfer`

---

## 🗑️ **2. Xóa Device Token**

### **DELETE** `/remove`

Xóa device token của user hiện tại.

**Request Body:**
```json
{
  "token": "firebase-device-token-here"
}
```

**Response Success (200):**
```json
{
  "success": true,
  "message": "Device token removed successfully"
}
```

**Response Not Found (404):**
```json
{
  "success": false,
  "message": "Device token not found for current user"
}
```

---

## 📋 **3. Lấy danh sách Device Token**

### **GET** `/user-tokens`

Lấy tất cả device token của user hiện tại.

**Response Success (200):**
```json
{
  "success": true,
  "message": "User device tokens retrieved successfully",
  "data": [
    {
      "deviceTokenId": 1,
      "token": "firebase-device-token-1",
      "deviceType": "Android",
      "deviceInfo": "Samsung Galaxy S21",
      "isActive": true,
      "createdAt": "2025-01-27T10:30:00Z",
      "lastUsedAt": "2025-01-27T10:30:00Z"
    },
    {
      "deviceTokenId": 2,
      "token": "firebase-device-token-2",
      "deviceType": "iOS",
      "deviceInfo": "iPhone 13 Pro",
      "isActive": false,
      "createdAt": "2025-01-26T15:20:00Z",
      "lastUsedAt": "2025-01-26T18:45:00Z"
    }
  ]
}
```

---

## ⚔️ **4. Kiểm tra Token Conflict**

### **POST** `/check-conflict`

Kiểm tra xem device token có bị conflict với account khác không.

**Request Body:**
```json
{
  "token": "firebase-device-token-here"
}
```

**Response No Conflict (200):**
```json
{
  "success": true,
  "message": "No conflict found",
  "data": {
    "hasConflict": false,
    "conflictAccountIds": []
  }
}
```

**Response Has Conflict (409):**
```json
{
  "success": false,
  "message": "Token conflict detected",
  "data": {
    "hasConflict": true,
    "conflictAccountIds": [2, 5]
  }
}
```

---

## 🔄 **5. Chuyển Device Token**

### **POST** `/transfer`

Chuyển device token từ account khác sang account hiện tại.

**Request Body:**
```json
{
  "token": "firebase-device-token-here"
}
```

**Response Success (200):**
```json
{
  "success": true,
  "message": "Device token transferred successfully",
  "data": {
    "deviceTokenId": 1,
    "token": "firebase-device-token-here",
    "deviceType": "Android",
    "deviceInfo": "Samsung Galaxy S21",
    "isActive": true,
    "createdAt": "2025-01-27T10:30:00Z",
    "lastUsedAt": "2025-01-27T10:30:00Z"
  }
}
```

---

## 🧹 **6. Cleanup Token không hoạt động**

### **POST** `/cleanup-inactive`

Xóa tất cả device token không hoạt động (admin only).

**Request Body:**
```json
{
  "daysInactive": 30
}
```

**Response Success (200):**
```json
{
  "success": true,
  "message": "Cleaned up 5 inactive device tokens"
}
```

---

## 📱 **Flow sử dụng trong Mobile App**

### **1. Khi user đăng nhập lần đầu:**
```javascript
// 1. Lấy Firebase token
const firebaseToken = await messaging().getToken();

// 2. Đăng ký device token
const response = await fetch('/api/devicetoken/register', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    token: firebaseToken,
    deviceType: Platform.OS === 'ios' ? 'iOS' : 'Android',
    deviceInfo: `${DeviceInfo.getBrand()} ${DeviceInfo.getModel()}`
  })
});

if (response.status === 409) {
  // Xử lý conflict - có thể hiển thị dialog cho user
  const conflictData = await response.json();
  // Gọi API transfer nếu user đồng ý
}
```

### **2. Xử lý Token Conflict:**
```javascript
// Kiểm tra conflict trước
const checkResponse = await fetch('/api/devicetoken/check-conflict', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ token: firebaseToken })
});

if (checkResponse.status === 409) {
  // Hiển thị dialog: "Thiết bị này đã được đăng ký với tài khoản khác. Bạn có muốn chuyển sang tài khoản hiện tại?"
  if (userConfirmed) {
    await fetch('/api/devicetoken/transfer', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${jwtToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ token: firebaseToken })
    });
  }
}
```

### **3. Khi user đăng xuất:**
```javascript
const firebaseToken = await messaging().getToken();

await fetch('/api/devicetoken/remove', {
  method: 'DELETE',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ token: firebaseToken })
});
```

---

## 🔍 **Error Codes**

| Code | Meaning | Action |
|------|---------|--------|
| 200 | Success | Tiếp tục |
| 400 | Bad Request | Kiểm tra request body |
| 401 | Unauthorized | Đăng nhập lại |
| 404 | Not Found | Token không tồn tại |
| 409 | Conflict | Xử lý conflict với `/transfer` |
| 500 | Server Error | Thử lại sau |

---

## 🔧 **Testing với HTTP File**

Sử dụng file `KidTracking.API/test-firebase.http` để test:

```http
### Register Device Token
POST https://localhost:7008/api/devicetoken/register
Authorization: Bearer {{jwt_token}}
Content-Type: application/json

{
  "token": "test-firebase-token-123",
  "deviceType": "Android",
  "deviceInfo": "Samsung Galaxy S21, Android 12"
}

### Check Token Conflict
POST https://localhost:7008/api/devicetoken/check-conflict
Authorization: Bearer {{jwt_token}}
Content-Type: application/json

{
  "token": "test-firebase-token-123"
}
```

---

## 📋 **Best Practices**

1. **Always check conflict** trước khi register token
2. **Handle 409 errors** gracefully với user-friendly messages
3. **Remove token** khi user logout
4. **Refresh token** khi Firebase token thay đổi
5. **Store device info** để dễ identify trong admin panel
6. **Use meaningful device names**: "iPhone 13 Pro - iOS 15.1" thay vì chỉ "iOS"

---

## 🚨 **Security Notes**

- Token được mask trong logs để bảo mật
- Chỉ user sở hữu token mới có thể xóa/chuyển
- Admin có thể cleanup inactive tokens
- JWT token phải valid cho tất cả operations

