using AutoMapper;
using Contracts.DTOs.VaccinePackage;
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
    public class VaccinePackageService : IVaccinePackageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<VaccinePackageService> _logger;

        public VaccinePackageService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<VaccinePackageService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaccinePackageDTO> CreateVaccinePackageAsync(CreateVaccinePackageDTO vaccinePackageDto)
        {
            try
            {
                _logger.LogInformation($"Creating vaccine package with name: {vaccinePackageDto.Name}, FacilityId: {vaccinePackageDto.FacilityId}");

                // Validate FacilityId
                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facilityExists = await facilityRepository.AnyAsync(f => f.FacilityId == vaccinePackageDto.FacilityId);
                if (!facilityExists)
                {
                    throw new InvalidOperationException($"Facility with ID {vaccinePackageDto.FacilityId} does not exist");
                }

                // Validate no duplicate package name within the same facility
                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var existingPackage = await packageRepository.AnyAsync(p => p.Name == vaccinePackageDto.Name && p.FacilityId == vaccinePackageDto.FacilityId);
                if (existingPackage)
                {
                    throw new InvalidOperationException($"A vaccine package with name '{vaccinePackageDto.Name}' already exists in facility {vaccinePackageDto.FacilityId}");
                }

                // Map DTO to entity
                var vaccinePackage = _mapper.Map<VaccinePackage>(vaccinePackageDto);
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    _logger.LogError($"Invalid DateTime value: {currentTime}");
                    throw new InvalidOperationException($"Invalid DateTime value for CreatedAt/UpdatedAt: {currentTime}");
                }

                vaccinePackage.CreatedAt = currentTime;
                vaccinePackage.UpdatedAt = currentTime;
                _logger.LogInformation($"VaccinePackage CreatedAt: {vaccinePackage.CreatedAt:yyyy-MM-dd HH:mm:ss}, UpdatedAt: {vaccinePackage.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"VaccinePackage object before saving: {JsonConvert.SerializeObject(vaccinePackage)}");

                // Save VaccinePackage
                await packageRepository.AddAsync(vaccinePackage);
                await _unitOfWork.SaveChangesAsync();

                var savedPackage = await packageRepository.GetAsync(p => p.PackageId == vaccinePackage.PackageId, includeProperties: "PackageVaccines");
                return _mapper.Map<VaccinePackageDTO>(savedPackage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating vaccine package with name {vaccinePackageDto.Name}");
                throw;
            }
        }

        public async Task<VaccinePackageDTO> CreateVaccinePackageWithVaccinesAsync(CreateVaccinePackageWithVaccinesDTO vaccinePackageDto)
        {
            try
            {
                _logger.LogInformation($"Creating vaccine package with name: {vaccinePackageDto.Name}, FacilityId: {vaccinePackageDto.FacilityId}, with {vaccinePackageDto.Vaccines.Count} vaccines");

                // Validate FacilityId
                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facilityExists = await facilityRepository.AnyAsync(f => f.FacilityId == vaccinePackageDto.FacilityId);
                if (!facilityExists)
                {
                    throw new InvalidOperationException($"Facility with ID {vaccinePackageDto.FacilityId} does not exist");
                }

                // Validate no duplicate package name within the same facility
                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var existingPackage = await packageRepository.AnyAsync(p => p.Name == vaccinePackageDto.Name && p.FacilityId == vaccinePackageDto.FacilityId);
                if (existingPackage)
                {
                    throw new InvalidOperationException($"A vaccine package with name '{vaccinePackageDto.Name}' already exists in facility {vaccinePackageDto.FacilityId}");
                }

                // Validate vaccines
                var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                var vaccineIds = vaccinePackageDto.Vaccines.Select(v => v.VaccineId).ToList();
                var uniqueVaccineIds = vaccineIds.Distinct().ToList();
                if (uniqueVaccineIds.Count != vaccineIds.Count)
                {
                    throw new InvalidOperationException("Duplicate VaccineIds are not allowed in the package");
                }

                foreach (var vaccineId in uniqueVaccineIds)
                {
                    var vaccineExists = await vaccineRepository.AnyAsync(v => v.VaccineId == vaccineId);
                    if (!vaccineExists)
                    {
                        throw new InvalidOperationException($"Vaccine with ID {vaccineId} does not exist");
                    }
                }

                // Begin transaction
                using (var transaction = await _unitOfWork.BeginTransactionAsync())
                {
                    try
                    {
                        // Map and save VaccinePackage
                        var vaccinePackage = _mapper.Map<VaccinePackage>(vaccinePackageDto);
                        var currentTime = DateTime.UtcNow;
                        if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                        {
                            _logger.LogError($"Invalid DateTime value: {currentTime}");
                            throw new InvalidOperationException($"Invalid DateTime value for CreatedAt/UpdatedAt: {currentTime}");
                        }

                        vaccinePackage.CreatedAt = currentTime;
                        vaccinePackage.UpdatedAt = currentTime;
                        await packageRepository.AddAsync(vaccinePackage);
                        await _unitOfWork.SaveChangesAsync();

                        // Map and save PackageVaccines
                        var packageVaccineRepository = _unitOfWork.GetRepository<PackageVaccine>();
                        foreach (var vaccineDto in vaccinePackageDto.Vaccines)
                        {
                            var packageVaccine = new PackageVaccine
                            {
                                PackageId = vaccinePackage.PackageId,
                                VaccineId = vaccineDto.VaccineId,
                                Quantity = vaccineDto.Quantity,
                                CreatedAt = currentTime,
                                UpdatedAt = currentTime
                            };
                            _logger.LogInformation($"Adding package vaccine: PackageId {packageVaccine.PackageId}, VaccineId {packageVaccine.VaccineId}, Quantity {packageVaccine.Quantity}");
                            await packageVaccineRepository.AddAsync(packageVaccine);
                        }

                        await _unitOfWork.SaveChangesAsync();
                        await transaction.CommitAsync();

                        var savedPackage = await packageRepository.GetAsync(p => p.PackageId == vaccinePackage.PackageId, includeProperties: "PackageVaccines");
                        return _mapper.Map<VaccinePackageDTO>(savedPackage);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, $"Error creating vaccine package with name {vaccinePackageDto.Name} and vaccines");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating vaccine package with name {vaccinePackageDto.Name}");
                throw;
            }
        }

        public async Task<PackageVaccineDTO> AddVaccineToPackageAsync(int packageId, CreatePackageVaccineDTO packageVaccineDto)
        {
            try
            {
                _logger.LogInformation($"Adding vaccine to package with PackageId: {packageId}, VaccineId: {packageVaccineDto.VaccineId}");

                // Validate PackageId
                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var package = await packageRepository.GetAsync(p => p.PackageId == packageId);
                if (package == null)
                {
                    throw new InvalidOperationException($"Package with ID {packageId} does not exist");
                }

                // Validate VaccineId
                var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                var vaccineExists = await vaccineRepository.AnyAsync(v => v.VaccineId == packageVaccineDto.VaccineId);
                if (!vaccineExists)
                {
                    throw new InvalidOperationException($"Vaccine with ID {packageVaccineDto.VaccineId} does not exist");
                }

                // Validate no duplicate PackageVaccine
                var packageVaccineRepository = _unitOfWork.GetRepository<PackageVaccine>();
                var existingPackageVaccine = await packageVaccineRepository.AnyAsync(pv => pv.PackageId == packageId && pv.VaccineId == packageVaccineDto.VaccineId);
                if (existingPackageVaccine)
                {
                    throw new InvalidOperationException($"A package vaccine with PackageId {packageId} and VaccineId {packageVaccineDto.VaccineId} already exists");
                }

                // Map DTO to entity
                var packageVaccine = new PackageVaccine
                {
                    PackageId = packageId,
                    VaccineId = packageVaccineDto.VaccineId,
                    Quantity = packageVaccineDto.Quantity
                };
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    _logger.LogError($"Invalid DateTime value: {currentTime}");
                    throw new InvalidOperationException($"Invalid DateTime value for CreatedAt/UpdatedAt: {currentTime}");
                }

                packageVaccine.CreatedAt = currentTime;
                packageVaccine.UpdatedAt = currentTime;
                _logger.LogInformation($"PackageVaccine CreatedAt: {packageVaccine.CreatedAt:yyyy-MM-dd HH:mm:ss}, UpdatedAt: {packageVaccine.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"PackageVaccine object before saving: {JsonConvert.SerializeObject(packageVaccine)}");

                // Save PackageVaccine
                await packageVaccineRepository.AddAsync(packageVaccine);
                await _unitOfWork.SaveChangesAsync();

                var savedPackageVaccine = await packageVaccineRepository.GetAsync(pv => pv.PackageVaccineId == packageVaccine.PackageVaccineId);
                return _mapper.Map<PackageVaccineDTO>(savedPackageVaccine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding vaccine to package with PackageId {packageId} and VaccineId {packageVaccineDto.VaccineId}");
                throw;
            }
        }

        public async Task<VaccinePackageDTO> GetVaccinePackageByIdAsync(int packageId)
        {
            try
            {
                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var vaccinePackage = await packageRepository.GetAsync(p => p.PackageId == packageId, includeProperties: "PackageVaccines");
                if (vaccinePackage == null)
                {
                    throw new KeyNotFoundException($"VaccinePackage with ID {packageId} not found");
                }

                return _mapper.Map<VaccinePackageDTO>(vaccinePackage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting vaccine package with ID {packageId}");
                throw;
            }
        }

        public async Task<IEnumerable<VaccinePackageDTO>> GetAllVaccinePackagesAsync()
        {
            try
            {
                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var vaccinePackages = await packageRepository.GetAllAsync(includeProperties: "PackageVaccines");
                return _mapper.Map<IEnumerable<VaccinePackageDTO>>(vaccinePackages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all vaccine packages");
                throw;
            }
        }

        public async Task<VaccinePackageDTO> UpdateVaccinePackageAsync(int packageId, UpdateVaccinePackageDTO vaccinePackageDto)
        {
            try
            {
                _logger.LogInformation($"Updating vaccine package with ID: {packageId}");

                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var vaccinePackage = await packageRepository.GetAsync(p => p.PackageId == packageId);
                if (vaccinePackage == null)
                {
                    throw new KeyNotFoundException($"VaccinePackage with ID {packageId} not found");
                }

                // Validate FacilityId
                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facilityExists = await facilityRepository.AnyAsync(f => f.FacilityId == vaccinePackageDto.FacilityId);
                if (!facilityExists)
                {
                    throw new InvalidOperationException($"Facility with ID {vaccinePackageDto.FacilityId} does not exist");
                }

                // Validate no duplicate package name within the same facility
                var existingPackage = await packageRepository.AnyAsync(p => p.Name == vaccinePackageDto.Name && p.FacilityId == vaccinePackageDto.FacilityId && p.PackageId != packageId);
                if (existingPackage)
                {
                    throw new InvalidOperationException($"A vaccine package with name '{vaccinePackageDto.Name}' already exists in facility {vaccinePackageDto.FacilityId}");
                }

                // Update vaccine package properties
                _mapper.Map(vaccinePackageDto, vaccinePackage);
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    _logger.LogError($"Invalid DateTime value: {currentTime}");
                    throw new InvalidOperationException($"Invalid DateTime value for UpdatedAt: {currentTime}");
                }
                vaccinePackage.UpdatedAt = currentTime;
                _logger.LogInformation($"VaccinePackage UpdatedAt: {vaccinePackage.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"VaccinePackage object before saving: {JsonConvert.SerializeObject(vaccinePackage)}");

                // Update vaccine package
                packageRepository.Update(vaccinePackage);
                await _unitOfWork.SaveChangesAsync();

                var updatedPackage = await packageRepository.GetAsync(p => p.PackageId == packageId, includeProperties: "PackageVaccines");
                return _mapper.Map<VaccinePackageDTO>(updatedPackage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vaccine package with ID {packageId}");
                throw;
            }
        }

        public async Task<PackageVaccineDTO> UpdateVaccineInPackageAsync(int packageId, int vaccineId, UpdatePackageVaccineDTO packageVaccineDto)
        {
            try
            {
                _logger.LogInformation($"Updating vaccine in package with PackageId: {packageId}, VaccineId: {vaccineId}");

                var packageVaccineRepository = _unitOfWork.GetRepository<PackageVaccine>();
                var packageVaccine = await packageVaccineRepository.GetAsync(pv => pv.PackageId == packageId && pv.VaccineId == vaccineId);
                if (packageVaccine == null)
                {
                    throw new KeyNotFoundException($"PackageVaccine with PackageId {packageId} and VaccineId {vaccineId} not found");
                }

                // Validate PackageId
                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var package = await packageRepository.GetAsync(p => p.PackageId == packageId);
                if (package == null)
                {
                    throw new InvalidOperationException($"Package with ID {packageId} does not exist");
                }

                // Validate VaccineId
                var vaccineRepository = _unitOfWork.GetRepository<Vaccine>();
                var vaccineExists = await vaccineRepository.AnyAsync(v => v.VaccineId == packageVaccineDto.VaccineId);
                if (!vaccineExists)
                {
                    throw new InvalidOperationException($"Vaccine with ID {packageVaccineDto.VaccineId} does not exist");
                }

                // Validate no duplicate PackageVaccine (excluding current record)
                var existingPackageVaccine = await packageVaccineRepository.AnyAsync(pv => pv.PackageId == packageId && pv.VaccineId == packageVaccineDto.VaccineId && pv.VaccineId != vaccineId);
                if (existingPackageVaccine)
                {
                    throw new InvalidOperationException($"A package vaccine with PackageId {packageId} and VaccineId {packageVaccineDto.VaccineId} already exists");
                }

                // Update package vaccine properties
                packageVaccine.VaccineId = packageVaccineDto.VaccineId;
                packageVaccine.Quantity = packageVaccineDto.Quantity;
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    _logger.LogError($"Invalid DateTime value: {currentTime}");
                    throw new InvalidOperationException($"Invalid DateTime value for UpdatedAt: {currentTime}");
                }
                packageVaccine.UpdatedAt = currentTime;
                _logger.LogInformation($"PackageVaccine UpdatedAt: {packageVaccine.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"PackageVaccine object before saving: {JsonConvert.SerializeObject(packageVaccine)}");

                // Update package vaccine
                packageVaccineRepository.Update(packageVaccine);
                await _unitOfWork.SaveChangesAsync();

                var updatedPackageVaccine = await packageVaccineRepository.GetAsync(pv => pv.PackageId == packageId && pv.VaccineId == packageVaccineDto.VaccineId);
                return _mapper.Map<PackageVaccineDTO>(updatedPackageVaccine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vaccine in package with PackageId {packageId} and VaccineId {vaccineId}");
                throw;
            }
        }

        public async Task<bool> DeleteVaccinePackageAsync(int packageId)
        {
            try
            {
                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var vaccinePackage = await packageRepository.GetAsync(p => p.PackageId == packageId);
                if (vaccinePackage == null)
                {
                    throw new KeyNotFoundException($"VaccinePackage with ID {packageId} not found");
                }

                // Delete vaccine package (cascade delete will handle PackageVaccines and Orders)
                packageRepository.Delete(vaccinePackage);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting vaccine package with ID {packageId}");
                throw;
            }
        }

        public async Task<bool> DeleteVaccineFromPackageAsync(int packageId, int vaccineId)
        {
            try
            {
                var packageVaccineRepository = _unitOfWork.GetRepository<PackageVaccine>();
                var packageVaccine = await packageVaccineRepository.GetAsync(pv => pv.PackageId == packageId && pv.VaccineId == vaccineId);
                if (packageVaccine == null)
                {
                    throw new KeyNotFoundException($"PackageVaccine with PackageId {packageId} and VaccineId {vaccineId} not found");
                }

                // Delete package vaccine
                packageVaccineRepository.Delete(packageVaccine);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting vaccine from package with PackageId {packageId} and VaccineId {vaccineId}");
                throw;
            }
        }
    }
}
