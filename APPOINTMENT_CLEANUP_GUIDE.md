# Hướng dẫn Appointment Cleanup System

## Tổng quan

Hệ thống tự động dọn dẹp các appointment đã quá hạn và xóa AppointmentId khỏi ChildVaccineProfile để đảm bảo dữ liệu nhất quán và cho phép user đặt lại lịch hẹn.

## Vấn đề được giải quyết

**Vấn đề ban đầu**: Logic hiện tại không tự động tháo VaccinationAppointment ra khỏi ChildVaccineProfile nếu lịch quá thời gian rồi.

**Hậu quả**:
- ChildVaccineProfile vẫn giữ AppointmentId của appointment đã quá hạn
- User không thể đặt lại lịch hẹn cho vaccine đó
- Dữ liệu không nhất quán trong hệ thống

## Giải pháp

### 1. AppointmentCleanupBackgroundService

**File**: `Services/Implementations/AppointmentCleanupBackgroundService.cs`

- **Chạy tự động**: Mỗi 6 tiếng một lần
- **Chức năng**: Gọi `CleanupExpiredAppointmentsAsync()` để dọn dẹp appointment quá hạn

### 2. Logic Cleanup trong AppointmentBookingService

**File**: `Services/Implementations/AppointmentBookingService.cs`

#### Quy tắc xử lý:

**Appointment Status "Pending":**
- Quá 24 giờ kể từ thời gian hẹn → Chuyển thành "Cancelled"
- Lý do: Quá lâu không được facility xác nhận

**Appointment Status "Approval":**
- Quá 2 giờ kể từ thời gian hẹn → Chuyển thành "Expired"
- Lý do: User không đến tiêm đúng giờ

#### Các bước xử lý:

1. **Tìm appointment quá hạn**
   - Query appointment có status "Pending" hoặc "Approval"
   - Kiểm tra thời gian dựa trên `Schedule.Date` và `Slot.StartTime`

2. **Cập nhật appointment status**
   - "Pending" → "Cancelled" (với note tự động)
   - "Approval" → "Expired"

3. **Xóa AppointmentId khỏi ChildVaccineProfile**
   - Set `AppointmentId = null`
   - Set `Status = "Pending"`
   - Cập nhật `UpdatedAt`

4. **Trả lại số lượng vaccine**
   - Gọi `RestoreVaccineQuantityOnCancelAsync()` để hoàn trả vaccine về stock

### 3. Manual Cleanup API

**File**: `KidTracking.API/Controllers/AppointmentCleanupController.cs`

**Endpoint**: `POST /api/appointmentcleanup/cleanup-expired`

- Cho phép admin trigger cleanup thủ công
- Yêu cầu authentication
- Trả về kết quả chi tiết

## Cấu hình

### Đăng ký Service

Trong `Program.cs`:
```csharp
builder.Services.AddHostedService<AppointmentCleanupBackgroundService>();
```

### Thời gian chạy

- **Background Service**: Mỗi 6 tiếng
- **Delay ban đầu**: 1 phút sau khi khởi động hệ thống

## Kết quả Cleanup

### AppointmentCleanupResultDTO

```csharp
public class AppointmentCleanupResultDTO
{
    public int ExpiredAppointmentsCount { get; set; }      // Số appointment expired
    public int CancelledAppointmentsCount { get; set; }    // Số appointment cancelled
    public int TotalProcessed { get; set; }                // Tổng số đã xử lý
    public int ChildVaccineProfilesUpdated { get; set; }   // Số CVP đã cập nhật
    public DateTime ProcessedAt { get; set; }              // Thời gian xử lý
    public List<int> ProcessedAppointmentIds { get; set; } // Danh sách ID đã xử lý
    public string Message { get; set; }                    // Thông báo
    public bool HasErrors { get; set; }                    // Có lỗi không
    public List<string> Errors { get; set; }               // Danh sách lỗi
}
```

## Logging

Hệ thống ghi log chi tiết:

- **Info**: Bắt đầu/kết thúc cleanup, số lượng xử lý
- **Debug**: Chi tiết từng appointment được xử lý
- **Error**: Lỗi trong quá trình cleanup

## Test và Monitoring

### Manual Test

1. **Tạo appointment test**:
   - Tạo appointment với thời gian trong quá khứ
   - Set status "Pending" hoặc "Approval"

2. **Trigger cleanup**:
   ```bash
   POST /api/appointmentcleanup/cleanup-expired
   Authorization: Bearer <token>
   ```

3. **Kiểm tra kết quả**:
   - Appointment status đã thay đổi
   - ChildVaccineProfile.AppointmentId = null
   - ChildVaccineProfile.Status = "Pending"

### Monitoring

- Kiểm tra log của `AppointmentCleanupBackgroundService`
- Monitor số lượng appointment được cleanup
- Theo dõi performance của background service

## Lưu ý quan trọng

1. **Backup dữ liệu**: Nên backup trước khi deploy
2. **Test thoroughly**: Test với dữ liệu thực tế trước khi production
3. **Monitor performance**: Background service không được ảnh hưởng đến performance hệ thống
4. **Error handling**: Lỗi trong cleanup không được làm crash hệ thống

## Tương lai

Có thể mở rộng:
- Thêm notification cho user khi appointment bị cleanup
- Cấu hình thời gian cleanup qua config file
- Dashboard để monitor cleanup statistics
- Preview cleanup trước khi thực hiện
