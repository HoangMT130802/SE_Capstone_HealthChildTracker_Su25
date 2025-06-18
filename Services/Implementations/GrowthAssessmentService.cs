using AutoMapper;
using BusinessLogic.DTOs.GrowthAssessment;
using BusinessLogic.DTOs.GrowthRecord;
using BusinessLogic.Services.Interfaces;
using DataAccess.Entities;
using DataAccess.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementations
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

        public async Task<GrowthAssessmentDTO> AssessGrowthAsync(GrowthRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            try
            {
                // Lấy thông tin trẻ
                var childRepo = _unitOfWork.GetRepository<Child>();
                var child = await childRepo.GetAsync(c => c.ChildId == record.ChildId);

                if (child == null)
                    throw new KeyNotFoundException($"Không tìm thấy trẻ với ID {record.ChildId}");

                // Chuẩn hóa giới tính
                string gender = child.Gender?.Trim().ToUpper();
                if (string.IsNullOrEmpty(gender) || (gender != "MALE" && gender != "FEMALE"))
                {
                    throw new InvalidOperationException($"Giới tính không hợp lệ: {child.Gender}");
                }
                gender = char.ToUpper(gender[0]) + gender.Substring(1).ToLower();

                // Tính tuổi tại thời điểm đo (tính theo tháng)
                int ageInMonths = (int)((decimal)(record.CreatedAt - child.BirthDate).TotalDays / 30.44M);

                // Lấy dữ liệu chuẩn theo độ tuổi và giới tính
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
            if (headCircumference <= standard.Sd1neg) return "Bình thường thấp";
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
