# Hướng dẫn thiết lập Thông báo Đẩy (Push Notifications) cho Health Child Tracker

## Tổng quan
Hệ thống thông báo đẩy đã được tích hợp với hệ thống nhắc nhở qua email hiện có để gửi thông báo về:
- 🩺 **Nhắc nhở tiêm vaccine**: Nhắc nhở về vaccine sắp đến hạn (1-7 ngày)
- 📅 **Nhắc nhở lịch hẹn**: Nhắc nhở về lịch hẹn đã đặt (1-3 ngày)  
- ✅ **Hoàn thành tiêm vaccine**: Thông báo hoàn thành tiêm vaccine

## Kiến trúc hệ thống

### Các thành phần Backend
1. **IPushNotificationService**: Giao diện cho dịch vụ thông báo đẩy
2. **PushNotificationService**: Triển khai sử dụng Firebase Cloud Messaging
3. **IDeviceTokenService**: Quản lý mã token thiết bị của người dùng
4. **DeviceTokenService**: Triển khai quản lý token thiết bị
5. **DeviceToken Entity**: Lưu trữ FCM tokens trong cơ sở dữ liệu
6. **VaccineReminderService**: Đã được mở rộng để gửi cả email và thông báo đẩy

### Cấu trúc Cơ sở dữ liệu
```sql
-- Bảng DeviceToken (cần tạo migration)
CREATE TABLE DeviceToken (
    DeviceTokenId INT IDENTITY(1,1) PRIMARY KEY,
    AccountId INT NOT NULL,
    Token NVARCHAR(500) NOT NULL,
    DeviceType NVARCHAR(20) NOT NULL, -- 'android', 'ios', 'web'
    DeviceInfo NVARCHAR(MAX), -- JSON với thông tin thiết bị
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    LastUsedAt DATETIME2,
    FOREIGN KEY (AccountId) REFERENCES Account(AccountId)
);

CREATE INDEX IX_DeviceToken_AccountId ON DeviceToken(AccountId);
CREATE INDEX IX_DeviceToken_Token ON DeviceToken(Token);
```

## Cấu hình Firebase

