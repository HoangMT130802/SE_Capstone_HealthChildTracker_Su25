# Hướng dẫn sử dụng API Vaccine Reminder

## Tổng quan
Hệ thống email notification đã được triển khai để gửi nhắc nhở vaccine và appointment cho phụ huynh. Hệ thống bao gồm:

1. **Email Templates**: 3 loại email được thiết kế đẹp với HTML/CSS
2. **Background Service**: Tự động gửi email hàng ngày lúc 8:00 AM
3. **Manual APIs**: Cho phép gửi email thủ công
4. **Admin APIs**: Quản lý và test hệ thống

## Các loại Email

### 1. 🩺 Vaccine Reminder Email
- **Mục đích**: Nhắc nhở phụ huynh về vaccine sắp đến hạn (1-7 ngày tới)
- **Nội dung**: Tên trẻ, vaccine, mũi số, ngày dự kiến, cơ sở y tế gợi ý
- **Trigger**: Tự động hàng ngày hoặc manual

### 2. 📅 Appointment Reminder Email  
- **Mục đích**: Nhắc nhở về lịch hẹn tiêm vaccine đã đặt (1-3 ngày tới)
- **Nội dung**: Thông tin lịch hẹn, cơ sở y tế, hướng dẫn chuẩn bị
- **Trigger**: Tự động hàng ngày hoặc manual

### 3. ✅ Vaccination Completion Email
- **Mục đích**: Thông báo hoàn thành tiêm vaccine, lịch mũi tiếp theo
- **Nội dung**: Thông tin vaccine đã tiêm, ngày mũi tiếp theo (nếu có)
- **Trigger**: Manual khi hoàn thành tiêm

## Background Service

### Cấu hình tự động
- **Thời gian chạy**: Mỗi ngày lúc 8:00 AM
- **Vaccine reminders**: Gửi cho vaccine có expectedDate trong 7 ngày tới
- **Appointment reminders**: Gửi cho appointment trong 3 ngày tới
- **Logging**: Đầy đủ log để theo dõi

### Service Registration
```csharp
// Đã được đăng ký trong Program.cs
builder.Services.AddScoped<IVaccineReminderService, VaccineReminderService>();
builder.Services.AddHostedService<VaccineReminderBackgroundService>();
```

## API Endpoints

### 1. Manual Vaccine Reminder
```http
POST /api/vaccinereminder/send-vaccine-reminder/{childId}/{vaccineProfileId}
Authorization: Bearer {token}
```

**Mục đích**: Gửi vaccine reminder cho một trẻ cụ thể

**Response**:
```json
{
  "message": "Vaccine reminder sent successfully"
}
```

### 2. Manual Appointment Reminder
```http
POST /api/vaccinereminder/send-appointment-reminder/{appointmentId}
Authorization: Bearer {token}
```

**Mục đích**: Gửi appointment reminder cho một appointment cụ thể

### 3. Vaccination Completion Notification
```http
POST /api/vaccinereminder/send-completion-notification/{childId}/{vaccineProfileId}
Authorization: Bearer {token}
```

**Mục đích**: Gửi thông báo hoàn thành tiêm vaccine

### 4. Get Upcoming Vaccine Reminders
```http
GET /api/vaccinereminder/upcoming-vaccine-reminders?daysAhead=7
Authorization: Bearer {token}
```

**Response**:
```json
[
  {
    "vaccineProfileId": 123,
    "childId": 45,
    "childName": "Nguyễn Văn A",
    "parentName": "Nguyễn Thị B",
    "parentEmail": "parent@email.com",
    "vaccineName": "Vaccine A",
    "doseNumber": 1,
    "expectedDate": "2024-01-15",
    "facilityName": "Bệnh viện ABC",
    "reminderSent": false
  }
]
```

### 5. Get Upcoming Appointment Reminders
```http
GET /api/vaccinereminder/upcoming-appointment-reminders?daysAhead=3
Authorization: Bearer {token}
```

### 6. Trigger Daily Reminders (Admin Only)
```http
POST /api/vaccinereminder/trigger-daily-reminders
Authorization: Bearer {admin-token}
```

**Mục đích**: Chạy thủ công daily reminders (không cần chờ 8:00 AM)

### 7. Test Email (Admin Only)
```http
POST /api/vaccinereminder/test-email
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "email": "test@example.com",
  "emailType": "vaccine"  // vaccine, appointment, completion
}
```

