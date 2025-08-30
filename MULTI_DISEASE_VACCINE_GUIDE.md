# Hướng dẫn Multi-Disease Vaccine System

## Tổng quan

Hệ thống đã được cập nhật để hỗ trợ **Multi-Disease Vaccine** - các vaccine có thể chữa nhiều bệnh cùng lúc (ví dụ: MMR vaccine chữa Sởi, Quai bị, Rubella).

## Vấn đề đã giải quyết

### 🔴 **Vấn đề trước đây:**
- Khi user book vaccine MMR cho bệnh "Sởi", hệ thống chỉ tạo 1 ChildVaccineProfile cho "Sởi"
- Không ghi nhận rằng trẻ cũng được bảo vệ khỏi "Quai bị" và "Rubella"
- Mất thông tin về hiệu quả bảo vệ toàn diện của vaccine

### ✅ **Giải pháp hiện tại:**
- Khi user book vaccine MMR cho bệnh "Sởi", hệ thống tự động tạo ChildVaccineProfile cho **TẤT CẢ** bệnh mà MMR có thể chữa
- Ghi nhận đầy đủ hiệu quả bảo vệ của vaccine
- Đảm bảo tính nhất quán dữ liệu

## Cấu trúc dữ liệu

### VaccineDisease Table
```sql
CREATE TABLE VaccineDisease (
    VaccineDiseaseId INT PRIMARY KEY,
    VaccineId INT,
    DiseaseId INT,
    CreatedAt DATETIME,
    UpdatedAt DATETIME
);
```

### Ví dụ dữ liệu:
```sql
-- MMR Vaccine (VaccineId = 1) có thể chữa 3 bệnh
INSERT INTO VaccineDisease VALUES (1, 1, 101, '2024-01-01', '2024-01-01'); -- Sởi
INSERT INTO VaccineDisease VALUES (2, 1, 102, '2024-01-01', '2024-01-01'); -- Quai bị  
INSERT INTO VaccineDisease VALUES (3, 1, 103, '2024-01-01', '2024-01-01'); -- Rubella
```

## Logic hoạt động

### 📋 **Booking Process:**

1. **User chọn vaccine MMR cho bệnh "Sởi"**
2. **Hệ thống query VaccineDisease:**
   ```csharp
   var vaccineDiseases = vaccine.VaccineDiseases; // [Sởi, Quai bị, Rubella]
   ```
3. **Tạo ChildVaccineProfile cho TẤT CẢ bệnh:**
   ```csharp
   foreach (var diseaseId in diseaseIds)
   {
       await CreateChildVaccineProfileAsync(
           childId: 123,
           vaccineId: 1,     // MMR
           diseaseId: diseaseId, // 101, 102, 103
           appointmentId: 456,
           expectedDate: "2024-12-01",
           totalDoses: 2
       );
   }
   ```

4. **Kết quả:**
   - CVP 1: Child=123, Vaccine=MMR, Disease=Sởi, Appointment=456
   - CVP 2: Child=123, Vaccine=MMR, Disease=Quai bị, Appointment=456  
   - CVP 3: Child=123, Vaccine=MMR, Disease=Rubella, Appointment=456

### 💉 **Completion Process:**

1. **Doctor hoàn thành tiêm MMR**
2. **Hệ thống tìm TẤT CẢ ChildVaccineProfile theo AppointmentId:**
   ```csharp
   var appointmentProfiles = await profileRepository.FindAsync(p => 
       p.AppointmentId == appointmentId); // Tìm thấy 3 CVP
   ```
3. **Cập nhật TẤT CẢ CVP thành "Completed":**
   ```csharp
   foreach (var profile in appointmentProfiles)
   {
       profile.Status = "Completed";
       profile.ActualDate = DateTime.Today;
   }
   ```
4. **Tạo next dose cho TẤT CẢ diseases (nếu cần):**
   ```csharp
   foreach (var diseaseId in diseaseIds)
   {
       // Tạo CVP cho mũi 2 của từng bệnh
   }
   ```

## Thay đổi code chính

### 1. AppointmentBookingService.cs

#### Method mới: `CreateChildVaccineProfilesForMultiDiseaseVaccineAsync`
```csharp
private async Task CreateChildVaccineProfilesForMultiDiseaseVaccineAsync(
    IGenericRepository<ChildVaccineProfile> childVaccineProfileRepo,
    int childId,
    int vaccineId,
    int primaryDiseaseId, // Bệnh được chọn chính
    int appointmentId,
    DateOnly expectedDate,
    int totalDoses,
    ICollection<VaccineDisease>? vaccineDiseases)
```