### 1. Tạo Dự án Firebase
1. Truy cập [Firebase Console](https://console.firebase.google.com/)
2. Tạo dự án mới hoặc sử dụng dự án hiện có
3. Kích hoạt **Cloud Messaging** trong cài đặt dự án

### 2. Tạo Tài khoản Dịch vụ
1. Vào **Cài đặt Dự án** → **Tài khoản Dịch vụ**
2. Nhấp **Tạo khóa riêng mới**
3. Tải xuống file JSON chứa thông tin xác thực

### 3. Cấu hình Backend
Thêm thông tin xác thực Firebase vào `appsettings.json`:

```json
{
  "Firebase": {
    "CredentialsPath": "path/to/firebase-service-account.json",
    "ServiceAccountJson": ""
  }
}
```

**Hoặc** sử dụng biến môi trường:
```json
{
  "Firebase": {
    "CredentialsPath": "",
    "ServiceAccountJson": "{\"type\":\"service_account\",\"project_id\":\"du-an-cua-ban\"...}"
  }
}
```

## Các API Endpoint

### Quản lý Token Thiết bị

#### Đăng ký Token Thiết bị
```http
POST /api/devicetoken/register
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "token": "fcm-device-token-here",
  "deviceType": "android", // "android", "ios", "web"
  "deviceInfo": "{\"model\":\"Samsung Galaxy S21\",\"os\":\"Android 12\"}"
}
```

#### Xóa Device Token
```http
DELETE /api/devicetoken/remove?token=fcm-device-token-here
Authorization: Bearer {jwt-token}
```

#### Lấy Device Tokens của User
```http
GET /api/devicetoken/my-tokens
Authorization: Bearer {jwt-token}
```

### Push Notification Testing

#### Gửi Push thử nghiệm (Admin)
```http
POST /api/vaccinereminder/test-push
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "accountId": 123,
  "title": "Test Push Notification",
  "body": "This is a test message",
  "data": {
    "type": "test",
    "timestamp": "2024-01-15T10:00:00Z"
  }
}
```

## Mobile App Implementation

### Android (Flutter)

#### 1. Thêm dependencies
```yaml
dependencies:
  firebase_messaging: ^14.7.10
  firebase_core: ^2.24.2
```

#### 2. Cấu hình Firebase
```dart
// main.dart
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Firebase.initializeApp();
  
  // Xử lý background messages
  FirebaseMessaging.onBackgroundMessage(_firebaseMessagingBackgroundHandler);
  
  runApp(MyApp());
}

Future<void> _firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await Firebase.initializeApp();
  print("Background message: ${message.notification?.title}");
}
```

#### 3. Lấy FCM Token
```dart
class PushNotificationService {
  static final FirebaseMessaging _messaging = FirebaseMessaging.instance;
  
  static Future<void> initialize() async {
    // Request permission
    NotificationSettings settings = await _messaging.requestPermission(
      alert: true,
      badge: true,
      sound: true,
    );
    
    if (settings.authorizationStatus == AuthorizationStatus.authorized) {
      // Lấy FCM token
      String? token = await _messaging.getToken();
      if (token != null) {
        await _registerDeviceToken(token);
      }
      
      // Listen for token refresh
      _messaging.onTokenRefresh.listen(_registerDeviceToken);
      
      // Handle foreground messages
      FirebaseMessaging.onMessage.listen(_handleForegroundMessage);
      
      // Handle notification taps
      FirebaseMessaging.onMessageOpenedApp.listen(_handleNotificationTap);
    }
  }
  
  static Future<void> _registerDeviceToken(String token) async {
    try {
      final response = await http.post(
        Uri.parse('${ApiConfig.baseUrl}/api/devicetoken/register'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer ${await AuthService.getToken()}',
        },
        body: json.encode({
          'token': token,
          'deviceType': Platform.isAndroid ? 'android' : 'ios',
          'deviceInfo': json.encode({
            'model': await DeviceInfo.getDeviceModel(),
            'os': Platform.operatingSystem,
            'appVersion': await PackageInfo.fromPlatform().then((info) => info.version),
          }),
        }),
      );
      
      if (response.statusCode == 200) {
        print('Device token registered successfully');
      }
    } catch (e) {
      print('Failed to register device token: $e');
    }
  }
  
  static void _handleForegroundMessage(RemoteMessage message) {
    // Hiển thị notification trong app
    showDialog(
      context: navigatorKey.currentContext!,
      builder: (context) => AlertDialog(
        title: Text(message.notification?.title ?? ''),
        content: Text(message.notification?.body ?? ''),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: Text('OK'),
          ),
        ],
      ),
    );
  }
  
  static void _handleNotificationTap(RemoteMessage message) {
    // Xử lý khi user tap vào notification
    final data = message.data;
    
    switch (data['type']) {
      case 'vaccine_reminder':
        // Navigate to vaccine schedule page
        break;
      case 'appointment_reminder':
        // Navigate to appointments page
        break;
      case 'vaccination_completion':
        // Navigate to vaccination history
        break;
    }
  }
}
```

#### 4. Notification Channel (Android)
```dart
// android/app/src/main/res/values/strings.xml
<resources>
    <string name="default_notification_channel_id">vaccine_reminders</string>
    <string name="default_notification_channel_name">Vaccine Reminders</string>
</resources>
```

### iOS (Flutter)

#### 1. Cấu hình APNs
- Tạo APNs certificate trong Apple Developer Console
- Upload certificate lên Firebase Console

#### 2. Info.plist configuration
```xml
<!-- ios/Runner/Info.plist -->
<key>FirebaseMessagingAutoInitEnabled</key>
<true/>
```

## Testing và Monitoring

### 1. Test Push Notifications

#### Backend Test
```bash
# Test gửi push notification
curl -X POST "https://your-api.com/api/vaccinereminder/test-push" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": 123,
    "title": "Test Vaccine Reminder",
    "body": "Bé ABC sắp đến lịch tiêm vaccine BCG",
    "data": {
      "type": "vaccine_reminder",
      "childName": "ABC",
      "vaccineName": "BCG"
    }
  }'
```

#### Firebase Console Test
1. Vào Firebase Console → Cloud Messaging
2. Click "Send your first message"
3. Nhập device token để test

### 2. Monitoring và Analytics

#### Logs Backend
```csharp
// Logs được tự động ghi trong PushNotificationService
[INFO] Push notification sent to device xxxxxx...xxxx for child John
[WARNING] Failed to send push notification: invalid token
[INFO] Cleaned up 5 inactive device tokens
```

#### Firebase Analytics
- Message delivery rates
- Open rates
- Device token statistics
- Error tracking

## Production Deployment

### 1. Environment Configuration

#### Production appsettings.json
```json
{
  "Firebase": {
    "ServiceAccountJson": "{\"type\":\"service_account\",...}"
  }
}
```

#### Environment Variables
```bash
FIREBASE_SERVICE_ACCOUNT_JSON='{...}'
```

### 2. Security Considerations

1. **Token Security**: Device tokens không nên expose trong logs
2. **Rate Limiting**: Firebase có giới hạn 1000 messages/minute
3. **Token Cleanup**: Tự động cleanup inactive tokens
4. **Error Handling**: Graceful fallback khi push notification fails

### 3. Performance Optimization

1. **Batch Processing**: Sử dụng multicast cho nhiều devices
2. **Async Processing**: Push notifications không block email sending
3. **Token Caching**: Cache active tokens để giảm database queries
4. **Background Jobs**: Cleanup tokens trong background service

## Troubleshooting

### Common Issues

1. **"Invalid token" errors**
   - Token đã expire hoặc app bị uninstall
   - Solution: Auto-deactivate invalid tokens

2. **Messages không đến**
   - Kiểm tra Firebase project configuration
   - Verify APNs certificates (iOS)
   - Check device notification settings

3. **High failure rates**
   - Token cleanup không đủ thường xuyên
   - Network connectivity issues
   - Firebase service outages

### Debug Commands

```bash
# Check Firebase connectivity
curl -X POST https://fcm.googleapis.com/v1/projects/YOUR_PROJECT/messages:send \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{...}'

# Test device token registration
curl -X POST "https://your-api.com/api/devicetoken/register" \
  -H "Authorization: Bearer USER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"token":"test-token","deviceType":"android"}'
```

## Migration Steps

### 1. Database Migration
```bash
# Tạo migration cho DeviceToken table
dotnet ef migrations add AddDeviceTokenTable
dotnet ef database update
```

### 2. Service Registration
Services đã được đăng ký trong Program.cs:
```csharp
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<IDeviceTokenService, DeviceTokenService>();
```

### 3. Mobile App Updates
1. Thêm Firebase dependencies
2. Initialize push notification service
3. Register device tokens after login
4. Handle notification taps và foreground messages

## Kết luận

Hệ thống push notification đã được tích hợp hoàn chỉnh với:
- ✅ Firebase Cloud Messaging integration
- ✅ Device token management
- ✅ Automatic push notifications với vaccine reminders
- ✅ RESTful APIs cho mobile app
- ✅ Error handling và token cleanup
- ✅ Comprehensive logging và monitoring

Hệ thống sẽ tự động gửi cả email và push notifications cho vaccine reminders, đảm bảo phụ huynh không bỏ lỡ lịch tiêm quan trọng.
