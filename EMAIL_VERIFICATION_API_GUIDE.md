# Hướng dẫn sử dụng API Email Verification

## Tổng quan
Hệ thống đã được cập nhật để yêu cầu xác thực email trước khi đăng ký tài khoản và hỗ trợ quên mật khẩu qua email OTP.

## Cấu hình Email
Đã thêm cấu hình Gmail SMTP vào `appsettings.json`:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "healthchildtracker@gmail.com",
    "SenderPassword": "tqja rsmb afxw qkze",
    "SenderName": "Health Child Tracker",
    "EnableSsl": true
  }
}
```

## Flow đăng ký mới với Email Verification

### 1. Đăng ký tài khoản (Gửi OTP)
**Endpoint:** `POST /api/auth/register`

**Request Body:**
```json
{
  "accountName": "testuser",
  "password": "password123",
  "email": "user@example.com",
  "fullName": "Nguyễn Văn A",
  "phone": "0123456789",
  "address": "123 Đường ABC, TP.HCM"
}
```

**Response:**
```json
{
  "message": "Vui lòng kiểm tra email và nhập mã xác thực để hoàn tất đăng ký"
}
```

### 2. Hoàn tất đăng ký (Xác thực OTP và tạo tài khoản)
**Endpoint:** `POST /api/auth/complete-registration`

**Request Body:**
```json
{
  "email": "user@example.com",
  "otpCode": "123456"
}
```

**Response (thành công):**
```json
{
  "accountId": 1,
  "accountName": "testuser",
  "email": "user@example.com",
  "role": "Member",
  "fullName": "Nguyễn Văn A",
  "phone": "0123456789",
  "address": "123 Đường ABC, TP.HCM",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 3. Gửi lại mã xác thực
**Endpoint:** `POST /api/auth/resend-verification`

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

## Flow Quên Mật Khẩu

### 1. Yêu cầu đặt lại mật khẩu
**Endpoint:** `POST /api/auth/forgot-password`

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Response:**
```json
{
  "message": "Email khôi phục mật khẩu đã được gửi"
}
```

### 2. Đặt lại mật khẩu với OTP
**Endpoint:** `POST /api/auth/reset-password`

**Request Body:**
```json
{
  "email": "user@example.com",
  "otpCode": "123456",
  "newPassword": "newpassword123"
}
```

**Response:**
```json
{
  "message": "Mật khẩu đã được đặt lại thành công"
}
```

## Các endpoint phụ trợ

### Gửi email xác thực (trước khi đăng ký)
**Endpoint:** `POST /api/auth/send-verification-email`

**Request Body:**
```json
{
  "email": "user@example.com"
}
```



## Lưu ý quan trọng

1. **Thời gian hết hạn OTP:** 15 phút
2. **OTP được lưu trong memory cache** - không ảnh hưởng đến database
3. **Mã OTP có 6 chữ số** được tạo ngẫu nhiên
4. **Email template** đã được thiết kế đẹp với HTML
5. **Tự động xóa OTP cũ** khi tạo OTP mới cho cùng email và loại

## Cách test

1. **Test đăng ký:**
   - Gọi `/api/auth/register` với thông tin hợp lệ
   - Kiểm tra email nhận được OTP
   - Gọi `/api/auth/complete-registration` với email và OTP
   - Nhận token để đăng nhập

2. **Test quên mật khẩu:**
   - Gọi `/api/auth/forgot-password` với email đã tồn tại
   - Kiểm tra email nhận được OTP
   - Gọi `/api/auth/reset-password` với email, OTP và mật khẩu mới
   - Đăng nhập với mật khẩu mới

## Xử lý lỗi

- **Email đã tồn tại:** Khi đăng ký với email đã có tài khoản
- **OTP không hợp lệ:** Khi nhập sai OTP hoặc OTP đã hết hạn
- **Email không tồn tại:** Khi quên mật khẩu với email chưa đăng ký
- **Lỗi gửi email:** Khi có vấn đề với SMTP server

## Security Features

- Mật khẩu được hash bằng BCrypt
- JWT token có thời gian hết hạn
- OTP có thời gian sống ngắn (15 phút)
- Tự động cleanup OTP hết hạn
- Kiểm tra email hợp lệ trước khi gửi OTP
