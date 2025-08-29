# 🔥 FCM Token - Hướng dẫn cho Frontend

## ❌ **VẤN ĐỀ HIỆN TẠI:**
FE đang gửi **Expo Token**, nhưng Backend cần **FCM Token**!

```javascript
// ❌ SAI - Expo Token
"ExponentPushToken[1h8CfmNgROHEoP_o9wbnUq]"
"ExponentPushToken_1h8CfmNgROHEoP_o9wbnUq" 

// ✅ ĐÚNG - FCM Token  
"fGxqX-8kQB6mKVTz9wQ_Ej:APA91bH8vL2kJ9xYzABC123def456GHI789jkl"
```

---

## 🎯 **CÁCH LẤY FCM TOKEN ĐÚNG:**

### **📱 Method 1: Expo với FCM (RECOMMENDED)**

```javascript
import * as Notifications from 'expo-notifications';
import * as Device from 'expo-device';
import { Platform } from 'react-native';

// ✅ LẤY FCM TOKEN (KHÔNG PHẢI EXPO TOKEN)
async function getFCMToken() {
  try {
    // Request permissions
    const { status: existingStatus } = await Notifications.getPermissionsAsync();
    let finalStatus = existingStatus;
    
    if (existingStatus !== 'granted') {
      const { status } = await Notifications.requestPermissionsAsync();
      finalStatus = status;
    }
    
    if (finalStatus !== 'granted') {
      console.log('Push notification permission denied');
      return null;
    }

    // ✅ LẤY FCM TOKEN - QUAN TRỌNG!
    const { data: fcmToken } = await Notifications.getDevicePushTokenAsync();
    
    console.log('✅ FCM Token:', fcmToken);
    // Output: "fGxqX-8kQB6mKVTz9wQ_Ej:APA91bH8vL2kJ9..."
    
    return fcmToken;
  } catch (error) {
    console.error('❌ Error getting FCM token:', error);
    return null;
  }
}

// ❌ ĐỪNG DÙNG CÁI NÀY (Expo Token)
async function getExpoTokenWRONG() {
  const { data: expoToken } = await Notifications.getExpoPushTokenAsync();
  // ❌ Output: "ExponentPushToken[...]" - KHÔNG hoạt động với FCM!
  return expoToken;
}

// ✅ ĐĂNG KÝ DEVICE TOKEN
async function registerDeviceToken() {
  const fcmToken = await getFCMToken();
  
  if (!fcmToken) {
    console.error('❌ Cannot get FCM token');
    return;
  }

  const deviceData = {
    token: fcmToken, // ✅ FCM Token
    deviceType: Platform.OS, // "android" hoặc "ios"
    deviceInfo: JSON.stringify({
      modelName: Device.modelName,
      osName: Device.osName, 
      osVersion: Device.osVersion,
      manufacturer: Device.manufacturer,
      isDevice: Device.isDevice
    })
  };

  try {
    const response = await fetch('https://localhost:7008/api/device-token/register', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${userJwtToken}`
      },
      body: JSON.stringify(deviceData)
    });

    if (response.ok) {
      console.log('✅ Device token registered successfully');
    } else {
      console.error('❌ Failed to register device token:', response.status);
    }
  } catch (error) {
    console.error('❌ Network error:', error);
  }
}
```

### **📱 Method 2: React Native Firebase**

```javascript
import messaging from '@react-native-firebase/messaging';

async function getFCMTokenFirebase() {
  try {
    // Request permission
    const authStatus = await messaging().requestPermission();
    const enabled = 
      authStatus === messaging.AuthorizationStatus.AUTHORIZED ||
      authStatus === messaging.AuthorizationStatus.PROVISIONAL;

    if (enabled) {
      // ✅ LẤY FCM TOKEN
      const fcmToken = await messaging().getToken();
      console.log('✅ FCM Token:', fcmToken);
      // Output: "fGxqX-8kQB6mKVTz9wQ_Ej:APA91bH8vL2kJ9..."
      
      return fcmToken;
    }
  } catch (error) {
    console.error('❌ Error getting FCM token:', error);
    return null;
  }
}
```

---

## 🔍 **NHẬN BIẾT TOKEN ĐÚNG:**

### **✅ FCM Token (ĐÚNG):**
- **Length**: 150-200 ký tự
- **Format**: `[a-zA-Z0-9_-]+:[a-zA-Z0-9_-]+`
- **Contains**: Dấu `:` và `APA91b`
- **Example**: 
  ```
  fGxqX-8kQB6mKVTz9wQ_Ej:APA91bH8vL2kJ9xYzABC123def456GHI789jkl
  ```

### **❌ Expo Token (SAI):**
- **Format**: `ExponentPushToken[xxx]` hoặc `ExponentPushToken_xxx`
- **Examples**:
  ```
  ExponentPushToken[1h8CfmNgROHEoP_o9wbnUq]  ❌
  ExponentPushToken_1h8CfmNgROHEoP_o9wbnUq   ❌
  ```

---

## 📋 **REQUEST BODY MẪU:**

### **✅ ĐÚNG:**
```json
{
  "token": "fGxqX-8kQB6mKVTz9wQ_Ej:APA91bH8vL2kJ9xYzABC123def456GHI789jkl",
  "deviceType": "android",
  "deviceInfo": "{\"modelName\":\"sdk_gphone64_x86_64\",\"osName\":\"Android\",\"osVersion\":\"16\",\"manufacturer\":\"Google\",\"isDevice\":false}"
}
```

### **❌ SAI:**
```json
{
  "token": "ExponentPushToken[1h8CfmNgROHEoP_o9wbnUq]",
  "deviceType": "android",
  "deviceInfo": "..."
}
```

---

## 🚀 **TESTING:**

```javascript
// ✅ Test FCM token format
function isValidFCMToken(token) {
  // FCM token chứa dấu ':' và 'APA91b'
  return token && 
         typeof token === 'string' && 
         token.includes(':') && 
         token.includes('APA91b') &&
         token.length > 100;
}

// Usage
const token = await getFCMToken();
if (isValidFCMToken(token)) {
  console.log('✅ Valid FCM token');
  await registerDeviceToken();
} else {
  console.log('❌ Invalid token format');
}
```

---

## ⚡ **TÓM TẮT:**

1. **❌ ĐỪNG dùng**: `Notifications.getExpoPushTokenAsync()`
2. **✅ PHẢI dùng**: `Notifications.getDevicePushTokenAsync()` 
3. **✅ FCM Token format**: `xxx:APA91bxxx`
4. **❌ Expo Token format**: `ExponentPushToken[xxx]`

**FE cần thay đổi code để lấy FCM token thay vì Expo token!** 🔥



