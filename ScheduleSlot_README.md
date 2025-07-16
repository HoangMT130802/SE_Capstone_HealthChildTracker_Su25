# ScheduleSlot Management System

## 📋 Tổng quan

Hệ thống quản lý ScheduleSlot được thiết kế để tạo và quản lý các slot thời gian cho các facility. Mỗi slot có thời gian cụ thể và có thể được tạo đơn lẻ hoặc tự động từ working hours.

## 🏗️ Cấu trúc

### Entity: ScheduleSlot
```csharp
public class ScheduleSlot
{
    public int SlotId { get; set; }
    public int FacilityId { get; set; }
    public string SlotTime { get; set; }          // "08:00 - 09:00" cho frontend
    public TimeOnly StartTime { get; set; }       // 08:00
    public TimeOnly EndTime { get; set; }         // 09:00
    public int SlotDurationMinutes { get; set; }  // 60
    public int MaxCapacity { get; set; }          // 10
    public int BookedCount { get; set; }          // 0 (tự động tính)
    public string Status { get; set; }            // Available/Unavailable
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

## 🔧 Tính năng chính

### 1. Tạo Single Slot
Tạo 1 slot đơn lẻ với thời gian cụ thể:
```json
POST /api/scheduleslots
{
    "IsWorkingHours": false,
    "StartTime": "19:00",
    "EndTime": "20:00",
    "MaxCapacity": 5,
    "Status": "Available"
}
```

**Kết quả:** Tạo 1 slot từ 19:00-20:00 với `SlotTime = "19:00 - 20:00"`

### 2. Tạo Working Hours
Tạo nhiều slots tự động từ working hours:
```json
POST /api/scheduleslots
{
    "IsWorkingHours": true,
    "WorkingHoursStart": "08:00",
    "WorkingHoursEnd": "17:00",
    "SlotDurationMinutes": 60,
    "LunchBreakStart": "12:00",
    "LunchBreakEnd": "13:00",
    "MaxCapacity": 10,
    "Status": "Available"
}
```

**Kết quả:** Tạo 8 slots riêng biệt:
- 08:00-09:00 (`SlotTime = "08:00 - 09:00"`)
- 09:00-10:00 (`SlotTime = "09:00 - 10:00"`)
- 10:00-11:00 (`SlotTime = "10:00 - 11:00"`)
- 11:00-12:00 (`SlotTime = "11:00 - 12:00"`)
- *Skip lunch break 12:00-13:00*
- 13:00-14:00 (`SlotTime = "13:00 - 14:00"`)
- 14:00-15:00 (`SlotTime = "14:00 - 15:00"`)
- 15:00-16:00 (`SlotTime = "15:00 - 16:00"`)
- 16:00-17:00 (`SlotTime = "16:00 - 17:00"`)

## 🛡️ Phân quyền

### Admin
- ✅ Xem tất cả slots của tất cả facilities
- ✅ Xem slots theo facility ID

### Manager
- ✅ CRUD operations chỉ với slots của facility mình
- ✅ Tạo/sửa/xóa slots
- ✅ Tạo working hours (tạo nhiều slots cùng lúc)
- ✅ Cập nhật trạng thái slots

### FacilityStaff, Doctor
- ✅ Xem slots của facility mình (read-only)

### Member
- ✅ Xem slots của tất cả facilities (để book appointment)

## 📡 API Endpoints

### Basic Operations
- `GET /api/scheduleslots` - Lấy tất cả slots (Admin)
- `GET /api/scheduleslots/my-facility` - Lấy slots của facility mình
- `GET /api/scheduleslots/facility/{id}` - Lấy slots theo facility ID
- `GET /api/scheduleslots/{id}` - Lấy slot theo ID
- `POST /api/scheduleslots` - Tạo slot/working hours (Manager)
- `PUT /api/scheduleslots/{id}` - Cập nhật slot (Manager)
- `DELETE /api/scheduleslots/{id}` - Xóa slot (Manager)

### Utility Operations
- `PUT /api/scheduleslots/{id}/status` - Cập nhật trạng thái slot
- `DELETE /api/scheduleslots/batch` - Xóa nhiều slots

## 🎯 Ưu điểm

### 1. Thời gian cụ thể
- Mỗi slot có thời gian bắt đầu/kết thúc cụ thể
- `SlotTime` string format "08:00 - 09:00" cho frontend dễ hiển thị
- Dễ dàng sort theo thời gian

### 2. Tự động hóa
- Tự động tạo nhiều slots từ working hours
- Tự động tính `BookedCount` từ appointments
- Tự động skip lunch break
- Tự động tạo `SlotTime` format từ `StartTime` và `EndTime`

### 3. Đơn giản và hiệu quả
- Không cần update database schema
- Sử dụng các trường hiện có
- Logic đơn giản, dễ bảo trì

### 4. Phân quyền chặt chẽ
- Manager chỉ quản lý slots của facility mình
- Member có thể xem tất cả để book appointment

## 📊 Ví dụ sử dụng

### Frontend hiển thị slots
```javascript
// Response từ API
{
    "slotId": 1,
    "facilityId": 1,
    "slotTime": "08:00 - 09:00",    // ← Hiển thị cho user
    "startTime": "08:00:00",
    "endTime": "09:00:00",
    "maxCapacity": 10,
    "bookedCount": 3,
    "availableCapacity": 7,
    "status": "Available"
}