#### Cập nhật 3 luồng booking:
- **Order**: Tạo CVP cho tất cả diseases
- **Package**: Tạo CVP cho tất cả diseases  
- **Individual**: Tạo CVP cho tất cả diseases

### 2. ChildVaccineProfileService.cs

#### Cập nhật `CompleteVaccinationAsync`:
- Tìm TẤT CẢ CVP theo AppointmentId
- Cập nhật TẤT CẢ CVP thành "Completed"
- Tạo next dose cho TẤT CẢ diseases

## Ví dụ thực tế

### Scenario: Book MMR Vaccine

**Input:**
```json
{
  "childId": 123,
  "facilityVaccineIds": [456], // MMR vaccine
  "diseaseId": 101, // Sởi (user chọn)
  "scheduleId": 789
}
```

**Kết quả trong database:**
```sql
-- 3 ChildVaccineProfile được tạo
INSERT INTO ChildVaccineProfile VALUES 
(1, 123, 101, 456, 1, 1, '2024-12-01', NULL, 'Pending', ...); -- Sởi
INSERT INTO ChildVaccineProfile VALUES 
(2, 123, 102, 456, 1, 1, '2024-12-01', NULL, 'Pending', ...); -- Quai bị
INSERT INTO ChildVaccineProfile VALUES 
(3, 123, 103, 456, 1, 1, '2024-12-01', NULL, 'Pending', ...); -- Rubella
```

### Scenario: Complete Vaccination

**Input:**
```json
{
  "appointmentId": 456,
  "facilityVaccineId": 789,
  "doseNumber": 1
}
```

**Kết quả:**
- Cả 3 CVP được cập nhật thành "Completed"
- Cả 3 CVP được tạo cho mũi 2 với status "Scheduled"

## Lợi ích

### ✅ **Cho User:**
- Ghi nhận đầy đủ hiệu quả bảo vệ của vaccine
- Không cần book riêng cho từng bệnh
- Lịch sử tiêm chủng chính xác và đầy đủ

### ✅ **Cho Hệ thống:**
- Dữ liệu nhất quán và chính xác
- Tối ưu hóa việc sử dụng multi-disease vaccine
- Hỗ trợ báo cáo và thống kê tốt hơn

### ✅ **Cho Y tế:**
- Theo dõi tình trạng miễn dịch toàn diện
- Giảm thiểu việc tiêm thừa vaccine
- Tối ưu hóa lịch tiêm chủng

## Backward Compatibility

- **Single-disease vaccine**: Hoạt động như cũ (chỉ tạo 1 CVP)
- **Dữ liệu cũ**: Không bị ảnh hưởng
- **API**: Không thay đổi interface

## Testing

### Test Case 1: Single Disease Vaccine
```csharp
// Vaccine chỉ chữa 1 bệnh → Tạo 1 CVP
var hepatitisB = new Vaccine { VaccineDiseases = [new VaccineDisease { DiseaseId = 201 }] };
// Expected: 1 ChildVaccineProfile
```

### Test Case 2: Multi Disease Vaccine
```csharp
// MMR vaccine chữa 3 bệnh → Tạo 3 CVP
var mmr = new Vaccine { 
    VaccineDiseases = [
        new VaccineDisease { DiseaseId = 101 }, // Sởi
        new VaccineDisease { DiseaseId = 102 }, // Quai bị
        new VaccineDisease { DiseaseId = 103 }  // Rubella
    ] 
};
// Expected: 3 ChildVaccineProfile với cùng AppointmentId
```

### Test Case 3: Completion
```csharp
// Complete MMR appointment → Cập nhật 3 CVP thành Completed
// Expected: 3 CVP status = "Completed", 3 CVP mới cho mũi tiếp theo
```

## Monitoring và Logging

Hệ thống ghi log chi tiết:
- `🦠 MULTI-DISEASE VACCINE`: Khi phát hiện multi-disease vaccine
- `🎯 Tạo ChildVaccineProfile`: Cho từng disease
- `✅ Cập nhật ChildVaccineProfile`: Khi completion
- `🔄 Tạo next dose`: Cho tất cả diseases

## Lưu ý quan trọng

1. **Performance**: Multi-disease vaccine tạo nhiều CVP hơn → Monitor database performance
2. **Storage**: Tăng số lượng record trong ChildVaccineProfile table
3. **UI/UX**: Frontend cần hiển thị đầy đủ thông tin về tất cả diseases được bảo vệ
4. **Validation**: Đảm bảo không tạo duplicate CVP cho cùng Child-Vaccine-Disease-Dose

