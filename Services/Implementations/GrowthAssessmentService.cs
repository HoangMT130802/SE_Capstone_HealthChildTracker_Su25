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
        private readonly IAIRecommendationService _aiService;

        public GrowthAssessmentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GrowthAssessmentService> logger,
            IAIRecommendationService aiService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _aiService = aiService;
        }

        public async Task<GrowthPredictionDTO> PredictGrowthAsync(int childId, int days = 90)
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

                // Tạo mốc dự đoán từ số ngày
                var timePoints = GenerateTimePoints(days);

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

                // Tính toán chất lượng dự đoán và thêm disclaimer
                prediction.PredictionQuality = CalculatePredictionQuality(recentRecords, days);
                prediction.RequiresMedicalConsultation = DetermineIfMedicalConsultationRequired(recentRecords, prediction.PredictionQuality);
                prediction.DataLimitations = GetDataLimitations(recentRecords, days);

                // Tính xu hướng tuyến tính cho từng chỉ số
                var heightTrend = CalculateLinearTrend(recentRecords, r => (double)r.Height, r => r.CreatedAt);
                var weightTrend = CalculateLinearTrend(recentRecords, r => (double)r.Weight, r => r.CreatedAt);
                var headTrend = CalculateLinearTrend(recentRecords, r => (double)r.HeadCircumference, r => r.CreatedAt);

                // Tạo điểm dự đoán duy nhất theo số ngày yêu cầu
                foreach (var timePoint in timePoints)
                {
                    var predictedDate = lastRecord.CreatedAt.AddDays(timePoint.Days);
                    var ageInDays = (int)(predictedDate - child.BirthDate).TotalDays;
                    var ageInMonths = (int)((decimal)ageInDays / 30.44M);

                    // Dự đoán dựa trên linear trend
                    var daysFromLast = timePoint.Days;
                    var predictedHeight = Math.Round((decimal)(heightTrend.Slope * daysFromLast + (double)lastRecord.Height), 2);
                    var predictedWeight = Math.Round((decimal)(weightTrend.Slope * daysFromLast + (double)lastRecord.Weight), 2);
                    var predictedHead = Math.Round((decimal)(headTrend.Slope * daysFromLast + (double)lastRecord.HeadCircumference), 2);

                    // Áp dụng growth velocity adjustment
                    var adjustedPredictions = await ApplyGrowthVelocityAdjustment(
                        child, ageInMonths, predictedHeight, predictedWeight, predictedHead);

                    // Áp dụng realistic constraints (validation cẩn thận)
                    var realisticPredictions = ApplyRealisticConstraints(
                        lastRecord, adjustedPredictions, daysFromLast);

                    // Làm tròn tất cả giá trị để đẹp
                    realisticPredictions = (
                        Math.Round(realisticPredictions.Height, 2),
                        Math.Round(realisticPredictions.Weight, 2),
                        Math.Round(realisticPredictions.HeadCircumference, 2)
                    );

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

                // ✅ Sử dụng AI để tạo recommendations
                prediction.Recommendations = await GenerateAIRecommendations(recentRecords, prediction.PredictionPoints, prediction.PredictionQuality, child);

                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi dự đoán tăng trưởng cho trẻ {childId}");
                throw;
            }
        }

        private List<(int Days, string Label)> GenerateTimePoints(int days)
        {
            string label = days switch
            {
                1 => "1 ngày",
                7 => "1 tuần",
                30 => "1 tháng",
                90 => "3 tháng",
                180 => "6 tháng",
                365 => "1 năm",
                _ => $"{days} ngày"
            };
            return new List<(int, string)> { (days, label) };
        }

        private (double Slope, double Intercept) CalculateLinearTrend(
            List<GrowthRecord> records, 
            Func<GrowthRecord, double> valueSelector,
            Func<GrowthRecord, DateTime> dateSelector)
        {
            if (records.Count < 2) return (0, 0);

            var baseDate = records.First().CreatedAt;
            var xValues = records.Select(r => (dateSelector(r) - baseDate).TotalDays + 1).ToArray();
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
                // ✅ IMPROVED: Sử dụng GrowthVelocity standards thay vì GrowthStandard
                var velocityRepo = _unitOfWork.GetRepository<GrowthVelocity>();
                
                string gender = child.Gender?.Trim().ToUpper();
                if (string.IsNullOrEmpty(gender) || (gender != "MALE" && gender != "FEMALE"))
                {
                    return (predictedHeight, predictedWeight, predictedHead);
                }
                gender = char.ToUpper(gender[0]) + gender.Substring(1).ToLower();

                // Tìm growth velocity standards gần nhất
                var heightVelocity = await velocityRepo.GetAsync(s => 
                    s.Gender == gender && 
                    s.Measurement == "Height" &&
                    s.AgeInMonths == ageInMonths);

                var weightVelocity = await velocityRepo.GetAsync(s => 
                    s.Gender == gender && 
                    s.Measurement == "Weight" &&
                    s.AgeInMonths == ageInMonths);

                var headVelocity = await velocityRepo.GetAsync(s => 
                    s.Gender == gender && 
                    s.Measurement == "HeadCircumference" &&
                    s.AgeInMonths == ageInMonths);

                // Nếu không có velocity standards, fallback về growth standards cũ
                if (heightVelocity == null && weightVelocity == null && headVelocity == null)
                {
                    _logger.LogWarning("Không tìm thấy GrowthVelocity standards, sử dụng GrowthStandard fallback");
                    return await ApplyGrowthStandardFallback(child, ageInMonths, predictedHeight, predictedWeight, predictedHead);
                }

                // ✅ Áp dụng velocity-based constraints
                var adjustedHeight = ApplyVelocityConstraints(predictedHeight, heightVelocity, "Height");
                var adjustedWeight = ApplyVelocityConstraints(predictedWeight, weightVelocity, "Weight");
                var adjustedHead = ApplyVelocityConstraints(predictedHead, headVelocity, "HeadCircumference");

                return (adjustedHeight, adjustedWeight, adjustedHead);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi áp dụng GrowthVelocity adjustment, sử dụng fallback");
                // Fallback về growth standards cũ
                return await ApplyGrowthStandardFallback(child, ageInMonths, predictedHeight, predictedWeight, predictedHead);
            }
        }

        // ✅ Fallback method sử dụng GrowthStandard cũ
        private async Task<(decimal Height, decimal Weight, decimal HeadCircumference)> ApplyGrowthStandardFallback(
            Child child, int ageInMonths, decimal predictedHeight, decimal predictedWeight, decimal predictedHead)
        {
            try
            {
                var standardRepo = _unitOfWork.GetRepository<GrowthStandard>();
                
                string gender = char.ToUpper(child.Gender[0]) + child.Gender.Substring(1).ToLower();

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

        // ✅ Method mới sử dụng GrowthVelocity
        private decimal ApplyVelocityConstraints(decimal predictedValue, GrowthVelocity velocity, string measurement)
        {
            if (velocity == null) return predictedValue;

            // Tính toán giới hạn dựa trên velocity standards
            var maxGrowthPerMonth = velocity.Sd2pos; // Giới hạn tối đa ở +2SD
            var minGrowthPerMonth = velocity.Sd2neg; // Giới hạn tối thiểu ở -2SD

            // Chuyển đổi từ growth per month sang growth per day
            var maxGrowthPerDay = maxGrowthPerMonth / 30.44m;
            var minGrowthPerDay = minGrowthPerMonth / 30.44m;

            decimal result = predictedValue;

            // Áp dụng constraints dựa trên loại measurement
            switch (measurement)
            {
                case "Height":
                    // Chiều cao chỉ có thể tăng hoặc không đổi
                    if (predictedValue < 0) result = 0;
                    else if (predictedValue > maxGrowthPerDay * 30) result = maxGrowthPerDay * 30;
                    break;

                case "Weight":
                    // Cân nặng có thể tăng hoặc giảm trong giới hạn
                    if (predictedValue < minGrowthPerDay * 30) result = minGrowthPerDay * 30;
                    else if (predictedValue > maxGrowthPerDay * 30) result = maxGrowthPerDay * 30;
                    break;

                case "HeadCircumference":
                    // Vòng đầu chỉ có thể tăng hoặc không đổi
                    if (predictedValue < 0) result = 0;
                    else if (predictedValue > maxGrowthPerDay * 30) result = maxGrowthPerDay * 30;
                    break;
            }

            return Math.Round(result, 2);
        }

        private decimal ApplyBounds(decimal predictedValue, GrowthStandard standard)
        {
            if (standard == null) return predictedValue;

            // Không cho vượt quá ±3 SD so với median
            var minBound = standard.Sd3neg;
            var maxBound = standard.Sd3pos;

            decimal result;
            if (predictedValue < minBound) 
                result = minBound + (standard.Median - minBound) * 0.1m;
            else if (predictedValue > maxBound) 
                result = maxBound - (maxBound - standard.Median) * 0.1m;
            else 
                result = predictedValue;

            return Math.Round(result, 2);
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
                return Math.Round(currentHeight, 2); // Giữ nguyên chiều cao hiện tại
            }

            // Giới hạn tăng trưởng chiều cao hợp lý: tối đa 2cm/tháng cho trẻ nhỏ
            var maxGrowthPerDay = 0.067m; // ~2cm/30days
            var maxAllowedHeight = currentHeight + (maxGrowthPerDay * 30); // Tính theo 30 ngày

            if (predictedHeight > maxAllowedHeight)
            {
                _logger.LogWarning($"Dự đoán chiều cao tăng quá nhanh từ {currentHeight} lên {predictedHeight}, điều chỉnh về {maxAllowedHeight}");
                return Math.Round(maxAllowedHeight, 2);
            }

            return Math.Round(predictedHeight, 2);
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
                return Math.Round(minAllowedWeight, 2);
            }

            // Giới hạn tăng cân tối đa: 1kg/tháng cho trẻ nhỏ
            var maxWeightGainPerMonth = 1.0m;
            var maxAllowedWeight = currentWeight + (maxWeightGainPerMonth * monthsFromLast);

            if (predictedWeight > maxAllowedWeight)
            {
                _logger.LogWarning($"Dự đoán cân nặng tăng quá nhanh từ {currentWeight} lên {predictedWeight}, điều chỉnh về {maxAllowedWeight}");
                return Math.Round(maxAllowedWeight, 2);
            }

            return Math.Round(predictedWeight, 2);
        }

        private decimal ApplyHeadCircumferenceConstraints(decimal currentHead, decimal predictedHead)
        {
            // Vòng đầu chỉ có thể tăng hoặc không đổi, KHÔNG BAO GIỜ GIẢM
            if (predictedHead < currentHead)
            {
                _logger.LogWarning($"Dự đoán vòng đầu giảm từ {currentHead} xuống {predictedHead}, điều chỉnh về không đổi");
                return Math.Round(currentHead, 2); // Giữ nguyên vòng đầu hiện tại
            }

            // Giới hạn tăng trưởng vòng đầu hợp lý: tối đa 1cm/tháng cho trẻ nhỏ
            var maxGrowthPerDay = 0.033m; // ~1cm/30days
            var maxAllowedHead = currentHead + (maxGrowthPerDay * 30); // Tính theo 30 ngày

            if (predictedHead > maxAllowedHead)
            {
                _logger.LogWarning($"Dự đoán vòng đầu tăng quá nhanh từ {currentHead} lên {predictedHead}, điều chỉnh về {maxAllowedHead}");
                return Math.Round(maxAllowedHead, 2);
            }

            return Math.Round(predictedHead, 2);
        }

        private async Task<string> GenerateEnhancedRecommendations(List<GrowthRecord> recentRecords, List<PredictionDataPointDTO> predictions, PredictionQualityDTO quality, Child child)
        {
            var recommendations = new List<string>();

            // ĐÁNH GIA TÌNH TRẠNG HIỆN TẠI (DỰA TRÊN DỮ LIỆU THỰC TẾ)
            var latestRecord = recentRecords.Last();
            var currentAssessment = await AssessCurrentStatus(latestRecord, child);
            
            recommendations.Add("📊 **TÌNH TRẠNG HIỆN TẠI** (dựa trên chuẩn WHO)");
            recommendations.Add($"- Chiều cao: {currentAssessment.HeightStatus}");
            recommendations.Add($"- Cân nặng: {currentAssessment.WeightStatus}");
            recommendations.Add($"- BMI: {currentAssessment.BMIStatus}");
            recommendations.Add($"- Vòng đầu: {currentAssessment.HeadCircumferenceStatus}");
            recommendations.Add("");

            // PHÂN TÍCH XU HƯỚNG TĂNG TRƯỞNG
            var heightTrend = AnalyzeTrend(recentRecords.Select(r => (double)r.Height).ToList());
            var weightTrend = AnalyzeTrend(recentRecords.Select(r => (double)r.Weight).ToList());

            recommendations.Add("📈 **DỰ ĐOÁN TĂNG TRƯỞNG**");

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

            // KHUYẾN NGHỊ Y KHOA DỰA TRÊN TÌNH TRẠNG
            recommendations.Add("💡 **KHUYẾN NGHỊ**");
            
            // Khuyến nghị dựa trên xu hướng
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

            // Khuyến nghị dựa trên assessment hiện tại
            var medicalRecommendations = GenerateMedicalRecommendations(currentAssessment, heightTrend, weightTrend);
            recommendations.AddRange(medicalRecommendations);

            recommendations.Add("- 📊 Tiếp tục theo dõi định kỳ để dự đoán chính xác hơn");
            
            if (recentRecords.Count < 4)
            {
                recommendations.Add("- 📈 Để có dự đoán chính xác hơn, hãy đo đạc thêm vài lần nữa");
            }

            // THÔNG TIN CHẤT LƯỢNG DỰ ĐOÁN
            recommendations.Add("");
            recommendations.Add("🎯 **THÔNG TIN DỰ ĐOÁN**");
            recommendations.Add($"- 📊 Độ tin cậy: {quality.ConfidenceLevel} ({quality.OverallConfidence:F1}%)");
            recommendations.Add($"- 📈 Chất lượng dữ liệu: {quality.DataQualityDescription}");
            
            if (quality.QualityWarnings.Any())
            {
                recommendations.Add("- ⚠️ **Cảnh báo:**");
                foreach (var warning in quality.QualityWarnings)
                {
                    recommendations.Add($"  {warning}");
                }
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

        private PredictionQualityDTO CalculatePredictionQuality(List<GrowthRecord> records, int predictionDays)
        {
            var quality = new PredictionQualityDTO
            {
                DataPointsUsed = records.Count
            };

            // Tính điểm dựa trên số lượng dữ liệu
            var dataScore = Math.Min(records.Count * 16.67, 100); // 6 điểm = 100%

            // Tính độ nhất quán xu hướng
            var heightValues = records.Select(r => (double)r.Height).ToList();
            var weightValues = records.Select(r => (double)r.Weight).ToList();
            var heightConsistency = CalculateConsistency(heightValues);
            var weightConsistency = CalculateConsistency(weightValues);
            quality.TrendConsistency = (heightConsistency + weightConsistency) / 2;

            // Tính điểm thời gian
            var timeSpan = (records.Last().CreatedAt - records.First().CreatedAt).TotalDays;
            var timeScore = Math.Min(timeSpan / 180 * 100, 100); // Tối ưu ở 6 tháng

            // Điểm dự đoán (càng xa càng kém tin cậy)
            var predictionScore = Math.Max(100 - (predictionDays / 30.0 * 15), 20); // Giảm 15% mỗi tháng

            quality.OverallConfidence = (dataScore + quality.TrendConsistency + timeScore + predictionScore) / 4;
            
            // Xác định mức độ tin cậy
            quality.ConfidenceLevel = quality.OverallConfidence switch
            {
                >= 80 => "Cao",
                >= 60 => "Trung bình",
                >= 40 => "Thấp",
                _ => "Rất thấp"
            };

            quality.DataQualityDescription = records.Count switch
            {
                >= 6 => "Đủ dữ liệu cho dự đoán tin cậy",
                >= 4 => "Dữ liệu khá tốt",
                >= 2 => "Dữ liệu tối thiểu - cần thêm điểm đo",
                _ => "Không đủ dữ liệu"
            };

            // Thêm cảnh báo chất lượng
            if (quality.OverallConfidence < 60)
                quality.QualityWarnings.Add("⚠️ Độ tin cậy dự đoán thấp - cần thêm dữ liệu");
            
            if (records.Count < 4)
                quality.QualityWarnings.Add("📊 Cần ít nhất 4-6 điểm đo để có dự đoán chính xác");
            
            if (predictionDays > 180)
                quality.QualityWarnings.Add("⏰ Dự đoán xa (>6 tháng) có độ chính xác thấp");

            return quality;
        }

        private double CalculateConsistency(List<double> values)
        {
            if (values.Count < 3) return 50; // Điểm trung bình nếu không đủ dữ liệu

            var diffs = new List<double>();
            for (int i = 1; i < values.Count; i++)
            {
                diffs.Add(values[i] - values[i - 1]);
            }

            var avgDiff = diffs.Average();
            var variance = diffs.Select(d => Math.Pow(d - avgDiff, 2)).Average();
            var standardDev = Math.Sqrt(variance);

            // Điểm nhất quán: thấp hơn nếu có biến động lớn
            var consistencyScore = Math.Max(100 - (standardDev * 20), 0);
            return Math.Min(consistencyScore, 100);
        }

        private bool DetermineIfMedicalConsultationRequired(List<GrowthRecord> records, PredictionQualityDTO quality)
        {
            // Yêu cầu tham vấn y tế nếu:
            // 1. Độ tin cậy thấp
            if (quality.OverallConfidence < 50) return true;

            // 2. Xu hướng bất thường
            var heightTrend = AnalyzeTrend(records.Select(r => (double)r.Height).ToList());
            var weightTrend = AnalyzeTrend(records.Select(r => (double)r.Weight).ToList());
            
            if (heightTrend < 0 || weightTrend < -0.5) return true; // Giảm chiều cao hoặc giảm cân nhanh

            // 3. Dữ liệu không đủ
            if (records.Count < 3) return true;

            return false;
        }

        private List<string> GetDataLimitations(List<GrowthRecord> records, int predictionDays)
        {
            var limitations = new List<string>();

            if (records.Count < 4)
                limitations.Add("• Số điểm dữ liệu ít - độ chính xác hạn chế");

            if (predictionDays > 90)
                limitations.Add("• Dự đoán dài hạn - độ tin cậy giảm theo thời gian");

            var timeSpan = (records.Last().CreatedAt - records.First().CreatedAt).TotalDays;
            if (timeSpan < 60)
                limitations.Add("• Khoảng thời gian quan sát ngắn - có thể bỏ lỡ xu hướng dài hạn");

            limitations.Add("• Không tính đến yếu tố di truyền, môi trường, bệnh lý");
            limitations.Add("• Dựa trên mô hình toán học đơn giản - không thay thế đánh giá y khoa");

            return limitations;
        }

        private async Task<GrowthAssessmentsDTO> AssessCurrentStatus(GrowthRecord record, Child child)
        {
            try
            {
                string gender = child.Gender?.Trim().ToUpper();
                if (string.IsNullOrEmpty(gender) || (gender != "MALE" && gender != "FEMALE"))
                {
                    return new GrowthAssessmentsDTO
                    {
                        HeightStatus = "Không có dữ liệu chuẩn",
                        WeightStatus = "Không có dữ liệu chuẩn",
                        BMIStatus = "Không có dữ liệu chuẩn",
                        HeadCircumferenceStatus = "Không có dữ liệu chuẩn"
                    };
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
                    return new GrowthAssessmentsDTO
                    {
                        HeightStatus = "Không có dữ liệu chuẩn cho độ tuổi này",
                        WeightStatus = "Không có dữ liệu chuẩn cho độ tuổi này",
                        BMIStatus = "Không có dữ liệu chuẩn cho độ tuổi này",
                        HeadCircumferenceStatus = "Không có dữ liệu chuẩn cho độ tuổi này"
                    };
                }

                var heightStandard = standards.FirstOrDefault(s => s.Measurement == "Height");
                var weightStandard = standards.FirstOrDefault(s => s.Measurement == "Weight");
                var bmiStandard = standards.FirstOrDefault(s => s.Measurement == "BMI");
                var headStandard = standards.FirstOrDefault(s => s.Measurement == "HeadCircumference");

                return new GrowthAssessmentsDTO
                {
                    HeightStatus = AssessHeightStatus(record.Height, heightStandard),
                    WeightStatus = AssessWeightAndBMIStatus(record.Weight, weightStandard),
                    BMIStatus = AssessWeightAndBMIStatus(record.Bmi, bmiStandard),
                    HeadCircumferenceStatus = AssessHeadCircumferenceStatus(record.HeadCircumference, headStandard)
                };
            }
            catch
            {
                return new GrowthAssessmentsDTO
                {
                    HeightStatus = "Lỗi đánh giá",
                    WeightStatus = "Lỗi đánh giá",
                    BMIStatus = "Lỗi đánh giá",
                    HeadCircumferenceStatus = "Lỗi đánh giá"
                };
            }
        }

        private List<string> GenerateMedicalRecommendations(GrowthAssessmentsDTO assessment, double heightTrend, double weightTrend)
        {
            var recommendations = new List<string>();

            // Khuyến nghị dựa trên tình trạng chiều cao
            if (assessment.HeightStatus.Contains("Thấp còi nặng"))
            {
                recommendations.Add("- 🏥 Cần đưa trẻ đi khám bác sĩ chuyên khoa nhi gấp");
                recommendations.Add("- 🔬 Kiểm tra các vấn đề về nội tiết và dinh dưỡng");
                recommendations.Add("- 💊 Có thể cần bổ sung hormone tăng trưởng theo chỉ định bác sĩ");
            }
            else if (assessment.HeightStatus.Contains("Thấp còi"))
            {
                recommendations.Add("- 🥛 Cần bổ sung vitamin D và canxi");
                recommendations.Add("- 🍖 Đảm bảo chế độ ăn đủ protein (thịt, cá, trứng, sữa)");
                recommendations.Add("- 🌞 Tăng cường vận động ngoài trời");
                recommendations.Add("- 👨‍⚕️ Tham vấn bác sĩ nhi khoa về nguyên nhân");
            }
            else if (assessment.HeightStatus.Contains("Nguy cơ thấp còi"))
            {
                recommendations.Add("- 🥛 Tăng cường canxi và vitamin D cho chiều cao");
                recommendations.Add("- 🏃‍♂️ Khuyến khích vận động và thể dục");
            }

            // Khuyến nghị dựa trên tình trạng cân nặng và BMI
            if (assessment.WeightStatus.Contains("Suy dinh dưỡng nặng") || assessment.BMIStatus.Contains("Suy dinh dưỡng nặng"))
            {
                recommendations.Add("- 🏥 Cần nhập viện điều trị dinh dưỡng ngay lập tức");
                recommendations.Add("- 💊 Bổ sung các vitamin và khoáng chất theo chỉ định bác sĩ");
                recommendations.Add("- 🍼 Có thể cần sữa công thức đặc biệt");
            }
            else if (assessment.WeightStatus.Contains("Suy dinh dưỡng") || assessment.BMIStatus.Contains("Suy dinh dưỡng"))
            {
                recommendations.Add("- 🍎 Cần tăng cường dinh dưỡng ngay");
                recommendations.Add("- 🥩 Bổ sung protein chất lượng cao");
                recommendations.Add("- 💊 Bổ sung các vitamin và khoáng chất cần thiết");
                recommendations.Add("- 👨‍⚕️ Tham vấn bác sĩ dinh dưỡng");
            }
            else if (assessment.WeightStatus.Contains("Béo phì nặng") || assessment.BMIStatus.Contains("Béo phì nặng"))
            {
                recommendations.Add("- 🏥 Cần tham vấn bác sĩ nhi khoa và dinh dưỡng ngay");
                recommendations.Add("- 🥗 Điều chỉnh chế độ ăn theo hướng dẫn chuyên gia");
                recommendations.Add("- 🏃‍♂️ Tăng cường hoạt động thể chất phù hợp");
                recommendations.Add("- 🔬 Kiểm tra các vấn đề về chuyển hóa");
            }
            else if (assessment.WeightStatus.Contains("Béo phì") || assessment.BMIStatus.Contains("Béo phì"))
            {
                recommendations.Add("- 👨‍⚕️ Cần tham vấn bác sĩ về chế độ ăn phù hợp");
                recommendations.Add("- 🏃‍♂️ Tăng cường vận động hàng ngày");
                recommendations.Add("- 🥗 Giảm đồ ăn nhiều đường và chất béo");
            }
            else if (assessment.WeightStatus.Contains("Nguy cơ") || assessment.BMIStatus.Contains("Nguy cơ"))
            {
                recommendations.Add("- 📊 Theo dõi chế độ ăn và hoạt động thể chất");
                recommendations.Add("- 👨‍⚕️ Tham khảo ý kiến bác sĩ nếu cần");
            }

            // Khuyến nghị dựa trên vòng đầu
            if (assessment.HeadCircumferenceStatus.Contains("Đầu rất nhỏ") || 
                assessment.HeadCircumferenceStatus.Contains("Microcephaly"))
            {
                recommendations.Add("- 🧠 Cần đưa trẻ đi khám chuyên khoa thần kinh nhi ngay");
                recommendations.Add("- 🔬 Cần các xét nghiệm chẩn đoán hình ảnh não bộ");
                recommendations.Add("- 👨‍⚕️ Theo dõi sát sự phát triển trí tuệ và vận động");
            }
            else if (assessment.HeadCircumferenceStatus.Contains("Đầu rất to") || 
                     assessment.HeadCircumferenceStatus.Contains("Macrocephaly"))
            {
                recommendations.Add("- 🧠 Cần đưa trẻ đi khám chuyên khoa thần kinh nhi ngay");
                recommendations.Add("- 🔬 Kiểm tra áp lực nội sọ và não úng thủy");
                recommendations.Add("- 📊 Theo dõi sự phát triển của não bộ");
            }

            return recommendations;
        }

        private List<string> GenerateStatusBasedRecommendations(GrowthAssessmentsDTO assessment, double heightTrend, double weightTrend)
        {
            var recommendations = new List<string>();

            // Dựa trên tình trạng chiều cao
            if (assessment.HeightStatus.Contains("Thấp còi") || assessment.HeightStatus.Contains("thấp còi"))
            {
                recommendations.Add("- 📏 Tình trạng chiều cao cần chú ý - tham vấn bác sĩ về dinh dưỡng và hormone tăng trưởng");
                if (heightTrend <= 0.05)
                    recommendations.Add("- 📈 Xu hướng tăng chiều cao chậm - cần đánh giá chuyên sâu");
            }
            else if (assessment.HeightStatus.Contains("Nguy cơ"))
            {
                recommendations.Add("- 📏 Theo dõi sát chiều cao - đảm bảo dinh dưỡng đầy đủ");
            }

            // Dựa trên tình trạng cân nặng
            if (assessment.WeightStatus.Contains("Suy dinh dưỡng") || assessment.BMIStatus.Contains("Suy dinh dưỡng"))
            {
                recommendations.Add("- ⚖️ Tình trạng cân nặng cần can thiệp - tham vấn bác sĩ dinh dưỡng ngay");
                if (weightTrend < -0.1)
                    recommendations.Add("- 📉 Xu hướng giảm cân - cần đánh giá nguyên nhân");
            }
            else if (assessment.WeightStatus.Contains("Béo phì") || assessment.BMIStatus.Contains("Béo phì"))
            {
                recommendations.Add("- ⚖️ Tình trạng thừa cân/béo phì - tham vấn bác sĩ về chế độ ăn và vận động");
            }
            else if (assessment.WeightStatus.Contains("Nguy cơ") || assessment.BMIStatus.Contains("Nguy cơ"))
            {
                recommendations.Add("- ⚖️ Cân nặng/BMI ở mức cần theo dõi - tham khảo bác sĩ");
            }

            // Dựa trên tình trạng vòng đầu
            if (assessment.HeadCircumferenceStatus.Contains("Microcephaly") || 
                assessment.HeadCircumferenceStatus.Contains("Macrocephaly") ||
                assessment.HeadCircumferenceStatus.Contains("rất"))
            {
                recommendations.Add("- 🧠 Vòng đầu bất thường - cần khám bác sĩ thần kinh nhi ngay");
            }

            // Khuyến nghị chung cho trường hợp bình thường
            if (assessment.HeightStatus.Contains("Bình thường") && 
                assessment.WeightStatus.Contains("Bình thường") && 
                assessment.BMIStatus.Contains("Bình thường"))
            {
                recommendations.Add("- ✅ Tình trạng phát triển trong giới hạn bình thường");
                recommendations.Add("- 📊 Tiếp tục duy trì chế độ sinh hoạt hiện tại và theo dõi định kỳ");
            }

            return recommendations;
        }

        private bool RequiresMedicalAttention(GrowthAssessmentsDTO assessment)
        {
            // Cần tham vấn y tế nếu có bất kỳ tình trạng nào ở mức nghiêm trọng
            return assessment.HeightStatus.Contains("mức độ nặng") ||
                   assessment.WeightStatus.Contains("mức độ nặng") ||
                   assessment.BMIStatus.Contains("mức độ nặng") ||
                   assessment.HeadCircumferenceStatus.Contains("rất nhỏ") ||
                   assessment.HeadCircumferenceStatus.Contains("rất to") ||
                   assessment.HeightStatus.Contains("suy dinh dưỡng thể thấp còi") ||
                   assessment.WeightStatus.Contains("suy dinh dưỡng thể gầy còm") ||
                   assessment.BMIStatus.Contains("suy dinh dưỡng thể gầy còm") ||
                   assessment.WeightStatus.Contains("Trẻ béo phì") ||
                   assessment.BMIStatus.Contains("Trẻ béo phì");
        }

        private List<string> GetMedicalConsultationReasons(GrowthAssessmentsDTO assessment, PredictionQualityDTO quality, double heightTrend, double weightTrend)
        {
            var reasons = new List<string>();

            if (quality.OverallConfidence < 60)
                reasons.Add("Độ tin cậy dự đoán thấp");

            if (heightTrend < 0)
                reasons.Add("Xu hướng giảm chiều cao bất thường");

            if (weightTrend < -0.3)
                reasons.Add("Xu hướng giảm cân đáng lo ngại");

            if (assessment.HeightStatus.Contains("mức độ nặng") || assessment.HeightStatus.Contains("mức độ vừa"))
                reasons.Add($"Tình trạng chiều cao: {assessment.HeightStatus}");

            if (assessment.WeightStatus.Contains("mức độ nặng") || assessment.WeightStatus.Contains("mức độ vừa") || 
                assessment.WeightStatus.Contains("Trẻ béo phì") || assessment.WeightStatus.Contains("Trẻ thừa cân"))
                reasons.Add($"Tình trạng cân nặng: {assessment.WeightStatus}");

            if (assessment.BMIStatus.Contains("mức độ nặng") || assessment.BMIStatus.Contains("mức độ vừa") || 
                assessment.BMIStatus.Contains("Trẻ béo phì") || assessment.BMIStatus.Contains("Trẻ thừa cân"))
                reasons.Add($"Tình trạng BMI: {assessment.BMIStatus}");

            if (assessment.HeadCircumferenceStatus.Contains("rất nhỏ") || assessment.HeadCircumferenceStatus.Contains("rất to"))
                reasons.Add($"Tình trạng vòng đầu: {assessment.HeadCircumferenceStatus}");

            return reasons;
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
                
                // ✅ IMPROVED LOGIC: Tìm độ tuổi gần nhất thay vì tìm chính xác
                var standards = await standardRepo.FindAsync(s =>
                    s.Gender == gender &&
                    s.AgeInMonths == ageInMonths
                );

                // Biến để track xem có sử dụng độ tuổi gần nhất không
                bool isUsingClosestAge = false;
                int? standardAgeInMonths = null;
                
                // Nếu không tìm thấy độ tuổi chính xác, tìm độ tuổi gần nhất
                if (!standards.Any())
                {
                    _logger.LogWarning("Không tìm thấy dữ liệu chuẩn cho độ tuổi {AgeInMonths} tháng. Tìm độ tuổi gần nhất...", ageInMonths);
                    
                    // Lấy tất cả standards cho giới tính này
                    var allStandardsForGender = await standardRepo.FindAsync(s => s.Gender == gender);
                    
                    if (!allStandardsForGender.Any())
                    {
                        throw new InvalidOperationException($"Không tìm thấy dữ liệu chuẩn cho giới tính {gender}");
                    }
                    
                    // Tìm độ tuổi gần nhất
                    var closestAge = allStandardsForGender
                        .Select(s => s.AgeInMonths)
                        .OrderBy(age => Math.Abs(age - ageInMonths))
                        .First();
                    
                    standardAgeInMonths = closestAge;
                    isUsingClosestAge = true;
                    
                    _logger.LogInformation("Sử dụng dữ liệu chuẩn cho độ tuổi gần nhất: {ClosestAge} tháng (thay vì {RequestedAge} tháng)", closestAge, ageInMonths);
                    
                    // Lấy standards cho độ tuổi gần nhất
                    standards = allStandardsForGender.Where(s => s.AgeInMonths == closestAge).ToList();
                }
                else
                {
                    // Nếu tìm thấy độ tuổi chính xác
                    standardAgeInMonths = ageInMonths;
                    isUsingClosestAge = false;
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
                    // ✅ Thông tin về độ tuổi chuẩn được sử dụng
                    StandardAgeInMonths = standardAgeInMonths,
                    RequestedAgeInMonths = ageInMonths,
                    IsUsingClosestAge = isUsingClosestAge,
                    Assessments = new GrowthAssessmentsDTO
                    {
                        HeightStatus = AssessHeightStatus(record.Height, heightStandard),
                        WeightStatus = AssessWeightAndBMIStatus(record.Weight, weightStandard),
                        BMIStatus = AssessWeightAndBMIStatus(record.Bmi, bmiStandard),
                        HeadCircumferenceStatus = AssessHeadCircumferenceStatus(record.HeadCircumference, headStandard)
                    }
                };

                // ✅ Sử dụng AI để tạo recommendations cho basic assessment
                assessment.Recommendations = await GenerateAIBasicRecommendations(assessment.Assessments, isUsingClosestAge, ageInMonths, standardAgeInMonths, child, record);

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

            if (height <= standard.Sd3neg) return "Trẻ suy dinh dưỡng thể thấp còi, mức độ nặng";
            if (height <= standard.Sd2neg) return "Trẻ suy dinh dưỡng thể thấp còi, mức độ vừa";
            if (height <= standard.Sd1neg) return "Nguy cơ thấp còi";
            if (height <= standard.Median) return "Bình thường";
            if (height <= standard.Sd1pos) return "Bình thường";
            if (height <= standard.Sd2pos) return "Chiều cao trung bình khá";
            if (height <= standard.Sd3pos) return "Cao";
            return "Rất cao";
        }

        private string AssessWeightAndBMIStatus(decimal value, GrowthStandard standard)
        {
            if (standard == null) return "Không có dữ liệu chuẩn";

            if (value <= standard.Sd3neg) return "Trẻ suy dinh dưỡng thể gầy còm, mức độ nặng";
            if (value <= standard.Sd2neg) return "Trẻ suy dinh dưỡng thể gầy còm, mức độ vừa";
            if (value <= standard.Sd1neg) return "Nguy cơ suy dinh dưỡng";
            if (value <= standard.Median) return "Bình thường thấp";
            if (value <= standard.Sd1pos) return "Bình thường";
            if (value <= standard.Sd2pos) return "Nguy cơ thừa cân/béo phì";
            if (value <= standard.Sd3pos) return "Trẻ thừa cân";
            return "Trẻ béo phì";
        }

        private string AssessHeadCircumferenceStatus(decimal headCircumference, GrowthStandard standard)
        {
            if (standard == null) return "Không có dữ liệu chuẩn";

            if (headCircumference <= standard.Sd3neg) return "Đầu rất nhỏ (Microcephaly)";
            if (headCircumference <= standard.Sd2neg) return "Đầu hơi nhỏ";
            if (headCircumference <= standard.Sd1neg) return "Bình thường";
            if (headCircumference <= standard.Median) return "Bình thường";
            if (headCircumference <= standard.Sd1pos) return "Bình thường";
            if (headCircumference <= standard.Sd2pos) return "Bình thường";
            if (headCircumference <= standard.Sd3pos) return "Đầu hơi to";
            return "Đầu rất to (Macrocephaly)";
        }

        private string GenerateBasicRecommendations(GrowthAssessmentsDTO assessments, bool isUsingClosestAge, int requestedAge, int? standardAge)
        {
            var recommendations = new List<string>();

            // ✅ Thông báo về độ tuổi chuẩn được sử dụng
            if (isUsingClosestAge && standardAge.HasValue)
            {
                recommendations.Add("⚠️ **LƯU Ý QUAN TRỌNG**");
                recommendations.Add($"- Độ tuổi yêu cầu: {requestedAge} tháng");
                recommendations.Add($"- Độ tuổi chuẩn được sử dụng: {standardAge.Value} tháng");
                recommendations.Add("- Đánh giá dựa trên dữ liệu chuẩn của độ tuổi gần nhất");
                recommendations.Add("");
            }
            
            // Hiển thị tình trạng hiện tại
            recommendations.Add("📊 **TÌNH TRẠNG HIỆN TẠI**");
            recommendations.Add($"- Chiều cao: {assessments.HeightStatus}");
            recommendations.Add($"- Cân nặng: {assessments.WeightStatus}");
            recommendations.Add($"- BMI: {assessments.BMIStatus}");
            recommendations.Add($"- Vòng đầu: {assessments.HeadCircumferenceStatus}");
            recommendations.Add("");

            // Khuyến nghị cơ bản (không chi tiết như VIP)
            recommendations.Add("💡 **KHUYẾN NGHỊ CỦA BÁC SĨ**");
            
            // Thêm khuyến nghị đặc biệt nếu sử dụng độ tuổi gần nhất
            if (isUsingClosestAge && standardAge.HasValue)
            {
                recommendations.Add("- 🔍 **KHUYẾN NGHỊ ĐẶC BIỆT**: Do không có dữ liệu chuẩn cho độ tuổi chính xác, đánh giá này dựa trên độ tuổi gần nhất. Vui lòng tham vấn bác sĩ để có đánh giá chính xác hơn.");
                recommendations.Add("");
            }

            // Đánh giá chiều cao
            if (assessments.HeightStatus.Contains("mức độ nặng") || assessments.HeightStatus.Contains("mức độ vừa"))
            {
                recommendations.Add("- 🏥 Cần tham vấn bác sĩ nhi khoa ngay");
                recommendations.Add("- 🥛 Chú ý chế độ dinh dưỡng đặc biệt");
                recommendations.Add("- 📊 Theo dõi sát sao chiều cao");
            }
            else if (assessments.HeightStatus.Contains("Nguy cơ thấp còi"))
            {
                recommendations.Add("- 📊 Theo dõi sát chiều cao");
                recommendations.Add("- 🏃‍♂️ Tăng cường vận động");
                recommendations.Add("- 🥛 Cải thiện chế độ dinh dưỡng");
            }

            // Đánh giá cân nặng và BMI
            if (assessments.WeightStatus.Contains("mức độ nặng") || assessments.BMIStatus.Contains("mức độ nặng") ||
                assessments.WeightStatus.Contains("mức độ vừa") || assessments.BMIStatus.Contains("mức độ vừa"))
            {
                recommendations.Add("- 🏥 Cần tham vấn bác sĩ dinh dưỡng ngay");
                recommendations.Add("- 🍎 Điều chỉnh chế độ ăn uống đặc biệt");
                recommendations.Add("- 📊 Theo dõi sát sao cân nặng và BMI");
            }
            else if (assessments.WeightStatus.Contains("Nguy cơ suy dinh dưỡng") || assessments.BMIStatus.Contains("Nguy cơ suy dinh dưỡng"))
            {
                recommendations.Add("- 📊 Theo dõi cân nặng thường xuyên");
                recommendations.Add("- 🍎 Cải thiện chế độ dinh dưỡng");
            }
            else if (assessments.WeightStatus.Contains("Nguy cơ thừa cân/béo phì") || assessments.BMIStatus.Contains("Nguy cơ thừa cân/béo phì"))
            {
                recommendations.Add("- 📊 Theo dõi cân nặng thường xuyên");
                recommendations.Add("- 🏃‍♂️ Tăng cường vận động");
                recommendations.Add("- 🍎 Điều chỉnh chế độ ăn uống");
            }
            else if (assessments.WeightStatus.Contains("Trẻ thừa cân") || assessments.BMIStatus.Contains("Trẻ thừa cân") ||
                     assessments.WeightStatus.Contains("Trẻ béo phì") || assessments.BMIStatus.Contains("Trẻ béo phì"))
            {
                recommendations.Add("- 🏥 Cần tham vấn bác sĩ dinh dưỡng");
                recommendations.Add("- 🍎 Điều chỉnh chế độ ăn uống");
                recommendations.Add("- 🏃‍♂️ Tăng cường vận động");
            }

            // Đánh giá vòng đầu
            if (assessments.HeadCircumferenceStatus.Contains("rất nhỏ") ||
                assessments.HeadCircumferenceStatus.Contains("Microcephaly"))
            {
                recommendations.Add("- 🧠 Cần khám chuyên khoa thần kinh nhi ngay");
                recommendations.Add("- 🔬 Kiểm tra sự phát triển não bộ");
                recommendations.Add("- 📊 Theo dõi sát sao vòng đầu");
            }
            else if (assessments.HeadCircumferenceStatus.Contains("rất to") ||
                     assessments.HeadCircumferenceStatus.Contains("Macrocephaly"))
            {
                recommendations.Add("- 🧠 Cần khám chuyên khoa thần kinh nhi ngay");
                recommendations.Add("- 🔬 Kiểm tra áp lực nội sọ và não úng thủy");
                recommendations.Add("- 📊 Theo dõi sát sao vòng đầu");
            }
            else if (assessments.HeadCircumferenceStatus.Contains("hơi nhỏ") ||
                     assessments.HeadCircumferenceStatus.Contains("hơi to"))
            {
                recommendations.Add("- 📊 Theo dõi vòng đầu thường xuyên");
                recommendations.Add("- 🔍 Quan sát các dấu hiệu bất thường");
            }

            // Khuyến nghị chung cho trường hợp bình thường
            if ((assessments.HeightStatus.Contains("Bình thường") || assessments.HeightStatus.Contains("Chiều cao trung bình khá")) && 
                (assessments.WeightStatus.Contains("Bình thường") || assessments.WeightStatus.Contains("Bình thường thấp")) && 
                (assessments.BMIStatus.Contains("Bình thường") || assessments.BMIStatus.Contains("Bình thường thấp")))
            {
                recommendations.Add("- ✅ Trẻ đang phát triển bình thường");
                recommendations.Add("- 📊 Tiếp tục theo dõi định kỳ");
                recommendations.Add("- 🏃‍♂️ Duy trì chế độ dinh dưỡng và vận động hiện tại");
            }

            return string.Join("\n", recommendations);
        }

        // ✅ AI METHODS - Thay thế recommendations cố định bằng AI
        private async Task<string> GenerateAIRecommendations(
            List<GrowthRecord> recentRecords, 
            List<PredictionDataPointDTO> predictions, 
            PredictionQualityDTO quality, 
            Child child)
        {
            try
            {
                _logger.LogInformation("🤖 Bắt đầu tạo AI recommendations cho trẻ {ChildId} - {ChildName}", child.ChildId, child.FullName);
                
                // Tạo context cho AI
                var context = new GrowthAssessmentContext
                {
                    Child = new ChildInfo
                    {
                        ChildId = child.ChildId,
                        FullName = child.FullName,
                        BirthDate = child.BirthDate,
                        Gender = child.Gender,
                        AgeInMonths = (int)((decimal)(DateTime.Now - child.BirthDate).TotalDays / 30.44M)
                    },
                    RecentRecords = recentRecords,
                    CurrentAssessment = await AssessCurrentStatus(recentRecords.Last(), child),
                    Predictions = predictions,
                    Quality = quality,
                    HeightTrend = AnalyzeTrend(recentRecords.Select(r => (double)r.Height).ToList()),
                    WeightTrend = AnalyzeTrend(recentRecords.Select(r => (double)r.Weight).ToList())
                };

                _logger.LogInformation("📊 Context AI đã tạo: Child={ChildName}, Records={RecordCount}, Predictions={PredictionCount}", 
                    context.Child.FullName, context.RecentRecords.Count, context.Predictions.Count);

                // Gọi AI để tạo khuyến nghị
                var aiResult = await _aiService.GenerateGrowthRecommendationsAsync(context);
                
                _logger.LogInformation("✅ AI đã tạo recommendations thành công cho trẻ {ChildId}. Độ dài: {Length} ký tự", 
                    child.ChildId, aiResult?.Length ?? 0);
                
                return aiResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ LỖI AI: Không thể tạo AI recommendations cho trẻ {ChildId} - {ChildName}. Lỗi: {ErrorMessage}", 
                    child.ChildId, child.FullName, ex.Message);
                _logger.LogWarning("🔄 Fallback về recommendations cố định cho trẻ {ChildId}", child.ChildId);
                
                // Fallback về recommendations cũ nếu AI lỗi
                return await GenerateEnhancedRecommendations(recentRecords, predictions, quality, child);
            }
        }

        private async Task<string> GenerateAIBasicRecommendations(
            GrowthAssessmentsDTO assessments, 
            bool isUsingClosestAge, 
            int requestedAge, 
            int? standardAge, 
            Child child, 
            GrowthRecord record)
        {
            try
            {
                _logger.LogInformation("🤖 Bắt đầu tạo AI basic recommendations cho trẻ {ChildId} - {ChildName}", child.ChildId, child.FullName);
                
                // Tạo context cho AI
                var context = new BasicAssessmentContext
                {
                    Child = new ChildInfo
                    {
                        ChildId = child.ChildId,
                        FullName = child.FullName,
                        BirthDate = child.BirthDate,
                        Gender = child.Gender,
                        AgeInMonths = requestedAge
                    },
                    CurrentRecord = record,
                    Assessment = assessments,
                    IsUsingClosestAge = isUsingClosestAge,
                    StandardAgeInMonths = standardAge,
                    RequestedAgeInMonths = requestedAge
                };

                _logger.LogInformation("📊 Basic Assessment Context: Child={ChildName}, Age={RequestedAge}, UsingClosestAge={IsUsingClosestAge}", 
                    context.Child.FullName, context.RequestedAgeInMonths, context.IsUsingClosestAge);

                // Gọi AI để tạo khuyến nghị
                var aiResult = await _aiService.GenerateBasicAssessmentRecommendationsAsync(context);
                
                _logger.LogInformation("✅ AI đã tạo basic recommendations thành công cho trẻ {ChildId}. Độ dài: {Length} ký tự", 
                    child.ChildId, aiResult?.Length ?? 0);
                
                return aiResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ LỖI AI: Không thể tạo AI basic recommendations cho trẻ {ChildId} - {ChildName}. Lỗi: {ErrorMessage}", 
                    child.ChildId, child.FullName, ex.Message);
                _logger.LogWarning("🔄 Fallback về basic recommendations cố định cho trẻ {ChildId}", child.ChildId);
                
                // Fallback về recommendations cũ nếu AI lỗi
                return GenerateBasicRecommendations(assessments, isUsingClosestAge, requestedAge, standardAge);
            }
        }
    }
}
