# Hướng dẫn sử dụng Thank You Email API

## Tổng quan

API này cho phép gửi email cảm ơn đẹp mắt và chuyên nghiệp cho các member đã sử dụng hệ thống Health Child Tracker. Email bao gồm lời cảm ơn, thống kê cá nhân, và thông tin về các tính năng của hệ thống.

## Base URL
```
/api/thankyouemail
```

## Authentication
Tất cả endpoints đều yêu cầu authentication (Bearer token).

## Endpoints

### 1. 📧 Gửi Email Cảm Ơn Cho Member Cụ Thể

```http
POST /api/thankyouemail/send/{memberId}
```

**Parameters:**
- `memberId` (path, required): ID của member
- `includeStatistics` (query, optional): Có bao gồm thống kê cá nhân không (default: true)

**Response:**
```json
{
  "success": true,
  "message": "Email cảm ơn đã được gửi thành công",
  "memberInfo": {
    "memberId": 123,
    "memberName": "Nguyễn Văn A",
    "email": "nguyenvana@email.com"
  },
  "statistics": {
    "totalChildren": 2,
    "totalAppointments": 15,
    "totalVaccinations": 12
  },
  "sentAt": "2024-12-15T10:30:00Z"
}
```

**Ví dụ sử dụng:**
```bash
curl -X POST "https://api.healthchildtracker.com/api/thankyouemail/send/123?includeStatistics=true" \
  -H "Authorization: Bearer your_token_here"
```

### 2. 📨 Gửi Email Hàng Loạt

```http
POST /api/thankyouemail/send-bulk
```

**Body:**
```json
[123, 456, 789]
```

**Parameters:**
- `includeStatistics` (query, optional): Có bao gồm thống kê cá nhân không (default: true)

**Response:**
```json
{
  "message": "Đã gửi email cho 3/3 members",
  "summary": {
    "totalRequested": 3,
    "successCount": 3,
    "failureCount": 0
  },
  "results": [
    {
      "memberId": 123,
      "memberName": "Nguyễn Văn A",
      "email": "nguyenvana@email.com",
      "success": true,
      "statistics": {
        "totalChildren": 2,
        "totalAppointments": 15,
        "totalVaccinations": 12
      }
    }
  ],
  "processedAt": "2024-12-15T10:30:00Z"
}
```

**Giới hạn:**
- Tối đa 100 members cùng lúc
- Có delay 200ms giữa các email để tránh spam

### 3. 👥 Lấy Danh Sách Members

```http
GET /api/thankyouemail/members
```

**Parameters:**
- `pageIndex` (query, optional): Trang (default: 1)
- `pageSize` (query, optional): Số lượng per trang (default: 20)
- `search` (query, optional): Tìm kiếm theo tên hoặc email

**Response:**
```json
{
  "members": [
    {
      "memberId": 123,
      "fullName": "Nguyễn Văn A",
      "email": "nguyenvana@email.com",
      "phoneNumber": "0123456789",
      "createdAt": "2024-01-15T08:00:00Z",
      "hasChildren": true
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8
  }
}
```

### 4. 👁️ Preview Email

```http
GET /api/thankyouemail/preview/{memberId}
```

**Response:**
```json
{
  "recipient": {
    "memberId": 123,
    "memberName": "Nguyễn Văn A",
    "email": "nguyenvana@email.com"
  },
  "emailPreview": {
    "subject": "🙏 Cảm ơn bạn đã tin tướng và sử dụng Health Child Tracker",
    "statistics": {
      "totalChildren": 2,
      "totalAppointments": 15,
      "totalVaccinations": 12
    },
    "features": [
      "📱 Quản lý tiêm chủng dễ dàng",
      "🏥 Kết nối cơ sở y tế uy tín",
      "📈 Theo dõi tăng trưởng toàn diện",
      "🛡️ An toàn và bảo mật"
    ]
  },
  "note": "Đây là preview, email chưa được gửi. Sử dụng endpoint /send/{memberId} để gửi thật."
}
```

## Nội dung Email

Email cảm ơn bao gồm:

### 📋 **Header**
- Lời chào cá nhân hóa với tên member
- Gradient background đẹp mắt (xanh dương → xanh lá)

### 📊 **Thống kê cá nhân** (nếu có dữ liệu)
- Số lượng bé yêu đã đăng ký
- Số lịch hẹn đã đặt  
- Số mũi tiêm hoàn thành

