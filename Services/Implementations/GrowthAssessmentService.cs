using AutoMapper;
using Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Interfaces;
using Contracts.DTOs.GrowthAssessment;
using Repositories.Entities;

namespace Services.Implementations
{
    public class GrowthAssessmentService : IGrowthAssessmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GrowthAssessmentService> _logger;
        private readonly IMapper _mapper;

        public GrowthAssessmentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GrowthAssessmentService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<GrowthPredictionDTO> PredictGrowthAsync(int childId, string period = "3months")
        {
            try
            {
                // Lấy thông tin trẻ
                var childRepo = _unitOfWork.GetRepository<Child>();
                var child = await childRepo.GetAsync(c => c.ChildId == childId);
                if (child == null)
                    throw new KeyNotFoundException($"Không tìm thấy trẻ với ID {childId}");

                // Lấy tất cả growth records của trẻ, sắp xếp theo thời gian
                var recordRepo = _unitOfWork.GetRepository<GrowthRecord>();
                var records = await recordRepo.FindAsync(r => r.ChildId == childId);
                var sortedRecords = records.OrderBy(r => r.CreatedAt).ToList();

                if (sortedRecords.Count < 2)
                    throw new InvalidOperationException("Cần ít nhất 2 điểm dữ liệu để dự đoán tăng trưởng");

                // Parse period
                var predictionDays = ParsePeriodToDays(period);
                var timePoints = GenerateTimePoints(period);

                // Sử dụng tối đa 6 điểm gần nhất cho dự đoán
                var recentRecords = sortedRecords.TakeLast(Math.Min(6, sortedRecords.Count)).ToList();
                var lastRecord = recentRecords.Last();

                var prediction = new GrowthPredictionDTO
                {
                    ChildId = childId,
                    ChildName = child.FullName,
                    LastMeasurementDate = lastRecord.CreatedAt,
                    PredictionMethod = "Linear Trend + Growth Velocity",
                    DataPointsUsed = recentRecords.Count
                };

                // Tính xu hướng tuyến tính cho từng chỉ số
                var heightTrend = CalculateLinearTrend(recentRecords, r => (double)r.Height, r => r.CreatedAt);
                var weightTrend = CalculateLinearTrend(recentRecords, r => (double)r.Weight, r => r.CreatedAt);
                var headTrend = CalculateLinearTrend(recentRecords, r => (double)r.HeadCircumference, r => r.CreatedAt);

                // Tạo các điểm dự đoán
                foreach (var timePoint in timePoints)
                {
                    var predictedDate = lastRecord.CreatedAt.AddDays(timePoint.Days);
                    var ageInDays = (int)(predictedDate - child.BirthDate).TotalDays;
                    var ageInMonths = (int)((decimal)ageInDays / 30.44M);

                    // Dự đoán dựa trên linear trend
                    var daysFromLast = timePoint.Days;
                    var predictedHeight = (decimal)(heightTrend.Slope * daysFromLast + (double)lastRecord.Height);
                    var predictedWeight = (decimal)(weightTrend.Slope * daysFromLast + (double)lastRecord.Weight);
                    var predictedHead = (decimal)(headTrend.Slope * daysFromLast + (double)lastRecord.HeadCircumference);

                    // Áp dụng growth velocity adjustment
                    var adjustedPredictions = await ApplyGrowthVelocityAdjustment(
                        child, ageInMonths, predictedHeight, predictedWeight, predictedHead);

                    // Áp dụng realistic constraints (validation cẩn thận)
                    var realisticPredictions = ApplyRealisticConstraints(
                        lastRecord, adjustedPredictions, daysFromLast);

                    // Tính BMI dự đoán
                    var heightInMeters = realisticPredictions.Height / 100;
                    var predictedBMI = Math.Round(realisticPredictions.Weight / (heightInMeters * heightInMeters), 2);

                    prediction.PredictionPoints.Add(new PredictionDataPointDTO
                    {
                        PredictedDate = predictedDate,
                        AgeInDays = ageInDays,
                        PredictedHeight = realisticPredictions.Height,
                        PredictedWeight = realisticPredictions.Weight,
                        PredictedBMI = predictedBMI,
                        PredictedHeadCircumference = realisticPredictions.HeadCircumference,
                        TimeLabel = timePoint.Label
                    });
                }

                prediction.Recommendations = GeneratePredictionRecommendations(recentRecords, prediction.PredictionPoints);

                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi dự đoán tăng trưởng cho trẻ {childId}");
                throw;
            }
        }

