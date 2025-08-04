using AutoMapper;
using Contracts.DTOs.VaccineTemplate;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class VaccineTemplateService : IVaccineTemplateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<VaccineTemplateService> _logger;

        public VaccineTemplateService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<VaccineTemplateService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<VaccineTemplateDTO> CreateVaccineTemplateAsync(CreateVaccineTemplateDTO vaccineTemplateDto)
        {
            try
            {
                _logger.LogInformation($"Creating vaccine template with DiseaseId: {vaccineTemplateDto.DiseaseId}");

                // Validate DiseaseId
                var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                var diseaseExists = await diseaseRepository.AnyAsync(d => d.DiseaseId == vaccineTemplateDto.DiseaseId);
                if (!diseaseExists)
                {
                    throw new InvalidOperationException($"Disease with ID {vaccineTemplateDto.DiseaseId} does not exist");
                }

                // Map DTO to entity
                var vaccineTemplate = _mapper.Map<VaccineTemplate>(vaccineTemplateDto);
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    _logger.LogError($"Invalid DateTime value: {currentTime}");
                    throw new InvalidOperationException($"Invalid DateTime value for CreatedAt/UpdatedAt: {currentTime}");
                }
                vaccineTemplate.CreatedAt = currentTime;
                vaccineTemplate.UpdatedAt = currentTime;

                // Log vaccine template object
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var vaccineTemplateRepository = _unitOfWork.GetRepository<VaccineTemplate>();
                await vaccineTemplateRepository.AddAsync(vaccineTemplate);
                await _unitOfWork.SaveChangesAsync();
                var savedVaccineTemplate = await vaccineTemplateRepository.GetAsync(vt => vt.Id == vaccineTemplate.Id, includeProperties: "Disease");
                return _mapper.Map<VaccineTemplateDTO>(savedVaccineTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vaccine template");
                throw;
            }
        }

        public async Task<VaccineTemplateDTO> UpdateVaccineTemplateAsync(int vaccineTemplateId, UpdateVaccineTemplateDTO vaccineTemplateDto)
        {
            try
            {
                _logger.LogInformation($"Updating vaccine template with ID: {vaccineTemplateId}");

                var vaccineTemplateRepository = _unitOfWork.GetRepository<VaccineTemplate>();
                var vaccineTemplate = await vaccineTemplateRepository.GetAsync(vt => vt.Id == vaccineTemplateId, includeProperties: "Disease");
                if (vaccineTemplate == null)
                {
                    throw new KeyNotFoundException($"Vaccine template with ID {vaccineTemplateId} not found");
                }
                var diseaseRepository = _unitOfWork.GetRepository<Disease>();
                var diseaseExists = await diseaseRepository.AnyAsync(d => d.DiseaseId == vaccineTemplateDto.DiseaseId);
                if (!diseaseExists)
                {
                    throw new InvalidOperationException($"Disease with ID {vaccineTemplateDto.DiseaseId} does not exist");
                }
                _mapper.Map(vaccineTemplateDto, vaccineTemplate);
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    _logger.LogError($"Invalid DateTime value: {currentTime}");
                    throw new InvalidOperationException($"Invalid DateTime value for UpdatedAt: {currentTime}");
                }
                vaccineTemplate.UpdatedAt = currentTime;
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                vaccineTemplateRepository.Update(vaccineTemplate);
                await _unitOfWork.SaveChangesAsync();

                var updatedVaccineTemplate = await vaccineTemplateRepository.GetAsync(vt => vt.Id == vaccineTemplateId, includeProperties: "Disease");
                return _mapper.Map<VaccineTemplateDTO>(updatedVaccineTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vaccine template with ID {vaccineTemplateId}");
                throw;
            }
        }

        public async Task<VaccineTemplateDTO> GetVaccineTemplateByIdAsync(int vaccineTemplateId)
        {
            try
            {
                _logger.LogInformation($"Retrieving vaccine template with ID: {vaccineTemplateId}");

                var vaccineTemplateRepository = _unitOfWork.GetRepository<VaccineTemplate>();
                var vaccineTemplate = await vaccineTemplateRepository.GetAsync(vt => vt.Id == vaccineTemplateId, includeProperties: "Disease");
                if (vaccineTemplate == null)
                {
                    throw new KeyNotFoundException($"Vaccine template with ID {vaccineTemplateId} not found");
                }

                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                _logger.LogInformation($"Retrieved vaccine template: {JsonConvert.SerializeObject(vaccineTemplate, settings)}");

                return _mapper.Map<VaccineTemplateDTO>(vaccineTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving vaccine template with ID {vaccineTemplateId}");
                throw;
            }
        }

        public async Task<IEnumerable<VaccineTemplateDTO>> GetAllVaccineTemplatesAsync(string? diseaseName = null, int? diseaseId = null, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation($"Retrieving vaccine templates with filters: DiseaseName={diseaseName}, DiseaseId={diseaseId}, PageNumber={pageNumber}, PageSize={pageSize}");

                if (pageNumber < 1)
                {
                    throw new ArgumentException("Page number must be greater than or equal to 1");
                }
                if (pageSize < 1)
                {
                    throw new ArgumentException("Page size must be greater than or equal to 1");
                }

                var vaccineTemplateRepository = _unitOfWork.GetRepository<VaccineTemplate>();
                Expression<Func<VaccineTemplate, bool>> filter = null;

                if (!string.IsNullOrWhiteSpace(diseaseName))
                {
                    filter = vt => vt.Disease != null && vt.Disease.Name.Contains(diseaseName);
                }
                if (diseaseId.HasValue)
                {
                    Expression<Func<VaccineTemplate, bool>> diseaseIdFilter = vt => vt.DiseaseId == diseaseId.Value;
                    if (filter == null)
                    {
                        filter = diseaseIdFilter;
                    }
                    else
                    {
                        var parameter = Expression.Parameter(typeof(VaccineTemplate), "vt");
                        var combined = Expression.AndAlso(
                            Expression.Invoke(filter, parameter),
                            Expression.Invoke(diseaseIdFilter, parameter)
                        );
                        filter = Expression.Lambda<Func<VaccineTemplate, bool>>(combined, parameter);
                    }
                }

                var vaccineTemplatesResult = await vaccineTemplateRepository.GetAllAsync(
                    filter: filter,
                    include: "Disease"
                );

                // Apply pagination
                var pagedVaccineTemplates = vaccineTemplatesResult.Data
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                return _mapper.Map<IEnumerable<VaccineTemplateDTO>>(pagedVaccineTemplates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vaccine templates");
                throw;
            }
        }
    }
}
