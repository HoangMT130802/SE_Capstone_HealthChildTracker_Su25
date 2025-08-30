# MẪU THUYẾT TRÌNH: HỆ THỐNG ĐÁNH GIÁ TĂNG TRƯỞNG TRẺ EM
## 🍼 Health Child Tracker System

---

## 📋 NỘI DUNG THUYẾT TRÌNH

### 1. TỔNG QUAN HỆ THỐNG
### 2. LUỒNG NHẬP LIỆU CHỈ SỐ TĂNG TRƯỞNG  
### 3. THUẬT TOÁN ĐÁNH GIÁ & Dự ĐOÁN
### 4. CÔNG THỨC TÍNH TOÁN CHI TIẾT
### 5. VÍ DỤ BIỂU ĐỒ VÀ KẾT QUẢ
### 6. RECOMMENDATIONS & LỜI KHUYÊN

---

## 1. 🎯 TỔNG QUAN HỆ THỐNG

### Mục tiêu chính:
- **Theo dõi tăng trưởng**: Chiều cao, cân nặng, BMI, chu vi đầu
- **Đánh giá hiện tại**: So sánh với chuẩn WHO  
- **Dự đoán xu hướng**: Linear trend analysis + Growth velocity
- **Tư vấn y tế**: Recommendations dựa trên kết quả phân tích

### Đối tượng sử dụng:
- **Cha mẹ**: Nhập liệu, xem báo cáo
- **Bác sĩ**: Theo dõi, tư vấn chuyên môn
- **Hệ thống**: Tự động phân tích và cảnh báo

---

## 2. 🔄 LUỒNG NHẬP LIỆU CHỈ SỐ TĂNG TRƯỞNG

### BƯỚC 1: Nhập thông tin đo đạc
```
📏 Dữ liệu cần nhập:
• Chiều cao (30-200cm) 
• Cân nặng (2-100kg)
• Chu vi đầu (30-100cm)
• Ngày đo (không được trong tương lai)
• Ghi chú (tùy chọn)
```

### BƯỚC 2: Validation dữ liệu
```
✅ Kiểm tra:
• Range hợp lệ theo độ tuổi
• Ngày đo >= ngày sinh trẻ
• Ngày đo <= ngày hiện tại
• Logic kiểm tra bất thường
```

### BƯỚC 3: Xử lý & lưu trữ
```
💾 Tự động tính toán:
• BMI = Cân nặng(kg) / [Chiều cao(m)]²
• Ghi đè nếu cùng ngày đo
• Lưu vào database với timestamp
```

### BƯỚC 4: Phân tích & đánh giá
```
📊 Hai luồng song song:
1. Đánh giá hiện tại (so với WHO)
2. Dự đoán tăng trưởng (cần ≥2 điểm dữ liệu)
```

---

## 3. 🧮 THUẬT TOÁN ĐÁNH GIÁ & DỰ ĐOÁN

### A. ĐÁNH GIÁ HIỆN TẠI

#### So sánh với chuẩn WHO:
```
🎯 Phương pháp Z-Score:
Z = (Giá trị đo - Median WHO) / Standard Deviation

📊 Phân loại:
• < -3 SD: Rất thấp      | Cần can thiệp y tế
• -3 đến -2 SD: Thấp    | Theo dõi sát
• -2 đến -1 SD: Hơi thấp | Lưu ý dinh dưỡng  
• -1 đến +1 SD: Bình thường | Duy trì hiện tại
• +1 đến +2 SD: Hơi cao | Theo dõi
• +2 đến +3 SD: Cao    | Cần tư vấn
• > +3 SD: Rất cao      | Can thiệp y tế
```

### B. DỰ ĐOÁN TĂNG TRƯỞNG

#### Thuật toán Linear Trend Analysis:
```
📈 Công thức hồi quy tuyến tính:
y = ax + b

Trong đó:
• a (slope) = (n∑xy - ∑x∑y) / (n∑x² - (∑x)²)  
• b (intercept) = (∑y - a∑x) / n
• x = số ngày từ điểm gốc
• y = giá trị chỉ số (chiều cao/cân nặng/chu vi đầu)
```

