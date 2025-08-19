using AutoMapper;
using Contracts.DTOs.FacilityVaccine;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class FacilityVaccineService : IFacilityVaccineService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<FacilityVaccineService> _logger;

        public FacilityVaccineService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<FacilityVaccineService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private async Task ValidateManagerAccess(int accountId, int facilityId)
        {
            var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
            var staff = await staffRepository.GetAsync(s => s.AccountId == accountId && s.FacilityId == facilityId && s.Position == "Manager,Staff,Doctor");
            if (staff == null)
            {
                throw new UnauthorizedAccessException($"Người dùng với AccountId {accountId} không phải FacilityStaff hoặc không thuộc FacilityId {facilityId}");
            }
        }

        public async Task<FacilityVaccineDTO> CreateFacilityVaccineAsync(CreateFacilityVaccineDTO facilityVaccineDto, int accountId)
        {
            try
            {
                _logger.LogInformation($"Creating facility vaccine for FacilityId: {facilityVaccineDto.FacilityId}, VaccineId: {facilityVaccineDto.VaccineId}, by AccountId: {accountId}");

                // Validate Manager access
                await ValidateManagerAccess(accountId, facilityVaccineDto.FacilityId);

                // Validate FacilityId
                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facilityExists = await facilityRepository.AnyAsync(f => f.FacilityId == facilityVaccineDto.FacilityId);
                if (!facilityExists)
                {
                    throw new InvalidOperationException($"Facility với ID {facilityVaccineDto.FacilityId} không tồn tại");
                }

                // Validate VaccineId
                var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                var vaccineExists = await vaccineRepository.AnyAsync(v => v.VaccineId == facilityVaccineDto.VaccineId);
                if (!vaccineExists)
                {
                    throw new InvalidOperationException($"Vaccine với ID {facilityVaccineDto.VaccineId} không tồn tại");
                }

                // Validate no duplicate FacilityVaccine
                var facilityVaccineRepository = _unitOfWork.GetRepository<FacilityVaccine>();
                var existingFacilityVaccine = await facilityVaccineRepository.AnyAsync(fv => fv.FacilityId == facilityVaccineDto.FacilityId && fv.VaccineId == facilityVaccineDto.VaccineId);
                if (existingFacilityVaccine)
                {
                    throw new InvalidOperationException($"FacilityVaccine với FacilityId {facilityVaccineDto.FacilityId} và VaccineId {facilityVaccineDto.VaccineId} đã tồn tại");
                }

                // Map DTO to entity
                var facilityVaccine = _mapper.Map<FacilityVaccine>(facilityVaccineDto);
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    _logger.LogError($"Invalid DateTime value: {currentTime}");
                    throw new InvalidOperationException($"Invalid DateTime value for CreatedAt/UpdatedAt: {currentTime}");
                }

                facilityVaccine.CreatedAt = currentTime;
                facilityVaccine.UpdatedAt = currentTime;
                _logger.LogInformation($"FacilityVaccine CreatedAt: {facilityVaccine.CreatedAt:yyyy-MM-dd HH:mm:ss}, UpdatedAt: {facilityVaccine.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

                // Save FacilityVaccine
                await facilityVaccineRepository.AddAsync(facilityVaccine);
                await _unitOfWork.SaveChangesAsync();

                var savedFacilityVaccine = await facilityVaccineRepository.GetAsync(fv => fv.FacilityVaccineId == facilityVaccine.FacilityVaccineId);
                return _mapper.Map<FacilityVaccineDTO>(savedFacilityVaccine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating facility vaccine for FacilityId {facilityVaccineDto.FacilityId} and VaccineId {facilityVaccineDto.VaccineId}");
                throw;
            }
        }

        public async Task<FacilityVaccineDTO> GetFacilityVaccineByIdAsync(int facilityVaccineId)
        {
            try
            {
                var facilityVaccineRepository = _unitOfWork.GetRepository<FacilityVaccine>();
                var facilityVaccine = await facilityVaccineRepository.GetAsync(
                    fv => fv.FacilityVaccineId == facilityVaccineId,
                    includeProperties: "Vaccine,Vaccine.VaccineDiseases,Vaccine.VaccineDiseases.Disease"
                );
                if (facilityVaccine == null)
                {
                    throw new KeyNotFoundException($"FacilityVaccine với ID {facilityVaccineId} không tồn tại");
                }
                return _mapper.Map<FacilityVaccineDTO>(facilityVaccine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy FacilityVaccine với ID {facilityVaccineId}");
                throw;
            }
        }

        public async Task<QueryResultModel<IEnumerable<FacilityVaccineDTO>>> GetAllFacilityVaccinesAsync(
            Expression<Func<FacilityVaccine, bool>>? filter = null,
            Func<IQueryable<FacilityVaccine>, IOrderedQueryable<FacilityVaccine>>? orderBy = null,
            string include = "Vaccine",
            int? pageIndex = null,
            int? pageSize = null)
        {
            try
            {
                var facilityVaccineRepository = _unitOfWork.GetRepository<FacilityVaccine>();
                string includeProperties = string.IsNullOrEmpty(include) ? "Vaccine.VaccineDiseases.Disease" : $"{include},Vaccine.VaccineDiseases.Disease".Trim(',');

                var result = await facilityVaccineRepository.GetAllAsync(
                    filter: filter,
                    orderBy: orderBy,
                    include: includeProperties,
                    pageIndex: pageIndex,
                    pageSize: pageSize);

                var facilityVaccineDtos = _mapper.Map<IEnumerable<FacilityVaccineDTO>>(result.Data);
                return new QueryResultModel<IEnumerable<FacilityVaccineDTO>>
                {
                    TotalCount = result.TotalCount,
                    Data = facilityVaccineDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách FacilityVaccines với phân trang");
                throw;
            }
        }

        public async Task<FacilityVaccineDTO> UpdateFacilityVaccineAsync(int facilityVaccineId, UpdateFacilityVaccineDTO facilityVaccineDto, int accountId)
        {
            try
            {
                _logger.LogInformation($"Updating facility vaccine with ID: {facilityVaccineId}, by AccountId: {accountId}");

                var facilityVaccineRepository = _unitOfWork.GetRepository<FacilityVaccine>();
                var facilityVaccine = await facilityVaccineRepository.GetAsync(fv => fv.FacilityVaccineId == facilityVaccineId);
                if (facilityVaccine == null)
                {
                    throw new KeyNotFoundException($"FacilityVaccine với ID {facilityVaccineId} không tồn tại");
                }

                // Validate Manager access
                await ValidateManagerAccess(accountId, facilityVaccineDto.FacilityId);

                // Validate FacilityId
                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facilityExists = await facilityRepository.AnyAsync(f => f.FacilityId == facilityVaccineDto.FacilityId);
                if (!facilityExists)
                {
                    throw new InvalidOperationException($"Facility với ID {facilityVaccineDto.FacilityId} không tồn tại");
                }

                // Validate VaccineId
                var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                var vaccineExists = await vaccineRepository.AnyAsync(v => v.VaccineId == facilityVaccineDto.VaccineId);
                if (!vaccineExists)
                {
                    throw new InvalidOperationException($"Vaccine với ID {facilityVaccineDto.VaccineId} không tồn tại");
                }

                // Validate no duplicate FacilityVaccine (excluding current record)
                var existingFacilityVaccine = await facilityVaccineRepository.AnyAsync(fv => fv.FacilityId == facilityVaccineDto.FacilityId && fv.VaccineId == facilityVaccineDto.VaccineId && fv.FacilityVaccineId != facilityVaccineId);
                if (existingFacilityVaccine)
                {
                    throw new InvalidOperationException($"FacilityVaccine với FacilityId {facilityVaccineDto.FacilityId} và VaccineId {facilityVaccineDto.VaccineId} đã tồn tại");
                }

                // Update facility vaccine properties
                _mapper.Map(facilityVaccineDto, facilityVaccine);
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    _logger.LogError($"Invalid DateTime value: {currentTime}");
                    throw new InvalidOperationException($"Invalid DateTime value for UpdatedAt: {currentTime}");
                }
                facilityVaccine.UpdatedAt = currentTime;
                _logger.LogInformation($"FacilityVaccine UpdatedAt: {facilityVaccine.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

                // Update facility vaccine
                facilityVaccineRepository.Update(facilityVaccine);
                await _unitOfWork.SaveChangesAsync();

                var updatedFacilityVaccine = await facilityVaccineRepository.GetAsync(fv => fv.FacilityVaccineId == facilityVaccineId);
                return _mapper.Map<FacilityVaccineDTO>(updatedFacilityVaccine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating facility vaccine with ID {facilityVaccineId}");
                throw;
            }
        }

        public async Task<bool> DeleteFacilityVaccineAsync(int facilityVaccineId, int accountId)
        {
            try
            {
                var facilityVaccineRepository = _unitOfWork.GetRepository<FacilityVaccine>();
                var facilityVaccine = await facilityVaccineRepository.GetAsync(fv => fv.FacilityVaccineId == facilityVaccineId);
                if (facilityVaccine == null)
                {
                    throw new KeyNotFoundException($"FacilityVaccine với ID {facilityVaccineId} không tồn tại");
                }

                // Validate Manager access
                await ValidateManagerAccess(accountId, facilityVaccine.FacilityId);

                // Delete facility vaccine
                facilityVaccineRepository.Delete(facilityVaccine);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting facility vaccine with ID {facilityVaccineId}");
                throw;
            }
        }
        public async Task<int> GetCountByFacilityAsync(int facilityId)
        {
            var repository = _unitOfWork.GetRepository<FacilityVaccine>();
            return await repository.CountAsync(fv => fv.FacilityId == facilityId);
        }
    }
}
