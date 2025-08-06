using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Contracts.DTOs;
using Contracts.DTOs.VaccinationFacilityPaymentAccount;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

public class VaccinationFacilityPaymentAccountService : IVaccinationFacilityPaymentAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<VaccinationFacilityPaymentAccountService> _logger;
    private readonly Cloudinary _cloudinary;

    public VaccinationFacilityPaymentAccountService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<VaccinationFacilityPaymentAccountService> logger, IOptions<CloudinarySettings> cloudinaryConfig)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var config = cloudinaryConfig.Value;
        if (string.IsNullOrEmpty(config.CloudName) || string.IsNullOrEmpty(config.ApiKey) || string.IsNullOrEmpty(config.ApiSecret))
        {
            throw new ArgumentException("Cloudinary configuration is incomplete or invalid.");
        }
        _cloudinary = new Cloudinary(new CloudinaryDotNet.Account(
            config.CloudName,
            config.ApiKey,
            config.ApiSecret
        ));
    }

    private async Task ValidateManagerAccess(int accountId)
    {
        var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
        var staff = await staffRepository.GetAsync(s => s.AccountId == accountId && s.Position == "Manager");
        if (staff == null)
        {
            throw new UnauthorizedAccessException($"User with AccountId {accountId} is not a Manager or does not belong to Facility");
        }
    }

    private async Task<string> UploadImageToCloudinary(IFormFile image)
    {
        if (image == null || image.Length == 0)
            throw new ArgumentException("QR code image is required");

        using var stream = image.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(image.FileName, stream),
            Folder = "vaccination_qrcodes"
        };
        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        return uploadResult.SecureUrl.AbsoluteUri;
    }

    public async Task<int> CreatePaymentAccountAsync(CreateVaccinationFacilityPaymentAccountDto paymentAccountDto, int accountId)
    {
        try
        {
            await ValidateManagerAccess(accountId);

            var qrcodeImageUrl = await UploadImageToCloudinary(paymentAccountDto.QrcodeImage);

            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var existingAccountsResult = await repository.GetAllAsync(
                filter: pa => pa.FacilityId == paymentAccountDto.FacilityId,
                orderBy: null,
                pageIndex: null,
                pageSize: null
            );

            if (paymentAccountDto.IsActive)
            {
                var activeAccountIds = existingAccountsResult.Data
                    .Where(pa => pa.IsActive == "true")
                    .Select(pa => pa.Id)
                    .ToList();

                if (activeAccountIds.Any())
                {
                    foreach (var id in activeAccountIds)
                    {
                        var accountToUpdate = await repository.GetAsync(pa => pa.Id == id);
                        if (accountToUpdate != null)
                        {
                            accountToUpdate.IsActive = "false";
                            repository.Update(accountToUpdate);
                        }
                    }
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            var paymentAccount = new VaccinationFacilityPaymentAccount
            {
                FacilityId = paymentAccountDto.FacilityId,
                BankName = paymentAccountDto.BankName,
                AccountNumber = paymentAccountDto.AccountNumber,
                AccountHolder = paymentAccountDto.AccountHolder,
                QrcodeImageUrl = qrcodeImageUrl,
                IsActive = paymentAccountDto.IsActive ? "true" : "false",
                CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow),
                UpdatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            await repository.AddAsync(paymentAccount);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Created payment account with ID {paymentAccount.Id} by AccountId {accountId}");
            return paymentAccount.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating payment account by AccountId {accountId}: {ex.Message}");
            throw;
        }
    }

    public async Task UpdatePaymentAccountAsync(int id, UpdateVaccinationFacilityPaymentAccountDto paymentAccountDto, int accountId)
    {
        try
        {
            await ValidateManagerAccess(accountId);

            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccount = await repository.GetAsync(pa => pa.Id == id);
            if (paymentAccount == null)
                throw new KeyNotFoundException($"Payment account with ID {id} not found");

            if (paymentAccountDto.IsActive && paymentAccount.IsActive != "true")
            {
                var existingAccountsResult = await repository.GetAllAsync(
                    filter: pa => pa.FacilityId == paymentAccount.FacilityId && pa.Id != id,
                    orderBy: null,
                    pageIndex: null,
                    pageSize: null
                );
                var activeAccountIds = existingAccountsResult.Data
                    .Where(pa => pa.IsActive == "true")
                    .Select(pa => pa.Id)
                    .ToList();

                if (activeAccountIds.Any())
                {
                    foreach (var accountIdToUpdate in activeAccountIds)
                    {
                        var accountToUpdate = await repository.GetAsync(pa => pa.Id == accountIdToUpdate);
                        if (accountToUpdate != null)
                        {
                            accountToUpdate.IsActive = "false";
                            repository.Update(accountToUpdate);
                        }
                    }
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            if (paymentAccountDto.QrcodeImage != null)
            {
                var qrcodeImageUrl = await UploadImageToCloudinary(paymentAccountDto.QrcodeImage);
                paymentAccount.QrcodeImageUrl = qrcodeImageUrl;
            }

            paymentAccount.BankName = paymentAccountDto.BankName;
            paymentAccount.AccountNumber = paymentAccountDto.AccountNumber;
            paymentAccount.AccountHolder = paymentAccountDto.AccountHolder;
            paymentAccount.IsActive = paymentAccountDto.IsActive ? "true" : "false";
            paymentAccount.UpdatedAt = DateOnly.FromDateTime(DateTime.UtcNow);

            repository.Update(paymentAccount);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Updated payment account with ID {id} by AccountId {accountId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating payment account with ID {id} by AccountId {accountId}: {ex.Message}");
            throw;
        }
    }

    public async Task DeletePaymentAccountAsync(int id, int accountId)
    {
        try
        {
            await ValidateManagerAccess(accountId);

            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccount = await repository.GetAsync(pa => pa.Id == id);
            if (paymentAccount == null)
                throw new KeyNotFoundException($"Payment account with ID {id} not found");

            if (!string.IsNullOrEmpty(paymentAccount.QrcodeImageUrl))
            {
                var publicId = paymentAccount.QrcodeImageUrl.Split('/').Last().Split('.').First();
                var deletionParams = new DeletionParams(publicId);
                await _cloudinary.DestroyAsync(deletionParams);
            }

            repository.Delete(paymentAccount);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Deleted payment account with ID {id} by AccountId {accountId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting payment account with ID {id} by AccountId {accountId}: {ex.Message}");
            throw;
        }
    }

    public async Task<VaccinationFacilityPaymentAccountDto> GetPaymentAccountByIdAsync(int id)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccount = await repository.GetAsync(pa => pa.Id == id);
            if (paymentAccount == null)
                throw new KeyNotFoundException($"Payment account with ID {id} not found");

            return _mapper.Map<VaccinationFacilityPaymentAccountDto>(paymentAccount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting payment account with ID {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>> GetAllPaymentAccountsAsync(bool? isActive = null, int? pageIndex = null, int? pageSize = null)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccountsResult = await repository.GetAllAsync(
                filter: isActive.HasValue ? pa => pa.IsActive == (isActive.Value ? "true" : "false") : null,
                orderBy: null,
                pageIndex: pageIndex,
                pageSize: pageSize
            );
            var dtos = _mapper.Map<IEnumerable<VaccinationFacilityPaymentAccountDto>>(paymentAccountsResult.Data);
            return new QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>
            {
                TotalCount = paymentAccountsResult.TotalCount,
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting all payment accounts: {ex.Message}");
            throw;
        }
    }

    public async Task<QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>> GetPaymentAccountByFacilityIdAsync(int facilityId, bool? isActive = null, int? pageIndex = null, int? pageSize = null)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccountsResult = await repository.GetAllAsync(
                filter: pa => pa.FacilityId == facilityId && (!isActive.HasValue || pa.IsActive == (isActive.Value ? "true" : "false")),
                orderBy: null,
                pageIndex: pageIndex,
                pageSize: pageSize
            );
            var dtos = _mapper.Map<IEnumerable<VaccinationFacilityPaymentAccountDto>>(paymentAccountsResult.Data);
            return new QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>
            {
                TotalCount = paymentAccountsResult.TotalCount,
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting payment accounts for FacilityId {facilityId}: {ex.Message}");
            throw;
        }
    }
}