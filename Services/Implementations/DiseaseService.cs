using AutoMapper;
using Contracts.DTOs.Disease;
using Microsoft.Extensions.Logging;
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
    public class DiseaseService : IDiseaseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<DiseaseService> _logger;

        public DiseaseService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<DiseaseService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DiseaseDTO> CreateDiseaseAsync(CreateDiseaseDTO diseaseDto)
        {
            try
            {
                // Validate no duplicate disease name
                var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                var existingDisease = await diseaseRepository.AnyAsync(d => d.Name == diseaseDto.Name);
                if (existingDisease)
                {
                    throw new InvalidOperationException($"A disease with name '{diseaseDto.Name}' already exists");
                }

                var disease = _mapper.Map<Disease>(diseaseDto);
                disease.CreatedAt = DateTime.UtcNow;
                disease.UpdatedAt = DateTime.UtcNow;

                await diseaseRepository.AddAsync(disease);
                await _unitOfWork.SaveChangesAsync();

                var savedDisease = await diseaseRepository.GetAsync(d => d.DiseaseId == disease.DiseaseId);
                return _mapper.Map<DiseaseDTO>(savedDisease);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating disease with name {diseaseDto.Name}");
                throw;
            }
        }

        public async Task<DiseaseDTO> GetDiseaseByIdAsync(int diseaseId)
        {
            try
            {
                var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                var disease = await diseaseRepository.GetAsync(d => d.DiseaseId == diseaseId);
                if (disease == null)
                {
                    throw new KeyNotFoundException($"Disease with ID {diseaseId} not found");
                }

                return _mapper.Map<DiseaseDTO>(disease);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting disease with ID {diseaseId}");
                throw;
            }
        }

        public async Task<IEnumerable<DiseaseDTO>> GetAllDiseasesAsync()
        {
            try
            {
                var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                var diseases = await diseaseRepository.GetAllAsync("");
                return _mapper.Map<IEnumerable<DiseaseDTO>>(diseases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all diseases");
                throw;
            }
        }

        public async Task<DiseaseDTO> UpdateDiseaseAsync(int diseaseId, UpdateDiseaseDTO diseaseDto)
        {
            try
            {
                var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                var disease = await diseaseRepository.GetAsync(d => d.DiseaseId == diseaseId);
                if (disease == null)
                {
                    throw new KeyNotFoundException($"Disease with ID {diseaseId} not found");
                }

                // Validate no duplicate disease name (except for the current disease)
                var existingDisease = await diseaseRepository.AnyAsync(d => d.Name == diseaseDto.Name && d.DiseaseId != diseaseId);
                if (existingDisease)
                {
                    throw new InvalidOperationException($"A disease with name '{diseaseDto.Name}' already exists");
                }

                _mapper.Map(diseaseDto, disease);
                disease.UpdatedAt = DateTime.UtcNow;

                diseaseRepository.Update(disease);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<DiseaseDTO>(disease);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating disease with ID {diseaseId}");
                throw;
            }
        }

        public async Task<bool> DeleteDiseaseAsync(int diseaseId)
        {
            try
            {
                var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                var disease = await diseaseRepository.GetAsync(d => d.DiseaseId == diseaseId);
                if (disease == null)
                {
                    throw new KeyNotFoundException($"Disease with ID {diseaseId} not found");
                }

                diseaseRepository.Delete(disease);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting disease with ID {diseaseId}");
                throw;
            }
        }
    }
}
