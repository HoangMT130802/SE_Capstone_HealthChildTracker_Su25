# Hướng dẫn sử dụng API Facility Appointment (Đã đơn giản hóa)

## Tổng quan
API này cho phép facility staff (có role "FacilityStaff") xem và quản lý các lịch đặt tiêm chủng của cơ sở của họ. API đã được đơn giản hóa để dễ sử dụng hơn.

## Quyền truy cập
- **Role được phép**: FacilityStaff
- **Position**: Các position khác nhau (Manager, Doctor, Nurse, etc.) được phân biệt bởi trường Position trong FacilityStaff
- **Token yêu cầu**: Phải có FacilityId trong JWT token
- **Authorization**: Bearer token

## Các API Endpoints

### 1. Lấy tất cả lịch đặt
```
GET /api/facilityappointment
```

### 2. Lấy lịch đặt theo ngày
```
GET /api/facilityappointment/date?date=2024-01-15
```

### 3. Lấy lịch đặt theo tuần
```
GET /api/facilityappointment/week?startOfWeek=2024-01-15
```

### 4. Lấy lịch đặt theo tháng
```
GET /api/facilityappointment/month?month=2024-01-01
```

### 5. Lấy chi tiết lịch đặt
```
GET /api/facilityappointment/{appointmentId}
```

### 6. Cập nhật trạng thái lịch đặt
```
PUT /api/facilityappointment/{appointmentId}/status
```

**Request Body:**
```json
{
  "status": "Confirmed",
  "note": "Đã duyệt lịch hẹn"
}
```

## Response Format (Đã đơn giản hóa)

```json
{
  "appointments": [
    {
      "appointmentId": 1,
      "status": "Pending",
      "createdAt": "2024-01-15T10:00:00Z",
      "updatedAt": "2024-01-15T10:00:00Z",
      "note": "",
      "memberId": 1,
      "memberName": "Nguyễn Văn A",
      "memberPhone": "0123456789",
      "memberEmail": "nguyenvana@email.com",
      "child": {
        "childId": 1,
        "fullName": "Nguyễn Văn B",
        "birthDate": "2020-01-01",
        "gender": "Male",
        "bloodType": "A+"
      },
      "packageName": "Gói vaccine 5 trong 1",
      "vaccineNames": ["Vaccine A", "Vaccine B"],
      "appointmentDate": "2024-01-20",
      "appointmentTime": "09:00",
      "slotTime": "09:00-09:30",
      "estimatedCost": 500000,
      "isUpcoming": true,
      "isPast": false,
      "canApprove": true,
      "canReject": true,
      "canComplete": false
    }
  ],
  "pendingCount": 5,
  "confirmedCount": 3,
  "completedCount": 1,
  "cancelledCount": 1,
  "todayCount": 2
}
```

## Các trạng thái có thể chuyển đổi
- `Pending` → `Confirmed` (Duyệt)
- `Pending` → `Rejected` (Từ chối)
- `Confirmed` → `Completed` (Hoàn thành)
- `Confirmed` → `Cancelled` (Hủy)

## Ví dụ sử dụng

### Lấy tất cả lịch đặt
```bash
curl -X GET "https://api.example.com/api/facilityappointment" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Lấy lịch đặt hôm nay
```bash
curl -X GET "https://api.example.com/api/facilityappointment/date?date=2024-01-15" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Duyệt lịch đặt
```bash
curl -X PUT "https://api.example.com/api/facilityappointment/1/status" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Confirmed",
    "note": "Đã duyệt lịch hẹn"
  }'
```

## Thay đổi so với phiên bản trước

✅ **Đã bỏ**:
- Các filter phức tạp (status, search, sort, pagination)
- Thông tin facility trong response (vì staff đã biết cơ sở của mình)
- Thông tin chi tiết vaccine (chỉ giữ tên vaccine)

✅ **Đã đơn giản hóa**:
- Response format gọn gàng hơn
- Chỉ giữ lại API cần thiết: all, date, week, month, detail, update status
- Vaccine info chỉ hiển thị tên thay vì object phức tạp

## Lưu ý quan trọng

1. **Token yêu cầu**: Token phải chứa FacilityId để xác định cơ sở
2. **Quyền truy cập**: Chỉ staff của cơ sở mới có thể xem lịch đặt của cơ sở đó
3. **Role và Position**: 
   - Role trong Account chỉ có "FacilityStaff"
   - Position trong FacilityStaff phân biệt chức vụ (Manager, Doctor, Nurse, etc.)
4. **Trạng thái**: Chỉ có thể chuyển đổi trạng thái theo luồng đã định
5. **Thời gian**: CanApprove, CanReject chỉ true khi lịch chưa diễn ra
6. **Hoàn thành**: CanComplete chỉ true khi lịch đã đến giờ và status = "Confirmed" 