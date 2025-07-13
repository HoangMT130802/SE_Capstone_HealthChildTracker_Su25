using AutoMapper;
using Contracts.DTOs.Vaccine;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class VaccineService : IVaccineService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<VaccineService> _logger;

        public VaccineService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<VaccineService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaccineDTO> CreateVaccineAsync(CreateVaccineDTO vaccineDto)
        {
            try
            {
                _logger.LogInformation($"Creating vaccine with name: {vaccineDto.Name}");
                _logger.LogInformation($"DiseaseIds: {string.Join(", ", vaccineDto.DiseaseIds ?? new List<int>())}");

                // Validate no duplicate vaccine name
                var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                var existingVaccine = await vaccineRepository.AnyAsync(v => v.Name == vaccineDto.Name);
                if (existingVaccine)
                {
                    throw new InvalidOperationException($"A vaccine with name '{vaccineDto.Name}' already exists");
                }

                // Validate DiseaseIds
                if (vaccineDto.DiseaseIds != null && vaccineDto.DiseaseIds.Any())
                {
                    var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                    var invalidDiseaseIds = new List<int>();
                    foreach (var id in vaccineDto.DiseaseIds)
                    {
                        var exists = await diseaseRepository.AnyAsync(d => d.DiseaseId == id);
                        if (!exists)
                        {
                            invalidDiseaseIds.Add(id);
                        }
                    }
                    if (invalidDiseaseIds.Any())
                    {
                        throw new InvalidOperationException($"Invalid Disease IDs: {string.Join(", ", invalidDiseaseIds)}");
                    }
                }

                // Map DTO to entity
                var vaccine = _mapper.Map<Vaccine>(vaccineDto);
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    _logger.LogError($"Invalid DateTime value: {currentTime}");
                    throw new InvalidOperationException($"Invalid DateTime value for CreatedAt/UpdatedAt: {currentTime}");
                }

                // Explicitly set DateTime fields for Vaccine
                vaccine.CreatedAt = currentTime;
                vaccine.UpdatedAt = currentTime;
                _logger.LogInformation($"Vaccine CreatedAt: {vaccine.CreatedAt:yyyy-MM-dd HH:mm:ss}, UpdatedAt: {vaccine.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"Vaccine object before saving: {JsonConvert.SerializeObject(vaccine)}");

                // Save Vaccine
                await vaccineRepository.AddAsync(vaccine);
                await _unitOfWork.SaveChangesAsync();

                // Add VaccineDiseases
                if (vaccineDto.DiseaseIds != null && vaccineDto.DiseaseIds.Any())
                {
                    var vaccineDiseaseRepository = _unitOfWork.GetRepository<VaccineDisease>();
                    foreach (var diseaseId in vaccineDto.DiseaseIds)
                    {
                        var vaccineDisease = new VaccineDisease
                        {
                            VaccineId = vaccine.VaccineId,
                            DiseaseId = diseaseId, // Sửa lỗi: Gán DiseaseId
                            CreatedAt = currentTime,
                            UpdatedAt = currentTime
                        };
                        _logger.LogInformation($"Adding VaccineDisease with DiseaseId: {diseaseId}, CreatedAt: {vaccineDisease.CreatedAt:yyyy-MM-dd HH:mm:ss}, UpdatedAt: {vaccineDisease.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                        _logger.LogInformation($"VaccineDisease object: {JsonConvert.SerializeObject(vaccineDisease)}");
                        await vaccineDiseaseRepository.AddAsync(vaccineDisease);
                    }
                    await _unitOfWork.SaveChangesAsync();
                }

                var savedVaccine = await vaccineRepository.GetAsync(v => v.VaccineId == vaccine.VaccineId, includeProperties: "VaccineDiseases");
                return _mapper.Map<VaccineDTO>(savedVaccine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating vaccine with name {vaccineDto.Name}");
                throw;
            }
        }

        public async Task<VaccineDTO> GetVaccineByIdAsync(int vaccineId)
        {
            try
            {
                var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                var vaccine = await vaccineRepository.GetAsync(v => v.VaccineId == vaccineId, includeProperties: "VaccineDiseases.Disease");
                if (vaccine == null)
                {
                    throw new KeyNotFoundException($"Vaccine with ID {vaccineId} not found");
                }

                return _mapper.Map<VaccineDTO>(vaccine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting vaccine with ID {vaccineId}");
                throw;
            }
        }

        public async Task<IEnumerable<VaccineDTO>> GetAllVaccinesAsync()
        {
            try
            {
                var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                var vaccines = await vaccineRepository.GetAllAsync(includeProperties: "VaccineDiseases.Disease");
                return _mapper.Map<IEnumerable<VaccineDTO>>(vaccines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all vaccines");
                throw;
            }
        }

        public async Task<VaccineDTO> UpdateVaccineAsync(int vaccineId, UpdateVaccineDTO vaccineDto)
        {
            try
            {
                _logger.LogInformation($"Updating vaccine with ID: {vaccineId}");
                _logger.LogInformation($"DiseaseIds: {string.Join(", ", vaccineDto.DiseaseIds ?? new List<int>())}");

                var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                var vaccine = await vaccineRepository.GetAsync(v => v.VaccineId == vaccineId, includeProperties: "VaccineDiseases");
                if (vaccine == null)
                {
                    throw new KeyNotFoundException($"Vaccine with ID {vaccineId} not found");
                }

                // Validate no duplicate vaccine name
                var existingVaccine = await vaccineRepository.AnyAsync(v => v.Name == vaccineDto.Name && v.VaccineId != vaccineId);
                if (existingVaccine)
                {
                    throw new InvalidOperationException($"A vaccine with name '{vaccineDto.Name}' already exists");
                }

                // Validate DiseaseIds
                if (vaccineDto.DiseaseIds != null && vaccineDto.DiseaseIds.Any())
                {
                    var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                    var invalidDiseaseIds = new List<int>();
                    foreach (var id in vaccineDto.DiseaseIds)
                    {
                        var exists = await diseaseRepository.AnyAsync(d => d.DiseaseId == id);
                        if (!exists)
                        {
                            invalidDiseaseIds.Add(id);
                        }
                    }
                    if (invalidDiseaseIds.Any())
                    {
                        throw new InvalidOperationException($"Invalid Disease IDs: {string.Join(", ", invalidDiseaseIds)}");
                    }
                }

                // Update vaccine properties
                _mapper.Map(vaccineDto, vaccine);
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    _logger.LogError($"Invalid DateTime value: {currentTime}");
                    throw new InvalidOperationException($"Invalid DateTime value for UpdatedAt: {currentTime}");
                }
                vaccine.UpdatedAt = currentTime;
                _logger.LogInformation($"Vaccine UpdatedAt: {vaccine.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"Vaccine object before saving: {JsonConvert.SerializeObject(vaccine)}");

                // Update VaccineDiseases
                var vaccineDiseaseRepository = _unitOfWork.GetRepository<VaccineDisease>();
                if (vaccine.VaccineDiseases != null && vaccine.VaccineDiseases.Any())
                {
                    // Xóa tất cả VaccineDiseases hiện có (cascade delete sẽ xử lý trong DB)
                    foreach (var existing in vaccine.VaccineDiseases.ToList())
                    {
                        _logger.LogInformation($"Removing VaccineDisease with DiseaseId: {existing.DiseaseId}");
                        vaccineDiseaseRepository.Delete(existing);
                    }
                    vaccine.VaccineDiseases.Clear(); // Xóa navigation property để tránh xung đột
                }

                // Thêm VaccineDiseases mới
                if (vaccineDto.DiseaseIds != null && vaccineDto.DiseaseIds.Any())
                {
                    foreach (var diseaseId in vaccineDto.DiseaseIds)
                    {
                        var vaccineDisease = new VaccineDisease
                        {
                            VaccineId = vaccineId,
                            DiseaseId = diseaseId,
                            CreatedAt = currentTime,
                            UpdatedAt = currentTime
                        };
                        _logger.LogInformation($"Adding VaccineDisease with DiseaseId: {diseaseId}, CreatedAt: {vaccineDisease.CreatedAt:yyyy-MM-dd HH:mm:ss}, UpdatedAt: {vaccineDisease.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                        _logger.LogInformation($"VaccineDisease object: {JsonConvert.SerializeObject(vaccineDisease)}");
                        await vaccineDiseaseRepository.AddAsync(vaccineDisease);
                    }
                }

                // Update vaccine
                vaccineRepository.Update(vaccine);
                await _unitOfWork.SaveChangesAsync();

                var updatedVaccine = await vaccineRepository.GetAsync(v => v.VaccineId == vaccineId, includeProperties: "VaccineDiseases");
                return _mapper.Map<VaccineDTO>(updatedVaccine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vaccine with ID {vaccineId}");
                throw;
            }
        }

        public async Task<bool> DeleteVaccineAsync(int vaccineId)
        {
            try
            {
                var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                var vaccine = await vaccineRepository.GetAsync(v => v.VaccineId == vaccineId, includeProperties: "VaccineDiseases");
                if (vaccine == null)
                {
                    throw new KeyNotFoundException($"Vaccine with ID {vaccineId} not found");
                }

                // Xóa vaccine (cascade delete sẽ xử lý VaccineDiseases)
                vaccineRepository.Delete(vaccine);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting vaccine with ID {vaccineId}");
                throw;
            }
        }
    }
}