        private int ParsePeriodToDays(string period)
        {
            return period.ToLower() switch
            {
                "1day" => 1,
                "1week" => 7,
                "1month" => 30,
                "3months" => 90,
                "6months" => 180,
                "1year" => 365,
                _ => 90 // default 3 months
            };
        }

        private List<(int Days, string Label)> GenerateTimePoints(string period)
        {
            return period.ToLower() switch
            {
                "1day" => new List<(int, string)> { (1, "1 ngày") },
                "1week" => new List<(int, string)> { (7, "1 tuần") },
                "1month" => new List<(int, string)> { (30, "1 tháng") },
                "3months" => new List<(int, string)> { (90, "3 tháng") },
                "6months" => new List<(int, string)> { (180, "6 tháng") },
                "1year" => new List<(int, string)> { (365, "1 năm") },
                _ => new List<(int, string)> { (90, "3 tháng") }
            };
        }

        private (double Slope, double Intercept) CalculateLinearTrend(
            List<GrowthRecord> records, 
            Func<GrowthRecord, double> valueSelector,
            Func<GrowthRecord, DateTime> dateSelector)
        {
            if (records.Count < 2) return (0, 0);

            var baseDate = records.First().CreatedAt;
            var xValues = records.Select(r => (dateSelector(r) - baseDate).TotalDays).ToArray();
            var yValues = records.Select(valueSelector).ToArray();

            var n = xValues.Length;
            var sumX = xValues.Sum();
            var sumY = yValues.Sum();
            var sumXY = xValues.Zip(yValues, (x, y) => x * y).Sum();
            var sumX2 = xValues.Sum(x => x * x);

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;

            return (slope, intercept);
        }

        private async Task<(decimal Height, decimal Weight, decimal HeadCircumference)> ApplyGrowthVelocityAdjustment(
            Child child, int ageInMonths, decimal predictedHeight, decimal predictedWeight, decimal predictedHead)
        {
            try
            {
                // Lấy growth velocity standards (nếu có)
                var standardRepo = _unitOfWork.GetRepository<GrowthStandard>();
                
                string gender = child.Gender?.Trim().ToUpper();
                if (string.IsNullOrEmpty(gender) || (gender != "MALE" && gender != "FEMALE"))
                {
                    return (predictedHeight, predictedWeight, predictedHead);
                }
                gender = char.ToUpper(gender[0]) + gender.Substring(1).ToLower();

                // Tìm chuẩn tăng trưởng gần nhất
                var nearestStandards = await standardRepo.FindAsync(s => 
                    s.Gender == gender && 
                    Math.Abs(s.AgeInMonths - ageInMonths) <= 2);

                if (!nearestStandards.Any())
                {
                    return (predictedHeight, predictedWeight, predictedHead);
                }

                // Áp dụng soft constraints dựa trên WHO standards
                var heightStd = nearestStandards.FirstOrDefault(s => s.Measurement == "Height");
                var weightStd = nearestStandards.FirstOrDefault(s => s.Measurement == "Weight");
                var headStd = nearestStandards.FirstOrDefault(s => s.Measurement == "HeadCircumference");

                // Adjust predictions không cho vượt quá xa chuẩn WHO
                var adjustedHeight = ApplyBounds(predictedHeight, heightStd);
                var adjustedWeight = ApplyBounds(predictedWeight, weightStd);
                var adjustedHead = ApplyBounds(predictedHead, headStd);

                return (adjustedHeight, adjustedWeight, adjustedHead);
            }
            catch
            {
                // Nếu có lỗi, trả về dự đoán gốc
                return (predictedHeight, predictedWeight, predictedHead);
            }
        }

        private decimal ApplyBounds(decimal predictedValue, GrowthStandard standard)
        {
            if (standard == null) return predictedValue;

            // Không cho vượt quá ±3 SD so với median
            var minBound = standard.Sd3neg;
            var maxBound = standard.Sd3pos;

            if (predictedValue < minBound) return minBound + (standard.Median - minBound) * 0.1m;
            if (predictedValue > maxBound) return maxBound - (maxBound - standard.Median) * 0.1m;

            return predictedValue;
        }

        private (decimal Height, decimal Weight, decimal HeadCircumference) ApplyRealisticConstraints(
            GrowthRecord lastRecord, 
            (decimal Height, decimal Weight, decimal HeadCircumference) predictions, 
            int daysFromLast)
        {
            var constrainedHeight = ApplyHeightConstraints(lastRecord.Height, predictions.Height);
            var constrainedWeight = ApplyWeightConstraints(lastRecord.Weight, predictions.Weight, daysFromLast);
            var constrainedHead = ApplyHeadCircumferenceConstraints(lastRecord.HeadCircumference, predictions.HeadCircumference);

            return (constrainedHeight, constrainedWeight, constrainedHead);
        }

