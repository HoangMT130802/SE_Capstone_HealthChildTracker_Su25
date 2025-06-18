using AutoMapper;
using BusinessLogic.DTOs.GrowthStandard;
using BusinessLogic.Services.Interfaces;
using DataAccess.Entities;
using DataAccess.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementations
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

        public async Task<IEnumerable<GrowthStandardDTO>> GetHeightStandardsAsync(string gender, int? ageInMonths = null)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<GrowthStandard>();
                var standards = await repository.FindAsync(
                    x => x.Measurement == "Height" &&
                         x.Gender == gender &&
                         (!ageInMonths.HasValue || x.AgeInMonths == ageInMonths.Value)
                );

                // Sắp xếp sau khi lấy dữ liệu
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

        public async Task<IEnumerable<GrowthStandardDTO>> GetWeightStandardsAsync(string gender, int? ageInMonths = null)
        {
            try
            {
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

        public async Task<IEnumerable<GrowthStandardDTO>> GetBMIStandardsAsync(string gender, int? ageInMonths = null)
        {
            try
            {
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

        public async Task<IEnumerable<GrowthStandardDTO>> GetHeadCircumferenceStandardsAsync(string gender, int? ageInMonths = null)
        {
            try
            {
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
    }
}