#### Growth Velocity Adjustment:
```
🚀 Điều chỉnh tốc độ tăng trưởng:
1. Tính tốc độ trung bình từ 6 điểm gần nhất
2. So sánh với chuẩn WHO cho độ tuổi
3. Áp dụng soft constraints (-3SD đến +3SD)
4. Realistic bounds (không cho phép thay đổi đột ngột)
```

---

## 4. 📊 CÔNG THỨC TÍNH TOÁN CHI TIẾT

### CÔNG THỨC BMI:
```
BMI = Cân nặng (kg) / [Chiều cao (m)]²

Ví dụ: 
• Cân nặng = 12.5kg
• Chiều cao = 89cm = 0.89m  
• BMI = 12.5 / (0.89)² = 15.8
```

### CÔNG THỨC DỰ ĐOÁN:
```
Giá trị dự đoán = Giá trị hiện tại + (Slope × Số ngày)

Ví dụ dự đoán chiều cao sau 90 ngày:
• Chiều cao hiện tại = 89cm
• Slope = 0.027 cm/ngày (từ Linear Trend)
• Dự đoán = 89 + (0.027 × 90) = 91.4cm
```

### VALIDATION BOUNDS:
```
🛡️ Kiểm tra giới hạn:
• Min = WHO Median - 3×SD cho độ tuổi
• Max = WHO Median + 3×SD cho độ tuổi
• Nếu dự đoán < Min hoặc > Max → Điều chỉnh về giới hạn
```

---

## 5. 📈 VÍ DỤ BIỂU ĐỒ VÀ KẾT QUẢ

### VÍ DỤ: Bé Nam 24 tháng tuổi

#### Dữ liệu đầu vào:
```
📊 Lịch sử đo đạc (6 tháng gần nhất):
Tháng 1: 82.0cm, 11.2kg, BMI 16.7
Tháng 2: 83.5cm, 11.8kg, BMI 16.9  
Tháng 3: 84.8cm, 12.0kg, BMI 16.7
Tháng 4: 86.2cm, 12.2kg, BMI 16.4
Tháng 5: 87.5cm, 12.3kg, BMI 16.1
Tháng 6: 88.9cm, 12.5kg, BMI 15.8
```

#### Kết quả đánh giá hiện tại:
```
🎯 So với chuẩn WHO (24 tháng, Nam):
• Chiều cao: 88.9cm → Bình thường (P25-P50) 
• Cân nặng: 12.5kg → Bình thường (P50)
• BMI: 15.8 → Bình thường (P25-P50)
• Chu vi đầu: 48.2cm → Bình thường (P50)
```

#### Dự đoán 3 tháng tới:
```
🔮 Linear Trend Analysis:
• Slope chiều cao = 0.027 cm/ngày
• Slope cân nặng = 0.014 kg/ngày
• Dự đoán sau 90 ngày:
  - Chiều cao: 91.4cm (vẫn trong P25-P75)
  - Cân nặng: 13.2kg (vẫn ổn định)
  - BMI: 15.8 (xu hướng ổn định)
```

---

## 6. 💡 RECOMMENDATIONS & LỜI KHUYÊN

### LOGIC TẠO LỜI KHUYÊN:

#### A. Dựa trên đánh giá hiện tại:
```
📊 Nếu các chỉ số BÌNH THƯỜNG:
✅ "Trẻ đang phát triển tốt, tiếp tục duy trì chế độ dinh dưỡng hiện tại"
✅ "Tăng cường hoạt động vận động phù hợp độ tuổi"
✅ "Theo dõi định kỳ 3 tháng/lần"

⚠️ Nếu có chỉ số THẤP/CAO:
🚨 "Cân nặng thấp - Tăng cường protein và calorie"
🚨 "BMI cao - Kiểm soát đường và tinh bột, tăng vận động"
🚨 "Chu vi đầu bất thường - Cần thăm khám bác sĩ nhi khoa"
```