### ✨ **Tính năng nổi bật**
- 📱 Quản lý tiêm chủng dễ dàng
- 🏥 Kết nối cơ sở y tế uy tín
- 📈 Theo dõi tăng trưởng toàn diện
- 🛡️ An toàn và bảo mật

### 💭 **Testimonial**
- Quote về cam kết của đội ngũ
- Thiết kế đẹp với border và background

### 📞 **Thông tin liên hệ**
- Email support
- Hotline  
- Chat trong app

### 🏆 **Footer**
- Thông tin công ty
- Social links
- Copyright notice

## Ví dụ sử dụng thực tế

### Scenario 1: Gửi email cho 1 member
```javascript
// Frontend JavaScript
const sendThankYouEmail = async (memberId) => {
  try {
    const response = await fetch(`/api/thankyouemail/send/${memberId}`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });
    
    const result = await response.json();
    
    if (result.success) {
      alert(`Email đã gửi thành công cho ${result.memberInfo.memberName}`);
    }
  } catch (error) {
    console.error('Lỗi gửi email:', error);
  }
};
```

### Scenario 2: Gửi email hàng loạt
```javascript
const sendBulkEmails = async (memberIds) => {
  try {
    const response = await fetch('/api/thankyouemail/send-bulk', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(memberIds)
    });
    
    const result = await response.json();
    console.log(`Gửi thành công: ${result.summary.successCount}/${result.summary.totalRequested}`);
  } catch (error) {
    console.error('Lỗi gửi email hàng loạt:', error);
  }
};
```

### Scenario 3: Tìm kiếm và gửi email
```javascript
// 1. Lấy danh sách members
const getMembers = async (search = '') => {
  const response = await fetch(`/api/thankyouemail/members?search=${search}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  return response.json();
};

// 2. Gửi email cho members được chọn
const members = await getMembers('nguyễn');
const selectedIds = members.members.map(m => m.memberId);
await sendBulkEmails(selectedIds);
```

## Error Handling

### Common Errors

**404 - Member Not Found**
```json
{
  "message": "Không tìm thấy member với ID 123"
}
```

**400 - Invalid Request**
```json
{
  "message": "Member không có email để gửi"
}
```

**500 - Server Error**
```json
{
  "success": false,
  "message": "Có lỗi xảy ra khi gửi email",
  "error": "SMTP connection failed"
}
```

## Best Practices

### 1. **Kiểm tra trước khi gửi**
- Sử dụng preview endpoint để xem nội dung
- Validate email của member
- Kiểm tra thống kê trước khi gửi

### 2. **Gửi hàng loạt**
- Không gửi quá 100 emails cùng lúc
- Có delay giữa các emails (đã tự động)
- Monitor kết quả và xử lý failures

### 3. **User Experience**
- Hiển thị progress bar cho bulk sending
- Thông báo kết quả chi tiết
- Cho phép retry cho failures

### 4. **Security**
- Luôn yêu cầu authentication
- Log tất cả hoạt động gửi email
- Rate limiting để tránh abuse

## Configuration

### Email Settings
Cần cấu hình trong `appsettings.json`:
```json
{
  "EmailSettings": {
    "SenderEmail": "noreply@healthchildtracker.com",
    "SenderName": "Health Child Tracker",
    "SenderPassword": "your_email_password"
  }
}
```

### SMTP Configuration
- **Host**: smtp.gmail.com
- **Port**: 587
- **SSL**: Enabled
- **Authentication**: Required

## Monitoring & Logging

API sẽ log các hoạt động:
- ✅ Email gửi thành công
- ❌ Email gửi thất bại
- 📊 Thống kê bulk sending
- 🔍 Member lookup

Kiểm tra logs để monitor:
```
[2024-12-15 10:30:00] INFO: Thank you email sent to nguyenvana@email.com for member Nguyễn Văn A
[2024-12-15 10:30:05] INFO: Hoàn thành gửi email hàng loạt: 25 thành công, 0 thất bại
```

## Lưu ý quan trọng

1. **Rate Limiting**: Có giới hạn số email gửi để tránh spam
2. **Email Template**: Template đã được tối ưu cho mobile và desktop
3. **Error Recovery**: API sẽ tiếp tục gửi các email khác nếu một email thất bại
4. **Statistics**: Thống kê được tính realtime từ database
5. **Privacy**: Chỉ gửi cho members có email hợp lệ và đã đồng ý

## Support

Nếu có vấn đề với API:
- Kiểm tra logs server
- Verify email configuration  
- Test với single email trước
- Contact dev team nếu cần thiết