**Mục đích**: Test email templates với dữ liệu mẫu

## Cấu hình Email

### Gmail SMTP (Đã cấu hình)
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "healthchildrentracker@gmail.com",
    "SenderPassword": "tqja rsmb afxw qkze",
    "SenderName": "Health Child Tracker",
    "EnableSsl": true
  }
}
```

## Logic nghiệp vụ

### Vaccine Reminder Logic
1. Query `ChildVaccineProfile` với:
   - `ExpectedDate` từ hôm nay đến 7 ngày tới
   - `Status = "Scheduled"`
   - `ActualDate = null` (chưa tiêm)
2. Join với `Child`, `Child.Member`, `Child.Member.Account`, `Vaccine` để lấy thông tin parent
3. Sử dụng AutoMapper để convert Entity sang DTO
4. Gửi email cho từng record có email hợp lệ

### Appointment Reminder Logic
1. Query `VaccinationAppointment` với:
   - `Schedule.Date` từ hôm nay đến 3 ngày tới
   - `Status = "Confirmed" OR "Paid"`
2. Join với `Child`, `Child.Member`, `Child.Member.Account`, `Schedule`, `Facility` để lấy thông tin
3. Query `VaccinationAppointmentDetail` để lấy thông tin vaccine
4. Gửi email cho từng appointment có email hợp lệ

### Data Structure Improvements
- ✅ Sử dụng existing DTOs và AutoMapper
- ✅ Đúng relationship: `Child` → `Member` → `Account` (không phải `Child` → `Account`)
- ✅ Sử dụng `Child.FullName` thay vì `Child.Name`
- ✅ Format thời gian đúng: `StartTime:HH:mm - EndTime:HH:mm`
- ✅ Include properties đầy đủ cho Entity Framework

### Email Templates
- **Responsive Design**: Hoạt động tốt trên mobile và desktop
- **Professional Styling**: Màu sắc và layout đẹp mắt
- **Rich Content**: Icons, buttons, structured information
- **Vietnamese Content**: Hoàn toàn tiếng Việt

## Monitoring và Logging

### Log Levels
- **Information**: Successful email sends, daily runs
- **Warning**: Missing data, invalid profiles
- **Error**: Email send failures, service errors

### Log Examples
```
[INFO] Vaccine reminder sent to user@email.com for child John, vaccine BCG
[INFO] Daily vaccine reminders completed. Processed: 15, Errors: 0
[ERROR] Failed to send vaccine reminder to user@email.com: SMTP timeout
```

## Tích hợp với Mobile App

### Workflow
1. **Mobile app** hiển thị notification settings
2. **User** có thể enable/disable email notifications
3. **Background service** tự động gửi email
4. **Mobile app** có thể trigger manual reminders

### Future Enhancements
1. **Email preferences**: Frequency, types của notifications
2. **Email history**: Track emails đã gửi
3. **Unsubscribe functionality**: Cho phép user hủy đăng ký
4. **A/B testing**: Test different email templates
5. **Analytics**: Open rates, click rates

## Testing

### Manual Testing
1. Sử dụng `/test-email` endpoint để test templates
2. Tạo test data với `ExpectedDate` trong vài ngày tới
3. Trigger manual reminders để kiểm tra

### Production Monitoring
1. Check logs hàng ngày cho errors
2. Monitor email delivery rates
3. Track user feedback về emails

## Troubleshooting

### Common Issues
1. **Gmail rate limiting**: Max 500 emails/day
2. **Spam filters**: Emails có thể bị đánh dấu spam
3. **Invalid email addresses**: Handle gracefully
4. **SMTP timeout**: Retry mechanism

### Solutions
1. **Rate limiting**: Implement delays between emails
2. **Spam prevention**: Warm up email domain, proper SPF/DKIM
3. **Email validation**: Validate before sending
4. **Error handling**: Comprehensive try-catch blocks

## Kết luận

Hệ thống email notification đã sẵn sàng hoạt động với:
- ✅ 3 loại email templates đẹp mắt
- ✅ Background service tự động
- ✅ Manual trigger APIs
- ✅ Admin management tools
- ✅ Comprehensive logging
- ✅ Error handling

Hệ thống sẽ tự động bắt đầu gửi email từ 8:00 AM ngày mai và chạy hàng ngày.