        private decimal ApplyHeightConstraints(decimal currentHeight, decimal predictedHeight)
        {
            // Chiều cao chỉ có thể tăng hoặc không đổi, KHÔNG BAO GIỜ GIẢM
            if (predictedHeight < currentHeight)
            {
                _logger.LogWarning($"Dự đoán chiều cao giảm từ {currentHeight} xuống {predictedHeight}, điều chỉnh về không đổi");
                return currentHeight; // Giữ nguyên chiều cao hiện tại
            }

            // Giới hạn tăng trưởng chiều cao hợp lý: tối đa 2cm/tháng cho trẻ nhỏ
            var maxGrowthPerDay = 0.067m; // ~2cm/30days
            var maxAllowedHeight = currentHeight + (maxGrowthPerDay * 30); // Tính theo 30 ngày

            if (predictedHeight > maxAllowedHeight)
            {
                _logger.LogWarning($"Dự đoán chiều cao tăng quá nhanh từ {currentHeight} lên {predictedHeight}, điều chỉnh về {maxAllowedHeight}");
                return maxAllowedHeight;
            }

            return predictedHeight;
        }

        private decimal ApplyWeightConstraints(decimal currentWeight, decimal predictedWeight, int daysFromLast)
        {
            // Cân nặng có thể giảm nhưng không quá 10% mỗi tháng
            var monthsFromLast = (decimal)daysFromLast / 30;
            var maxWeightLossPercent = 0.10m; // 10% mỗi tháng
            var minAllowedWeight = currentWeight * (1 - (maxWeightLossPercent * monthsFromLast));

            if (predictedWeight < minAllowedWeight)
            {
                _logger.LogWarning($"Dự đoán cân nặng giảm quá nhiều từ {currentWeight} xuống {predictedWeight}, điều chỉnh về {minAllowedWeight}");
                return minAllowedWeight;
            }

            // Giới hạn tăng cân tối đa: 1kg/tháng cho trẻ nhỏ
            var maxWeightGainPerMonth = 1.0m;
            var maxAllowedWeight = currentWeight + (maxWeightGainPerMonth * monthsFromLast);

            if (predictedWeight > maxAllowedWeight)
            {
                _logger.LogWarning($"Dự đoán cân nặng tăng quá nhanh từ {currentWeight} lên {predictedWeight}, điều chỉnh về {maxAllowedWeight}");
                return maxAllowedWeight;
            }

            return predictedWeight;
        }

        private decimal ApplyHeadCircumferenceConstraints(decimal currentHead, decimal predictedHead)
        {
            // Vòng đầu chỉ có thể tăng hoặc không đổi, KHÔNG BAO GIỜ GIẢM
            if (predictedHead < currentHead)
            {
                _logger.LogWarning($"Dự đoán vòng đầu giảm từ {currentHead} xuống {predictedHead}, điều chỉnh về không đổi");
                return currentHead; // Giữ nguyên vòng đầu hiện tại
            }

            // Giới hạn tăng trưởng vòng đầu hợp lý: tối đa 1cm/tháng cho trẻ nhỏ
            var maxGrowthPerDay = 0.033m; // ~1cm/30days
            var maxAllowedHead = currentHead + (maxGrowthPerDay * 30); // Tính theo 30 ngày

            if (predictedHead > maxAllowedHead)
            {
                _logger.LogWarning($"Dự đoán vòng đầu tăng quá nhanh từ {currentHead} lên {predictedHead}, điều chỉnh về {maxAllowedHead}");
                return maxAllowedHead;
            }

            return predictedHead;
        }

