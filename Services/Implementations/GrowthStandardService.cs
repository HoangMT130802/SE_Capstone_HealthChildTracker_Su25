using AutoMapper;
using Services.Interfaces;
using Contracts.DTOs.GrowthStandard;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class GrowthStandardService : IGrowthStandardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GrowthStandardService> _logger;

        public GrowthStandardService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GrowthStandardService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<GrowthStandardDTO>> GetHeightStandardsAsync(string gender, int? ageInDays = null)
        {
            try
            {
                // Chuyển đổi ageInDays sang ageInMonths (1 tháng = 30 ngày)
                int? ageInMonths = ageInDays.HasValue ? (int)Math.Round(ageInDays.Value / 30.0) : null;
                
                var repository = _unitOfWork.GetRepository<GrowthStandard>();
                var standards = await repository.FindAsync(
                    x => x.Measurement == "Height" &&
                         x.Gender == gender &&
                         (!ageInMonths.HasValue || x.AgeInMonths == ageInMonths.Value)
                );

            
                return _mapper.Map<IEnumerable<GrowthStandardDTO>>(
                    standards.OrderBy(x => x.AgeInMonths)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu chuẩn chiều cao cho giới tính {Gender}", gender);
                throw;
            }
        }

        public async Task<IEnumerable<GrowthStandardDTO>> GetWeightStandardsAsync(string gender, int? ageInDays = null)
        {
            try
            {
                // Chuyển đổi ageInDays sang ageInMonths (1 tháng = 30 ngày)
                int? ageInMonths = ageInDays.HasValue ? (int)Math.Round(ageInDays.Value / 30.0) : null;
                
                var repository = _unitOfWork.GetRepository<GrowthStandard>();
                var standards = await repository.FindAsync(
                    x => x.Measurement == "Weight" &&
                         x.Gender == gender &&
                         (!ageInMonths.HasValue || x.AgeInMonths == ageInMonths.Value)
                );

                return _mapper.Map<IEnumerable<GrowthStandardDTO>>(
                    standards.OrderBy(x => x.AgeInMonths)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu chuẩn cân nặng cho giới tính {Gender}", gender);
                throw;
            }
        }

        public async Task<IEnumerable<GrowthStandardDTO>> GetBMIStandardsAsync(string gender, int? ageInDays = null)
        {
            try
            {
                // Chuyển đổi ageInDays sang ageInMonths (1 tháng = 30 ngày)
                int? ageInMonths = ageInDays.HasValue ? (int)Math.Round(ageInDays.Value / 30.0) : null;
                
                var repository = _unitOfWork.GetRepository<GrowthStandard>();
                var standards = await repository.FindAsync(
                    x => x.Measurement == "BMI" &&
                         x.Gender == gender &&
                         (!ageInMonths.HasValue || x.AgeInMonths == ageInMonths.Value)
                );

                return _mapper.Map<IEnumerable<GrowthStandardDTO>>(
                    standards.OrderBy(x => x.AgeInMonths)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu chuẩn BMI cho giới tính {Gender}", gender);
                throw;
            }
        }

        public async Task<IEnumerable<GrowthStandardDTO>> GetHeadCircumferenceStandardsAsync(string gender, int? ageInDays = null)
        {
            try
            {
                // Chuyển đổi ageInDays sang ageInMonths (1 tháng = 30 ngày)
                int? ageInMonths = ageInDays.HasValue ? (int)Math.Round(ageInDays.Value / 30.0) : null;
                
                var repository = _unitOfWork.GetRepository<GrowthStandard>();
                var standards = await repository.FindAsync(
                    x => x.Measurement == "HeadCircumference" &&
                         x.Gender == gender &&
                         (!ageInMonths.HasValue || x.AgeInMonths == ageInMonths.Value)
                );

                return _mapper.Map<IEnumerable<GrowthStandardDTO>>(
                    standards.OrderBy(x => x.AgeInMonths)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu chuẩn vòng đầu cho giới tính {Gender}", gender);
                throw;
            }
        }

        // ✅ Growth Velocity Methods
        public async Task<IEnumerable<GrowthVelocityDTO>> GetHeightVelocityStandardsAsync(string gender, int? ageInMonths = null)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<GrowthVelocity>();
                var standards = await repository.FindAsync(
                    x => x.Measurement == "Height" &&
                         x.Gender == gender &&
                         (!ageInMonths.HasValue || x.AgeInMonths == ageInMonths.Value)
                );

                return _mapper.Map<IEnumerable<GrowthVelocityDTO>>(
                    standards.OrderBy(x => x.AgeInMonths)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu tốc độ tăng trưởng chiều cao cho giới tính {Gender}", gender);
                throw;
            }
        }

        public async Task<IEnumerable<GrowthVelocityDTO>> GetWeightVelocityStandardsAsync(string gender, int? ageInMonths = null)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<GrowthVelocity>();
                var standards = await repository.FindAsync(
                    x => x.Measurement == "Weight" &&
                         x.Gender == gender &&
                         (!ageInMonths.HasValue || x.AgeInMonths == ageInMonths.Value)
                );

                return _mapper.Map<IEnumerable<GrowthVelocityDTO>>(
                    standards.OrderBy(x => x.AgeInMonths)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu tốc độ tăng trưởng cân nặng cho giới tính {Gender}", gender);
                throw;
            }
        }

        public async Task<IEnumerable<GrowthVelocityDTO>> GetHeadCircumferenceVelocityStandardsAsync(string gender, int? ageInMonths = null)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<GrowthVelocity>();
                var standards = await repository.FindAsync(
                    x => x.Measurement == "HeadCircumference" &&
                         x.Gender == gender &&
                         (!ageInMonths.HasValue || x.AgeInMonths == ageInMonths.Value)
                );

                return _mapper.Map<IEnumerable<GrowthVelocityDTO>>(
                    standards.OrderBy(x => x.AgeInMonths)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu tốc độ tăng trưởng vòng đầu cho giới tính {Gender}", gender);
                throw;
            }
        }

        public async Task<GrowthVelocityAssessmentDTO> AssessGrowthVelocityAsync(string gender, int ageInMonths, decimal actualVelocity, string measurement)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<GrowthVelocity>();
                var standard = await repository.GetAsync(
                    x => x.Measurement == measurement &&
                         x.Gender == gender &&
                         x.AgeInMonths == ageInMonths
                );

                if (standard == null)
                {
                    // Tìm độ tuổi gần nhất nếu không có dữ liệu chính xác
                    var nearestStandards = await repository.FindAsync(
                        x => x.Measurement == measurement &&
                             x.Gender == gender
                    );

                    if (!nearestStandards.Any())
                    {
                        throw new InvalidOperationException($"Không tìm thấy dữ liệu tốc độ tăng trưởng cho {measurement} - {gender} - {ageInMonths} tháng");
                    }

                    standard = nearestStandards
                        .OrderBy(x => Math.Abs(x.AgeInMonths - ageInMonths))
                        .First();
                }

                var assessment = new GrowthVelocityAssessmentDTO
                {
                    Gender = gender,
                    AgeInMonths = ageInMonths,
                    Measurement = measurement,
                    ActualVelocity = actualVelocity,
                    Unit = standard.Unit,
                    Sd3neg = standard.Sd3neg,
                    Sd2neg = standard.Sd2neg,
                    Sd1neg = standard.Sd1neg,
                    Median = standard.Median,
                    Sd1pos = standard.Sd1pos,
                    Sd2pos = standard.Sd2pos,
                    Sd3pos = standard.Sd3pos,
                    ExpectedVelocity = standard.Median
                };

                // ✅ Đánh giá tốc độ tăng trưởng
                assessment.VelocityStatus = AssessVelocityStatus(actualVelocity, standard);
                assessment.VelocityDescription = GetVelocityDescription(actualVelocity, standard);
                assessment.VelocityPercentile = CalculateVelocityPercentile(actualVelocity, standard);
                assessment.Recommendation = GenerateVelocityRecommendation(assessment.VelocityStatus, measurement);
                assessment.RequiresMedicalAttention = DetermineIfMedicalAttentionRequired(assessment.VelocityStatus);
                assessment.MedicalAdvice = GenerateMedicalAdvice(assessment.VelocityStatus, measurement);

                return assessment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đánh giá tốc độ tăng trưởng cho {Measurement} - {Gender} - {AgeInMonths} tháng", measurement, gender, ageInMonths);
                throw;
            }
        }

        private string AssessVelocityStatus(decimal actualVelocity, GrowthVelocity standard)
        {
            if (actualVelocity <= standard.Sd3neg) return "Tăng trưởng rất chậm";
            if (actualVelocity <= standard.Sd2neg) return "Tăng trưởng chậm";
            if (actualVelocity <= standard.Sd1neg) return "Tăng trưởng chậm hơn bình thường";
            if (actualVelocity <= standard.Median) return "Tăng trưởng bình thường thấp";
            if (actualVelocity <= standard.Sd1pos) return "Tăng trưởng bình thường";
            if (actualVelocity <= standard.Sd2pos) return "Tăng trưởng nhanh hơn bình thường";
            if (actualVelocity <= standard.Sd3pos) return "Tăng trưởng nhanh";
            return "Tăng trưởng rất nhanh";
        }

        private string GetVelocityDescription(decimal actualVelocity, GrowthVelocity standard)
        {
            var difference = actualVelocity - standard.Median;
            var percentDifference = (difference / standard.Median) * 100;

            if (Math.Abs(percentDifference) <= 10) return "Tốc độ tăng trưởng trong giới hạn bình thường";
            if (percentDifference > 10) return $"Tăng trưởng nhanh hơn {Math.Abs(percentDifference):F1}% so với chuẩn";
            return $"Tăng trưởng chậm hơn {Math.Abs(percentDifference):F1}% so với chuẩn";
        }

        private decimal CalculateVelocityPercentile(decimal actualVelocity, GrowthVelocity standard)
        {
            // Tính percentile dựa trên vị trí so với các SD
            if (actualVelocity <= standard.Sd3neg) return 0.1m;
            if (actualVelocity <= standard.Sd2neg) return 2.3m;
            if (actualVelocity <= standard.Sd1neg) return 15.9m;
            if (actualVelocity <= standard.Median) return 50.0m;
            if (actualVelocity <= standard.Sd1pos) return 84.1m;
            if (actualVelocity <= standard.Sd2pos) return 97.7m;
            if (actualVelocity <= standard.Sd3pos) return 99.9m;
            return 99.9m;
        }

        private string GenerateVelocityRecommendation(string velocityStatus, string measurement)
        {
            var measurementName = measurement switch
            {
                "Height" => "chiều cao",
                "Weight" => "cân nặng",
                "HeadCircumference" => "vòng đầu",
                _ => measurement
            };

            return velocityStatus switch
            {
                "Tăng trưởng rất chậm" => $"Cần tham vấn bác sĩ ngay về tình trạng tăng trưởng {measurementName}",
                "Tăng trưởng chậm" => $"Theo dõi sát và tham vấn bác sĩ về tình trạng tăng trưởng {measurementName}",
                "Tăng trưởng chậm hơn bình thường" => $"Cần cải thiện dinh dưỡng và vận động cho {measurementName}",
                "Tăng trưởng bình thường thấp" => $"Duy trì chế độ dinh dưỡng và vận động hiện tại",
                "Tăng trưởng bình thường" => $"Tiếp tục duy trì chế độ sinh hoạt hiện tại",
                "Tăng trưởng nhanh hơn bình thường" => $"Theo dõi để đảm bảo tăng trưởng không quá nhanh",
                "Tăng trưởng nhanh" => $"Cần kiểm tra để đảm bảo tăng trưởng không bất thường",
                "Tăng trưởng rất nhanh" => $"Cần tham vấn bác sĩ để kiểm tra tình trạng tăng trưởng",
                _ => "Theo dõi định kỳ và tham vấn bác sĩ nếu cần"
            };
        }

        private bool DetermineIfMedicalAttentionRequired(string velocityStatus)
        {
            return velocityStatus.Contains("rất chậm") || 
                   velocityStatus.Contains("rất nhanh") ||
                   velocityStatus.Contains("chậm") ||
                   velocityStatus.Contains("nhanh");
        }

        private string GenerateMedicalAdvice(string velocityStatus, string measurement)
        {
            var measurementName = measurement switch
            {
                "Height" => "chiều cao",
                "Weight" => "cân nặng",
                "HeadCircumference" => "vòng đầu",
                _ => measurement
            };

            if (velocityStatus.Contains("rất chậm") || velocityStatus.Contains("rất nhanh"))
            {
                return $"Cần khám bác sĩ nhi khoa ngay để đánh giá tình trạng tăng trưởng {measurementName}";
            }

            if (velocityStatus.Contains("chậm") || velocityStatus.Contains("nhanh"))
            {
                return $"Cần tham vấn bác sĩ nhi khoa trong thời gian sớm nhất";
            }

            return "Tiếp tục theo dõi định kỳ, không cần can thiệp y tế ngay lập tức";
        }
    }
}