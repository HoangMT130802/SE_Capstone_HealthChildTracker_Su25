# Hướng dẫn kiểm tra Push Notification

## Bước 1: Khởi động server
```bash
dotnet run --project KidTracking.API
```

## Bước 2: Kiểm tra Firebase connection
Mở trình duyệt hoặc Postman, gọi:

**URL:** `https://localhost:7000/api/TestPush/check-firebase`
**Method:** POST
**Headers:** Content-Type: application/json

Kết quả mong đợi:
```json
{
  "status": "success",
  "message": "Firebase connected successfully",
  "messageId": "some-firebase-message-id"
}
```

## Bước 3: Test với FCM token thật từ mobile
**URL:** `https://localhost:7000/api/TestPush/test-real-token`
**Method:** POST
**Headers:** Content-Type: application/json
**Body:**
```json
{
  "token": "YOUR_FCM_TOKEN_HERE"
}
```

## Bước 4: Kiểm tra logs
Trong console server, tìm các dòng log:
- `Firebase initialized with credentials file`
- `Firebase messaging initialized successfully`
- `Push notification sent successfully`

## Bước 5: Test device token registration
**URL:** `https://localhost:7000/api/DeviceToken/register`
**Method:** POST
**Headers:** 
- Content-Type: application/json
- Authorization: Bearer YOUR_JWT_TOKEN
**Body:**
```json
{
  "token": "YOUR_FCM_TOKEN",
  "deviceType": "android",
  "deviceInfo": "{\"modelName\":\"Pixel\",\"osName\":\"Android\",\"osVersion\":\"14\"}"
}
```

## Troubleshooting

### 1. Nếu Firebase không kết nối được:
- Kiểm tra file `Repositories/kidtrack-78a49-firebase-adminsdk-fbsvc-3d449b285c.json` có tồn tại không
- Kiểm tra project ID trong appsettings.json: `kidtrack-78a49`

### 2. Nếu token không valid:
- Frontend cần dùng `Notifications.getDevicePushTokenAsync()` thay vì `getExpoPushTokenAsync()`
- Token FCM sẽ có dạng: `fA1B2c3D4e5F...` (không có prefix `ExponentPushToken`)

### 3. Kiểm tra notification history:
**URL:** `https://localhost:7000/api/NotificationHistory/my-notifications`
**Method:** GET
**Headers:** Authorization: Bearer YOUR_JWT_TOKEN

### 4. Background service logs:
Server sẽ chạy background service mỗi ngày để gửi vaccine reminders tự động.
Kiểm tra logs có dòng: `Starting daily vaccine reminders process`

## Mobile app cần làm:

1. **Lấy FCM token:**
```javascript
import * as Notifications from 'expo-notifications';

const getFCMToken = async () => {
  const token = await Notifications.getDevicePushTokenAsync();
  return token.data; // Đây là FCM token
};
```

2. **Register token với backend:**
```javascript
const registerToken = async (fcmToken) => {
  await fetch('https://localhost:7000/api/DeviceToken/register', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${userToken}`
    },
    body: JSON.stringify({
      token: fcmToken,
      deviceType: 'android', // hoặc 'ios'
      deviceInfo: JSON.stringify({
        modelName: Device.modelName,
        osName: Device.osName,
        osVersion: Device.osVersion
      })
    })
  });
};
```

3. **Configure notification handling:**
```javascript
Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: false,
  }),
});
```

