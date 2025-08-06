# Sửa lỗi Rebooking API

## Vấn đề đã được sửa

### 1. Thay đổi trạng thái khi rebook thành công
**Vấn đề**: Khi rebook thành công, trạng thái ChildVaccineProfile không được cập nhật đúng
**Giải pháp**: 
- Thay đổi từ `"Booked"` thành `"Pending"` khi có appointmentId
- File: `Services/Implementations/AppointmentBookingService.Rebooking.cs` dòng 218

### 2. Lỗi tìm kiếm FacilityVaccine
**Vấn đề**: Logic tìm kiếm FacilityVaccine sử dụng status `"Available"` nhưng trong database thực tế là `"active"` (chữ thường)
**Giải pháp**:
- Thay đổi tất cả điều kiện tìm kiếm từ `Status == "Available"` thành `Status == "active"`
- Cập nhật trong các phương thức:
  - `ValidateOrderAndCostAsync()` - dòng 47
  - `RebookAppointmentAsync()` - dòng 240 và 260

### 3. Cải thiện logic validation cho trường hợp không có OrderId
**Vấn đề**: Khi không có OrderId, hệ thống không kiểm tra đúng cơ sở có vaccine phù hợp
**Giải pháp**:
- Thêm validation kiểm tra cơ sở có vaccine phù hợp trước khi tạo appointment
- Thêm logic tìm tất cả cơ sở có vaccine để gợi ý khi cơ sở được chọn không có vaccine
- Cải thiện thông báo lỗi với danh sách cơ sở có vaccine

### 4. Cải thiện error handling
**Vấn đề**: Error response không có cấu trúc rõ ràng
**Giải pháp**:
- Thêm errorType vào response để dễ debug
- Cải thiện logging với thông tin chi tiết hơn
- File: `KidTracking.API/Controllers/AppointmentBookingController.cs` dòng 370-385

### 5. Cải thiện logging và error handling
**Vấn đề**: Khó debug khi có lỗi với FacilityVaccine
**Giải pháp**:
- Cải thiện logging với thông tin chi tiết về các cơ sở có vaccine
- Thêm thông báo lỗi rõ ràng với danh sách cơ sở có vaccine
- File: `Services/Implementations/AppointmentBookingService.Rebooking.cs`

## Luồng xử lý mới

### Luồng 1: Có OrderId
1. Validate ChildVaccineProfile
2. Kiểm tra Order có vaccine phù hợp
3. Tạo VaccinationAppointment với status "Pending"
4. Cập nhật ChildVaccineProfile status thành "Pending"
5. Trừ vaccine từ Order

### Luồng 2: Không có OrderId
1. Validate ChildVaccineProfile
2. Kiểm tra Schedule có vaccine phù hợp tại cơ sở
3. Nếu không có, tìm tất cả cơ sở có vaccine để gợi ý
4. Tạo VaccinationAppointment với status "Pending"
5. Tạo VaccinationAppointmentDetail
6. Cập nhật ChildVaccineProfile status thành "Pending"

## Các file đã sửa đổi

1. `Services/Implementations/AppointmentBookingService.Rebooking.cs`
   - Sửa status từ "Booked" thành "Pending"
   - Sửa điều kiện tìm kiếm FacilityVaccine
   - Thêm validation cho cơ sở có vaccine
   - Cải thiện error handling

2. `KidTracking.API/Controllers/AppointmentBookingController.cs`
   - Cải thiện error response
   - Cải thiện logging

## Testing

### Test Case 1: Rebook với OrderId
```http
POST /api/AppointmentBooking/rebook
{
  "childVaccineProfileId": 1,
  "scheduleId": 5,
  "orderId": 10,
  "note": "Test rebook with order"
}
```

### Test Case 2: Rebook không có OrderId
```http
POST /api/AppointmentBooking/rebook
{
  "childVaccineProfileId": 1,
  "scheduleId": 5,
  "note": "Test rebook without order"
}
```



## Status Conventions

### FacilityVaccine Status
- **"active"** (chữ thường): Vaccine có sẵn tại cơ sở
- **"inactive"**: Vaccine không có sẵn tại cơ sở

### Vaccine Status  
- **"Approved"**: Vaccine đã được phê duyệt và có thể sử dụng
- **"Pending"**: Vaccine đang chờ phê duyệt
- **"Rejected"**: Vaccine bị từ chối

## Lưu ý

- ✅ FacilityVaccine có status "active" (chữ thường) trong database
- ✅ Vaccine có status "Approved" trong database
- Kiểm tra AvailableQuantity > 0
- Kiểm tra ExpiryDate chưa hết hạn
- Log sẽ hiển thị thông tin chi tiết về các cơ sở có vaccine 