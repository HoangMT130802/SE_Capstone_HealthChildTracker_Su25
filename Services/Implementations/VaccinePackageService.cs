using AutoMapper;
using Contracts.DTOs.VaccinePackage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Services.Interfaces;
using System;
using System.Linq;
using System.Linq.Expressions;
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

        private async Task ValidateManagerAccess(int accountId, int facilityId)
        {
            var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
            var staff = await staffRepository.GetAsync(s => s.AccountId == accountId && s.FacilityId == facilityId && s.Position == "Manager");
            if (staff == null)
            {
                throw new UnauthorizedAccessException($"Người dùng với AccountId {accountId} không phải FacilityStaff hoặc không thuộc FacilityId {facilityId}");
            }
        }

        private async Task<decimal> CalculatePackagePriceAsync(int packageId)
        {
            var packageVaccineRepository = _unitOfWork.GetRepository<PackageVaccine>();
            var packageVaccines = await packageVaccineRepository.GetAllAsync(pv => pv.PackageId == packageId, include: "FacilityVaccine");

            decimal totalPrice = 0;
            _logger.LogInformation($"Calculating price for PackageId {packageId}. Found {packageVaccines.Data.Count()} PackageVaccines.");
            foreach (var packageVaccine in packageVaccines.Data)
            {
                if (packageVaccine.FacilityVaccine == null)
                {
                    _logger.LogError($"FacilityVaccine with ID {packageVaccine.FacilityVaccineId} not found for PackageId {packageId}.");
                    throw new InvalidOperationException($"FacilityVaccine with ID {packageVaccine.FacilityVaccineId} not found for PackageId {packageId}.");
                }
                if (packageVaccine.FacilityVaccine.Price < 0)
                {
                    _logger.LogError($"FacilityVaccine with ID {packageVaccine.FacilityVaccineId} has invalid price: {packageVaccine.FacilityVaccine.Price}");
                    throw new InvalidOperationException($"FacilityVaccine with ID {packageVaccine.FacilityVaccineId} has invalid price: {packageVaccine.FacilityVaccine.Price}");
                }
                var vaccinePrice = packageVaccine.FacilityVaccine.Price * packageVaccine.Quantity;
                _logger.LogInformation($"FacilityVaccineId {packageVaccine.FacilityVaccineId}: Price = {packageVaccine.FacilityVaccine.Price}, Quantity = {packageVaccine.Quantity}, SubTotal = {vaccinePrice}");
                totalPrice += vaccinePrice;
            }
            _logger.LogInformation($"Total Price for PackageId {packageId}: {totalPrice}");
            return totalPrice;
        }

        private async Task ValidateVaccineInput(int facilityVaccineId, int facilityId)
        {
            if (facilityVaccineId <= 0)
            {
                throw new InvalidOperationException("FacilityVaccineId phải lớn hơn 0");
            }
            var facilityVaccineRepository = _unitOfWork.GetRepository<FacilityVaccine>();
            var facilityVaccineExists = await facilityVaccineRepository.AnyAsync(fv => fv.FacilityVaccineId == facilityVaccineId && fv.FacilityId == facilityId);
            if (!facilityVaccineExists)
            {
                throw new InvalidOperationException($"FacilityVaccine với ID {facilityVaccineId} không tồn tại hoặc không thuộc Facility {facilityId}");
            }
        }
        private async Task<int> GetDiseaseIdForFacilityVaccineAsync(int facilityVaccineId)
        {
            var facilityVaccineRepository = _unitOfWork.GetRepository<FacilityVaccine>();
            var vaccineDiseaseRepository = _unitOfWork.GetRepository<VaccineDisease>();

            var facilityVaccine = await facilityVaccineRepository.GetAsync(
                fv => fv.FacilityVaccineId == facilityVaccineId,
                includeProperties: "Vaccine,Vaccine.VaccineDiseases"
            );
            if (facilityVaccine == null)
            {
                throw new InvalidOperationException($"FacilityVaccine với ID {facilityVaccineId} không tồn tại");
            }

            var vaccineDisease = facilityVaccine.Vaccine?.VaccineDiseases?.FirstOrDefault();
            if (vaccineDisease == null)
            {
                _logger.LogWarning($"No Disease found for FacilityVaccineId {facilityVaccineId}. Using default DiseaseId = 0.");
                return 0; // Hoặc throw exception nếu DiseaseId bắt buộc
            }

            return vaccineDisease.DiseaseId;
        }

        public async Task<VaccinePackageDTO> CreateVaccinePackageAsync(CreateVaccinePackageDTO vaccinePackageDto, int accountId)
        {
            try
            {
                _logger.LogInformation($"Creating vaccine package with name: {vaccinePackageDto.Name}, FacilityId: {vaccinePackageDto.FacilityId}");
                await ValidateManagerAccess(accountId, vaccinePackageDto.FacilityId);

                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facilityExists = await facilityRepository.AnyAsync(f => f.FacilityId == vaccinePackageDto.FacilityId);
                if (!facilityExists)
                {
                    throw new InvalidOperationException($"Facility với ID {vaccinePackageDto.FacilityId} không tồn tại");
                }

                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var existingPackage = await packageRepository.AnyAsync(p => p.Name == vaccinePackageDto.Name && p.FacilityId == vaccinePackageDto.FacilityId);
                if (existingPackage)
                {
                    throw new InvalidOperationException($"Gói vaccine với tên '{vaccinePackageDto.Name}' đã tồn tại trong facility {vaccinePackageDto.FacilityId}");
                }

                var vaccinePackage = _mapper.Map<VaccinePackage>(vaccinePackageDto);
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    throw new InvalidOperationException($"Invalid DateTime value for CreatedAt/UpdatedAt: {currentTime}");
                }

                vaccinePackage.CreatedAt = currentTime;
                vaccinePackage.UpdatedAt = currentTime;
                vaccinePackage.Price = 0;
                _logger.LogInformation($"VaccinePackage CreatedAt: {vaccinePackage.CreatedAt:yyyy-MM-dd HH:mm:ss}, UpdatedAt: {vaccinePackage.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"VaccinePackage object before saving: {JsonConvert.SerializeObject(vaccinePackage)}");

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

        public async Task<VaccinePackageDTO> CreateVaccinePackageWithVaccinesAsync(CreateVaccinePackageWithVaccinesDTO vaccinePackageDto, int accountId)
        {
            try
            {
                _logger.LogInformation($"Creating vaccine package with name: {vaccinePackageDto.Name}, FacilityId: {vaccinePackageDto.FacilityId}, with {vaccinePackageDto.Vaccines.Count} vaccines, by AccountId: {accountId}");

                await ValidateManagerAccess(accountId, vaccinePackageDto.FacilityId);

                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facilityExists = await facilityRepository.AnyAsync(f => f.FacilityId == vaccinePackageDto.FacilityId);
                if (!facilityExists)
                {
                    throw new InvalidOperationException($"Facility với ID {vaccinePackageDto.FacilityId} không tồn tại");
                }

                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var existingPackage = await packageRepository.AnyAsync(p => p.Name == vaccinePackageDto.Name && p.FacilityId == vaccinePackageDto.FacilityId);
                if (existingPackage)
                {
                    throw new InvalidOperationException($"Gói vaccine với tên '{vaccinePackageDto.Name}' đã tồn tại trong facility {vaccinePackageDto.FacilityId}");
                }

                var facilityVaccineIds = vaccinePackageDto.Vaccines.Select(v => v.FacilityVaccineId).ToList();
                var uniqueFacilityVaccineIds = facilityVaccineIds.Distinct().ToList();
                if (uniqueFacilityVaccineIds.Count != facilityVaccineIds.Count)
                {
                    throw new InvalidOperationException("Không được phép có FacilityVaccineId trùng lặp trong gói");
                }

                foreach (var vaccineDto in vaccinePackageDto.Vaccines)
                {
                    await ValidateVaccineInput(vaccineDto.FacilityVaccineId, vaccinePackageDto.FacilityId);
                }

                using (var transaction = await _unitOfWork.BeginTransactionAsync())
                {
                    try
                    {
                        var vaccinePackage = _mapper.Map<VaccinePackage>(vaccinePackageDto);
                        var currentTime = DateTime.UtcNow;
                        if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                        {
                            throw new InvalidOperationException($"Invalid DateTime value for CreatedAt/UpdatedAt: {currentTime}");
                        }

                        vaccinePackage.CreatedAt = currentTime;
                        vaccinePackage.UpdatedAt = currentTime;
                        vaccinePackage.Price = 0;
                        await packageRepository.AddAsync(vaccinePackage);
                        await _unitOfWork.SaveChangesAsync();

                        var packageVaccineRepository = _unitOfWork.GetRepository<PackageVaccine>();
                        foreach (var vaccineDto in vaccinePackageDto.Vaccines)
                        {
                            var diseaseId = await GetDiseaseIdForFacilityVaccineAsync(vaccineDto.FacilityVaccineId);
                            var packageVaccine = new PackageVaccine
                            {
                                PackageId = vaccinePackage.PackageId,
                                FacilityVaccineId = vaccineDto.FacilityVaccineId,
                                Quantity = vaccineDto.Quantity,
                                CreatedAt = currentTime,
                                UpdatedAt = currentTime,
                                DiseaseId = diseaseId // Lấy DiseaseId từ GetDiseaseIdForFacilityVaccineAsync
                            };
                            _logger.LogInformation($"Adding package vaccine: PackageId {packageVaccine.PackageId}, FacilityVaccineId {packageVaccine.FacilityVaccineId}, Quantity {packageVaccine.Quantity}, DiseaseId {packageVaccine.DiseaseId}");
                            await packageVaccineRepository.AddAsync(packageVaccine);
                        }

                        await _unitOfWork.SaveChangesAsync();

                        vaccinePackage.Price = await CalculatePackagePriceAsync(vaccinePackage.PackageId);
                        _logger.LogInformation($"Calculated Price for PackageId {vaccinePackage.PackageId}: {vaccinePackage.Price}");
                        packageRepository.Update(vaccinePackage);
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

        public async Task<PackageVaccineDTO> AddVaccineToPackageAsync(int packageId, CreatePackageVaccineDTO packageVaccineDto, int accountId)
        {
            try
            {
                _logger.LogInformation($"Adding vaccine to package with PackageId: {packageId}, FacilityVaccineId: {packageVaccineDto.FacilityVaccineId}, by AccountId: {accountId}");

                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var package = await packageRepository.GetAsync(p => p.PackageId == packageId);
                if (package == null)
                {
                    throw new InvalidOperationException($"Gói vaccine với ID {packageId} không tồn tại");
                }

                await ValidateManagerAccess(accountId, package.FacilityId);
                await ValidateVaccineInput(packageVaccineDto.FacilityVaccineId, package.FacilityId);

                var packageVaccineRepository = _unitOfWork.GetRepository<PackageVaccine>();
                var existingPackageVaccine = await packageVaccineRepository.AnyAsync(pv => pv.PackageId == packageId && pv.FacilityVaccineId == packageVaccineDto.FacilityVaccineId);
                if (existingPackageVaccine)
                {
                    throw new InvalidOperationException($"FacilityVaccine với PackageId {packageId} và FacilityVaccineId {packageVaccineDto.FacilityVaccineId} đã tồn tại");
                }

                var diseaseId = await GetDiseaseIdForFacilityVaccineAsync(packageVaccineDto.FacilityVaccineId);
                var packageVaccine = new PackageVaccine
                {
                    PackageId = packageId,
                    FacilityVaccineId = packageVaccineDto.FacilityVaccineId,
                    Quantity = packageVaccineDto.Quantity,
                    DiseaseId = diseaseId // Lấy DiseaseId từ GetDiseaseIdForFacilityVaccineAsync
                };
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    throw new InvalidOperationException($"Invalid DateTime value for CreatedAt/UpdatedAt: {currentTime}");
                }

                packageVaccine.CreatedAt = currentTime;
                packageVaccine.UpdatedAt = currentTime;
                _logger.LogInformation($"PackageVaccine CreatedAt: {packageVaccine.CreatedAt:yyyy-MM-dd HH:mm:ss}, UpdatedAt: {packageVaccine.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"PackageVaccine object before saving: {JsonConvert.SerializeObject(packageVaccine)}");

                await packageVaccineRepository.AddAsync(packageVaccine);
                package.Price = await CalculatePackagePriceAsync(packageId);
                package.UpdatedAt = currentTime;
                packageRepository.Update(package);

                await _unitOfWork.SaveChangesAsync();

                var savedPackageVaccine = await packageVaccineRepository.GetAsync(pv => pv.PackageVaccineId == packageVaccine.PackageVaccineId);
                return _mapper.Map<PackageVaccineDTO>(savedPackageVaccine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding vaccine to package with PackageId {packageId} and FacilityVaccineId {packageVaccineDto.FacilityVaccineId}");
                throw;
            }
        }

        public async Task<VaccinePackageDTO> UpdateVaccinePackageAsync(int packageId, UpdateVaccinePackageDTO vaccinePackageDto, int accountId)
        {
            try
            {
                _logger.LogInformation($"Updating vaccine package with ID: {packageId}, by AccountId: {accountId}");

                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var vaccinePackage = await packageRepository.GetAsync(p => p.PackageId == packageId);
                if (vaccinePackage == null)
                {
                    throw new KeyNotFoundException($"Gói vaccine với ID {packageId} không tồn tại");
                }

                await ValidateManagerAccess(accountId, vaccinePackageDto.FacilityId);

                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facilityExists = await facilityRepository.AnyAsync(f => f.FacilityId == vaccinePackageDto.FacilityId);
                if (!facilityExists)
                {
                    throw new InvalidOperationException($"Facility với ID {vaccinePackageDto.FacilityId} không tồn tại");
                }

                var existingPackage = await packageRepository.AnyAsync(p => p.Name == vaccinePackageDto.Name && p.FacilityId == vaccinePackageDto.FacilityId && p.PackageId != packageId);
                if (existingPackage)
                {
                    throw new InvalidOperationException($"Gói vaccine với tên '{vaccinePackageDto.Name}' đã tồn tại trong facility {vaccinePackageDto.FacilityId}");
                }

                _mapper.Map(vaccinePackageDto, vaccinePackage);
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    throw new InvalidOperationException($"Invalid DateTime value for UpdatedAt: {currentTime}");
                }
                vaccinePackage.UpdatedAt = currentTime;
                vaccinePackage.Price = await CalculatePackagePriceAsync(packageId);
                _logger.LogInformation($"VaccinePackage UpdatedAt: {vaccinePackage.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"VaccinePackage object before saving: {JsonConvert.SerializeObject(vaccinePackage)}");

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

        public async Task<PackageVaccineDTO> UpdateVaccineInPackageAsync(int packageId, int facilityVaccineId, UpdatePackageVaccineDTO packageVaccineDto, int accountId)
        {
            try
            {
                _logger.LogInformation($"Updating vaccine in package with PackageId: {packageId}, FacilityVaccineId: {facilityVaccineId}, by AccountId: {accountId}");

                var packageVaccineRepository = _unitOfWork.GetRepository<PackageVaccine>();
                var packageVaccine = await packageVaccineRepository.GetAsync(pv => pv.PackageId == packageId && pv.FacilityVaccineId == facilityVaccineId);
                if (packageVaccine == null)
                {
                    throw new KeyNotFoundException($"FacilityVaccine với PackageId {packageId} và FacilityVaccineId {facilityVaccineId} không tồn tại");
                }

                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var package = await packageRepository.GetAsync(p => p.PackageId == packageId);
                if (package == null)
                {
                    throw new InvalidOperationException($"Gói vaccine với ID {packageId} không tồn tại");
                }

                await ValidateManagerAccess(accountId, package.FacilityId);
                await ValidateVaccineInput(facilityVaccineId, package.FacilityId); // Sử dụng facilityVaccineId từ route

                var diseaseId = await GetDiseaseIdForFacilityVaccineAsync(facilityVaccineId); // Lấy DiseaseId từ facilityVaccineId hiện tại
                packageVaccine.Quantity = packageVaccineDto.Quantity;
                packageVaccine.DiseaseId = diseaseId;
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    throw new InvalidOperationException($"Invalid DateTime value for UpdatedAt: {currentTime}");
                }
                packageVaccine.UpdatedAt = currentTime;
                _logger.LogInformation($"PackageVaccine UpdatedAt: {packageVaccine.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"PackageVaccine object before saving: {JsonConvert.SerializeObject(packageVaccine, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })}");

                packageVaccineRepository.Update(packageVaccine);
                package.Price = await CalculatePackagePriceAsync(packageId);
                package.UpdatedAt = currentTime;
                packageRepository.Update(package);

                await _unitOfWork.SaveChangesAsync();

                var updatedPackageVaccine = await packageVaccineRepository.GetAsync(pv => pv.PackageId == packageId && pv.FacilityVaccineId == facilityVaccineId);
                var resultPackageVaccineDto = _mapper.Map<PackageVaccineDTO>(updatedPackageVaccine);
                return resultPackageVaccineDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vaccine in package with PackageId {packageId} and FacilityVaccineId {facilityVaccineId}");
                throw;
            }
        }


        public async Task<bool> DeleteVaccinePackageAsync(int packageId, int accountId)
        {
            try
            {
                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var vaccinePackage = await packageRepository.GetAsync(p => p.PackageId == packageId);
                if (vaccinePackage == null)
                {
                    throw new KeyNotFoundException($"Gói vaccine với ID {packageId} không tồn tại");
                }

                await ValidateManagerAccess(accountId, vaccinePackage.FacilityId);
                var packageVaccineRepository = _unitOfWork.GetRepository<PackageVaccine>();
                var packageVaccines = await packageVaccineRepository.GetAllAsync(
                    filter: pv => pv.PackageId == packageId,
                    pageIndex: null,
                    pageSize: null,
                    include: ""
                );
                foreach (var packageVaccine in packageVaccines.Data)
                {
                    packageVaccineRepository.Delete(packageVaccine);
                }
                await _unitOfWork.SaveChangesAsync();

                // Xóa VaccinePackage
                packageRepository.Delete(vaccinePackage);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, $"Database error deleting vaccine package with ID {packageId}: Foreign key constraint violation or related data exists");
                throw new InvalidOperationException("Không thể xóa gói vaccine vì có dữ liệu liên quan chưa được xóa.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting vaccine package with ID {packageId}");
                throw;
            }
        }

        public async Task<bool> DeleteVaccineFromPackageAsync(int packageId, int facilityVaccineId, int accountId)
        {
            try
            {
                var packageVaccineRepository = _unitOfWork.GetRepository<PackageVaccine>();
                var packageVaccine = await packageVaccineRepository.GetAsync(pv => pv.PackageId == packageId && pv.FacilityVaccineId == facilityVaccineId);
                if (packageVaccine == null)
                {
                    throw new KeyNotFoundException($"FacilityVaccine với PackageId {packageId} và FacilityVaccineId {facilityVaccineId} không tồn tại");
                }

                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var package = await packageRepository.GetAsync(p => p.PackageId == packageId);
                if (package == null)
                {
                    throw new InvalidOperationException($"Gói vaccine với ID {packageId} không tồn tại");
                }

                await ValidateManagerAccess(accountId, package.FacilityId);

                packageVaccineRepository.Delete(packageVaccine);
                package.Price = await CalculatePackagePriceAsync(packageId);
                package.UpdatedAt = DateTime.UtcNow;
                packageRepository.Update(package);

                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting vaccine from package with PackageId {packageId} and FacilityVaccineId {facilityVaccineId}");
                throw;
            }
        }

        public async Task<VaccinePackageDTO> GetVaccinePackageByIdAsync(int packageId)
        {
            try
            {
                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var vaccinePackage = await packageRepository.GetAsync(
                    p => p.PackageId == packageId,
                    includeProperties: "PackageVaccines,PackageVaccines.FacilityVaccine,PackageVaccines.Disease,PackageVaccines.FacilityVaccine.Vaccine"
                );
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

        public async Task<QueryResultModel<IEnumerable<VaccinePackageDTO>>> GetAllVaccinePackagesAsync(
    Expression<Func<VaccinePackage, bool>>? filter = null,
    Func<IQueryable<VaccinePackage>, IOrderedQueryable<VaccinePackage>>? orderBy = null,
    string include = "",
    int? pageIndex = null,
    int? pageSize = null)
        {
            try
            {
                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var result = await packageRepository.GetAllAsync(
                    filter: filter,
                    orderBy: orderBy,
                    include: "PackageVaccines,PackageVaccines.FacilityVaccine,PackageVaccines.Disease,PackageVaccines.FacilityVaccine.Vaccine",
                    pageIndex: pageIndex,
                    pageSize: pageSize);

                var vaccinePackageDtos = _mapper.Map<IEnumerable<VaccinePackageDTO>>(result.Data);
                return new QueryResultModel<IEnumerable<VaccinePackageDTO>>
                {
                    TotalCount = result.TotalCount,
                    Data = vaccinePackageDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all vaccine packages with pagination");
                throw;
            }
        }
        public async Task<VaccinePackageDTO> UpdateVaccinePackageWithNewVaccineAsync(int packageId, AddPackageVaccineDTO packageVaccineDto, int accountId)
        {
            try
            {
                _logger.LogInformation($"Updating vaccine package with new vaccine for PackageId: {packageId}, FacilityVaccineId: {packageVaccineDto.FacilityVaccineId}, by AccountId: {accountId}");

                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var package = await packageRepository.GetAsync(p => p.PackageId == packageId, includeProperties: "PackageVaccines");
                if (package == null)
                {
                    throw new InvalidOperationException($"Gói vaccine với ID {packageId} không tồn tại");
                }

                await ValidateManagerAccess(accountId, package.FacilityId);
                await ValidateVaccineInput(packageVaccineDto.FacilityVaccineId, package.FacilityId);

                var packageVaccineRepository = _unitOfWork.GetRepository<PackageVaccine>();
                var existingPackageVaccine = await packageVaccineRepository.AnyAsync(pv => pv.PackageId == packageId && pv.FacilityVaccineId == packageVaccineDto.FacilityVaccineId);
                if (existingPackageVaccine)
                {
                    throw new InvalidOperationException($"FacilityVaccine với PackageId {packageId} và FacilityVaccineId {packageVaccineDto.FacilityVaccineId} đã tồn tại");
                }

                var diseaseId = await GetDiseaseIdForFacilityVaccineAsync(packageVaccineDto.FacilityVaccineId);
                var packageVaccine = new PackageVaccine
                {
                    PackageId = packageId,
                    FacilityVaccineId = packageVaccineDto.FacilityVaccineId,
                    Quantity = packageVaccineDto.Quantity,
                    DiseaseId = diseaseId
                };
                var currentTime = DateTime.UtcNow;
                if (currentTime < new DateTime(1753, 1, 1) || currentTime > new DateTime(9999, 12, 31))
                {
                    throw new InvalidOperationException($"Invalid DateTime value for CreatedAt/UpdatedAt: {currentTime}");
                }

                packageVaccine.CreatedAt = currentTime;
                packageVaccine.UpdatedAt = currentTime;
                _logger.LogInformation($"New PackageVaccine CreatedAt: {packageVaccine.CreatedAt:yyyy-MM-dd HH:mm:ss}, UpdatedAt: {packageVaccine.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"New PackageVaccine object before saving: {JsonConvert.SerializeObject(packageVaccine)}");

                await packageVaccineRepository.AddAsync(packageVaccine);
                await _unitOfWork.SaveChangesAsync(); // Lưu trước khi tính giá

                package.Price = await CalculatePackagePriceAsync(packageId);
                _logger.LogInformation($"Calculated new Price for PackageId {packageId}: {package.Price}");
                package.UpdatedAt = currentTime;
                packageRepository.Update(package);

                await _unitOfWork.SaveChangesAsync();

                var updatedPackage = await packageRepository.GetAsync(p => p.PackageId == packageId, includeProperties: "PackageVaccines");
                return _mapper.Map<VaccinePackageDTO>(updatedPackage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating vaccine package with new vaccine for PackageId {packageId} and FacilityVaccineId {packageVaccineDto.FacilityVaccineId}");
                throw;
            }

        }
        public async Task<int> GetCountByFacilityAsync(int facilityId)
        {
            try
            {
                _logger.LogInformation($"Counting VaccinePackages for FacilityId: {facilityId}");
                var repository = _unitOfWork.GetRepository<VaccinePackage>();
                return await repository.CountAsync(vp => vp.FacilityId == facilityId && vp.Status == "true");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error counting VaccinePackages for FacilityId {facilityId}");
                throw;
            }
        }
    }
}