        private string GeneratePredictionRecommendations(List<GrowthRecord> recentRecords, List<PredictionDataPointDTO> predictions)
        {
            var recommendations = new List<string>();

            // Phân tích xu hướng
            var heightTrend = AnalyzeTrend(recentRecords.Select(r => (double)r.Height).ToList());
            var weightTrend = AnalyzeTrend(recentRecords.Select(r => (double)r.Weight).ToList());

            recommendations.Add("📈 **DỰ ĐOÁN TĂNG TRƯỞNG:**");

            if (heightTrend > 0.15)
                recommendations.Add("- ✅ Chiều cao đang tăng trưởng tốt");
            else if (heightTrend > 0.05)
                recommendations.Add("- 📊 Chiều cao đang tăng trưởng ổn định");
            else
                recommendations.Add("- ⚠️ Tốc độ tăng chiều cao đang chậm lại");

            if (weightTrend > 0.2)
                recommendations.Add("- ✅ Cân nặng đang tăng đều đặn");
            else if (weightTrend > -0.1)
                recommendations.Add("- 📊 Cân nặng đang ổn định");
            else if (weightTrend > -0.3)
                recommendations.Add("- ⚠️ Cân nặng có xu hướng giảm nhẹ (trong giới hạn cho phép)");
            else
                recommendations.Add("- 🚨 Cân nặng có xu hướng giảm đáng lo ngại");



            recommendations.Add("");
            recommendations.Add("💡 **KHUYẾN NGHỊ:**");
            
            if (heightTrend <= 0.05)
            {
                recommendations.Add("- 🥛 Tăng cường canxi và vitamin D cho chiều cao");
                recommendations.Add("- 🏃‍♂️ Khuyến khích vận động và thể dục");
            }

            if (weightTrend < -0.1)
            {
                recommendations.Add("- 🍎 Chú ý dinh dưỡng, tăng cường các món giàu protein");
                recommendations.Add("- 👨‍⚕️ Có thể cần tham vấn bác sĩ nếu cân nặng tiếp tục giảm");
            }

            recommendations.Add("- 📊 Tiếp tục theo dõi định kỳ để dự đoán chính xác hơn");
            
            if (recentRecords.Count < 4)
            {
                recommendations.Add("- 📈 Để có dự đoán chính xác hơn, hãy đo đạc thêm vài lần nữa");
            }

            return string.Join("\n", recommendations);
        }

        private double AnalyzeTrend(List<double> values)
        {
            if (values.Count < 2) return 0;

            var diffs = new List<double>();
            for (int i = 1; i < values.Count; i++)
            {
                diffs.Add(values[i] - values[i - 1]);
            }

            return diffs.Average();
        }

        public async Task<GrowthAssessmentDTO> AssessGrowthAsync(GrowthRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            try
            {
              
                var childRepo = _unitOfWork.GetRepository<Child>();
                var child = await childRepo.GetAsync(c => c.ChildId == record.ChildId);

                if (child == null)
                    throw new KeyNotFoundException($"Không tìm thấy trẻ với ID {record.ChildId}");

                
                string gender = child.Gender?.Trim().ToUpper();
                if (string.IsNullOrEmpty(gender) || (gender != "MALE" && gender != "FEMALE"))
                {
                    throw new InvalidOperationException($"Giới tính không hợp lệ: {child.Gender}");
                }
                gender = char.ToUpper(gender[0]) + gender.Substring(1).ToLower();

                
                int ageInMonths = (int)((decimal)(record.CreatedAt - child.BirthDate).TotalDays / 30.44M);

                
                var standardRepo = _unitOfWork.GetRepository<GrowthStandard>();
                var standards = await standardRepo.FindAsync(s =>
                    s.Gender == gender &&
                    s.AgeInMonths == ageInMonths
                );

                if (!standards.Any())
                {
                    throw new InvalidOperationException($"Không tìm thấy dữ liệu chuẩn cho độ tuổi {ageInMonths} tháng");
                }

                var heightStandard = standards.FirstOrDefault(s => s.Measurement == "Height");
                var weightStandard = standards.FirstOrDefault(s => s.Measurement == "Weight");
                var bmiStandard = standards.FirstOrDefault(s => s.Measurement == "BMI");
                var headStandard = standards.FirstOrDefault(s => s.Measurement == "HeadCircumference");

                var assessment = new GrowthAssessmentDTO
                {
                    RecordId = record.RecordId,
                    ChildId = record.ChildId,
                    MeasurementDate = record.CreatedAt,
                    Height = record.Height,
                    Weight = record.Weight,
                    BMI = record.Bmi,
                    HeadCircumference = record.HeadCircumference,
                    Assessments = new GrowthAssessmentsDTO
                    {
                        HeightStatus = AssessHeightStatus(record.Height, heightStandard),
                        WeightStatus = AssessWeightAndBMIStatus(record.Weight, weightStandard),
                        BMIStatus = AssessWeightAndBMIStatus(record.Bmi, bmiStandard),
                        HeadCircumferenceStatus = AssessHeadCircumferenceStatus(record.HeadCircumference, headStandard)
                    }
                };

                assessment.Recommendations = GenerateRecommendations(assessment.Assessments);

                return assessment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi đánh giá tăng trưởng cho trẻ {record.ChildId}");
                throw;
            }
        }