#### B. Dựa trên xu hướng dự đoán:
```
📈 Trend TÍCH CỰC:
✅ "Xu hướng tăng trưởng ổn định, duy trì chế độ hiện tại"

📉 Trend TIÊU CỰC:  
⚠️ "Tốc độ tăng trưởng chậm lại, cần tăng cường dinh dưỡng"
🚨 "Cảnh báo: Xu hướng giảm cân - Cần thăm khám y tế"
```

#### C. Recommendations cụ thể theo độ tuổi:
```
👶 0-6 tháng: Tập trung sữa mẹ/sữa công thức
🍼 6-12 tháng: Ăn dặm đa dạng, theo dõi dị ứng
🧒 1-2 tuổi: Dinh dưỡng cân bằng, hoạt động vận động
👦 2+ tuổi: Thói quen ăn uống lành mạnh, vận động đều đặn
```

---

## 7. 🔧 TECHNICAL IMPLEMENTATION

### Database Tables liên quan:
```sql
GrowthRecord: Lưu các điểm đo
GrowthStandard: Chuẩn WHO theo tuổi/giới tính  
Child: Thông tin trẻ (ngày sinh, giới tính)
```

### API Endpoints chính:
```
POST /api/growth-records/{childId} - Tạo record mới
GET /api/growth-assessment/{childId} - Đánh giá hiện tại  
GET /api/growth-prediction/{childId}?days=90 - Dự đoán
```

### Error Handling:
```
• Ít hơn 2 điểm dữ liệu → Không thể dự đoán
• Ngày không hợp lệ → Validation error
• Thiếu chuẩn WHO → Fallback message
• Dữ liệu bất thường → Medical consultation warning
```

---

## 8. 📱 UI/UX RECOMMENDATIONS

### Dashboard Layout:
```
1. 📊 Bảng điều khiển tổng quan
   - Chỉ số hiện tại với màu sắc trạng thái
   - Trend arrows (↗️↘️➡️)
   
2. 📈 Biểu đồ tăng trưởng  
   - Line chart với WHO percentiles
   - Prediction dotted line
   - Interactive tooltips

3. 💡 Recommendations panel
   - Icon-based categories
   - Action items with priorities  
   - Medical alerts highlighted
```

### Mobile Responsiveness:
```
📱 Card-based layout
🎨 Color coding cho trạng thái
📊 Simplified charts cho mobile
🔔 Push notifications cho cảnh báo
```

---

## 9. 🎯 KEY TAKEAWAYS

### Điểm mạnh của hệ thống:
✅ **Dữ liệu WHO chuẩn**: Đánh giá chính xác theo chuẩn quốc tế  
✅ **Thuật toán dự đoán**: Linear trend + Growth velocity adjustment  
✅ **Validation chặt chẽ**: Nhiều lớp kiểm tra dữ liệu  
✅ **Recommendations thông minh**: Dựa trên nhiều yếu tố  
✅ **User-friendly**: Interface trực quan, dễ hiểu  

### Limitations cần lưu ý:
⚠️ **Cần ít nhất 2 điểm dữ liệu** để dự đoán  
⚠️ **Linear model đơn giản** - không phản ánh growth spurts  
⚠️ **Disclaimer y tế bắt buộc** - không thay thế bác sĩ  
⚠️ **Phụ thuộc chất lượng dữ liệu** đầu vào từ cha mẹ  

---

## 📞 CONTACT & SUPPORT

**Development Team**: SE Capstone Health Child Tracker  
**Technical Stack**: .NET Core, Entity Framework, AutoMapper  
**Standards**: WHO Growth Charts 2006  
**Last Updated**: {{current_date}}

---

*📝 Ghi chú: Tài liệu này được tạo từ phân tích source code thực tế của hệ thống Health Child Tracker.*