// Hiển thị trong dropdown
<option value="1">08:00 - 09:00 (7 slots còn lại)</option>
```

### Booking appointment
```javascript
// Khi user chọn slot, frontend gửi slotId
{
    "slotId": 1,
    "appointmentDate": "2024-01-15",
    "patientId": 123
}
```

### Xóa working hours (xóa nhiều slots)
```javascript
// Manager có thể chọn slots theo thời gian và xóa nhiều slots cùng lúc
DELETE /api/scheduleslots/batch
[1, 2, 3, 4, 5, 6, 7, 8]  // IDs của slots 08:00-17:00
```

## 🧪 Testing

Sử dụng file `KidTracking.API.http` để test các endpoints:
```
### Tạo working hours
POST {{host}}/api/scheduleslots
{
    "IsWorkingHours": true,
    "WorkingHoursStart": "08:00",
    "WorkingHoursEnd": "17:00",
    "SlotDurationMinutes": 60,
    "MaxCapacity": 10
}
```

## 🚀 Deployment

1. ✅ **Không cần update database** - Sử dụng schema hiện tại
2. Deploy code mới
3. Test các endpoints
4. Cập nhật frontend để sử dụng `SlotTime` field

## 💡 Logic Working Hours

**Input:** WorkingHoursStart, WorkingHoursEnd, SlotDurationMinutes, LunchBreak
**Output:** Nhiều ScheduleSlot records riêng biệt

**Ví dụ:** 08:00-17:00, duration=60, lunch=12:00-13:00
```
→ Slot 1: StartTime=08:00, EndTime=09:00, SlotTime="08:00 - 09:00"
→ Slot 2: StartTime=09:00, EndTime=10:00, SlotTime="09:00 - 10:00"
→ Slot 3: StartTime=10:00, EndTime=11:00, SlotTime="10:00 - 11:00"
→ Slot 4: StartTime=11:00, EndTime=12:00, SlotTime="11:00 - 12:00"
→ [Skip lunch 12:00-13:00]
→ Slot 5: StartTime=13:00, EndTime=14:00, SlotTime="13:00 - 14:00"
→ Slot 6: StartTime=14:00, EndTime=15:00, SlotTime="14:00 - 15:00"
→ Slot 7: StartTime=15:00, EndTime=16:00, SlotTime="15:00 - 16:00"
→ Slot 8: StartTime=16:00, EndTime=17:00, SlotTime="16:00 - 17:00"
```

---

**Lưu ý:** Hệ thống này được thiết kế đơn giản, hiệu quả và không cần thay đổi database schema hiện tại. 