        private string AssessHeightStatus(decimal height, GrowthStandard standard)
        {
            if (standard == null) return "Không có dữ liệu chuẩn";

            if (height <= standard.Sd3neg) return "Thấp còi nặng";
            if (height <= standard.Sd2neg) return "Thấp còi";
            if (height <= standard.Sd1neg) return "Nguy cơ thấp còi";
            if (height <= standard.Median) return "Bình thường thấp";
            if (height <= standard.Sd1pos) return "Bình thường";
            if (height <= standard.Sd2pos) return "Chiều cao trung bình khá";
            if (height <= standard.Sd3pos) return "Cao";
            return "Rất cao";
        }

        private string AssessWeightAndBMIStatus(decimal value, GrowthStandard standard)
        {
            if (standard == null) return "Không có dữ liệu chuẩn";

            if (value <= standard.Sd3neg) return "Suy dinh dưỡng nặng";
            if (value <= standard.Sd2neg) return "Suy dinh dưỡng";
            if (value <= standard.Sd1neg) return "Nguy cơ suy dinh dưỡng";
            if (value <= standard.Median) return "Bình thường thấp";
            if (value <= standard.Sd1pos) return "Bình thường";
            if (value <= standard.Sd2pos) return "Nguy cơ thừa cân/béo phì";
            if (value <= standard.Sd3pos) return "Béo phì";
            return "Béo phì nặng";
        }

        private string AssessHeadCircumferenceStatus(decimal headCircumference, GrowthStandard standard)
        {
            if (standard == null) return "Không có dữ liệu chuẩn";

            if (headCircumference <= standard.Sd3neg) return "Đầu rất nhỏ (Microcephaly)";
            if (headCircumference <= standard.Sd2neg) return "Đầu hơi nhỏ";
            if (headCircumference <= standard.Sd1neg) return "Bình thường nhỏ";
            if (headCircumference <= standard.Median) return "Bình thường thấp";
            if (headCircumference <= standard.Sd1pos) return "Bình thường";
            if (headCircumference <= standard.Sd2pos) return "Bình thường lớn";
            if (headCircumference <= standard.Sd3pos) return "Đầu hơi to";
            return "Đầu rất to (Macrocephaly)";
        }

        private string GenerateRecommendations(GrowthAssessmentsDTO assessments)
        {
            var recommendations = new List<string>();

            // Đánh giá chiều cao
            if (assessments.HeightStatus.Contains("nặng"))
            {
                recommendations.Add("- Cần đưa trẻ đi khám bác sĩ chuyên khoa nhi gấp");
                recommendations.Add("- Kiểm tra các vấn đề về nội tiết và dinh dưỡng");
            }
            else if (assessments.HeightStatus.Contains("Thấp còi"))
            {
                recommendations.Add("- Cần bổ sung vitamin D và canxi");
                recommendations.Add("- Đảm bảo chế độ ăn đủ protein (thịt, cá, trứng, sữa)");
                recommendations.Add("- Tăng cường vận động ngoài trời");
            }

            // Đánh giá cân nặng và BMI
            if (assessments.WeightStatus.Contains("nặng") || assessments.BMIStatus.Contains("nặng"))
            {
                recommendations.Add("- Cần tham vấn bác sĩ về chế độ ăn phù hợp");
                recommendations.Add("- Theo dõi chế độ ăn và hoạt động thể chất");
            }
            else if (assessments.WeightStatus.Contains("Suy dinh dưỡng") || assessments.BMIStatus.Contains("Suy dinh dưỡng"))
            {
                recommendations.Add("- Cần tăng cường dinh dưỡng");
                recommendations.Add("- Bổ sung các vitamin và khoáng chất cần thiết");
            }

            // Đánh giá vòng đầu
            if (assessments.HeadCircumferenceStatus.Contains("rất"))
            {
                recommendations.Add("- Cần đưa trẻ đi khám chuyên khoa thần kinh");
                recommendations.Add("- Theo dõi sự phát triển của não bộ");
            }

            if (recommendations.Count == 0)
            {
                recommendations.Add("Trẻ đang phát triển bình thường.");
                recommendations.Add("- Tiếp tục duy trì chế độ dinh dưỡng và vận động hiện tại");
            }

            return string.Join("\n", recommendations);
        }
    }
}
