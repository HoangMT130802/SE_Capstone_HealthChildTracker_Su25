using AutoMapper;
using Contracts.DTOs.DeviceToken;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class DeviceTokenService : IDeviceTokenService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<DeviceTokenService> _logger;

        public DeviceTokenService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<DeviceTokenService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DeviceTokenResponseDto> RegisterDeviceTokenAsync(int accountId, DeviceTokenCreateDto deviceTokenDto)
        {
            try
            {
                var deviceTokenRepo = _unitOfWork.GetRepository<DeviceToken>();

                // BƯỚC 1: Kiểm tra token đã được account khác sử dụng chưa
                var existingTokensForOtherAccounts = await deviceTokenRepo.FindAsync(
                    dt => dt.Token == deviceTokenDto.Token && dt.AccountId != accountId);

                if (existingTokensForOtherAccounts.Any())
                {
                    // Xóa token khỏi các account khác (1 device = 1 account)
                    foreach (var oldToken in existingTokensForOtherAccounts)
                    {
                        deviceTokenRepo.Delete(oldToken);
                        _logger.LogWarning("Removed device token from account {OldAccountId} - device now belongs to account {NewAccountId}", 
                            oldToken.AccountId, accountId);
                    }
                }

                // BƯỚC 2: Kiểm tra token đã tồn tại cho account hiện tại chưa
                var existingToken = await deviceTokenRepo.GetAsync(
                    dt => dt.Token == deviceTokenDto.Token && dt.AccountId == accountId);

                if (existingToken != null)
                {
                    // Cập nhật thông tin device token hiện có
                    existingToken.DeviceType = deviceTokenDto.DeviceType;
                    existingToken.DeviceInfo = deviceTokenDto.DeviceInfo;
                    existingToken.IsActive = true;
                    existingToken.UpdatedAt = DateTime.UtcNow;
                    existingToken.LastUsedAt = DateTime.UtcNow;

                    deviceTokenRepo.Update(existingToken);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Updated existing device token for account {AccountId}", accountId);
                    return _mapper.Map<DeviceTokenResponseDto>(existingToken);
                }

                // Tạo device token mới
                var newDeviceToken = new DeviceToken
                {
                    AccountId = accountId,
                    Token = deviceTokenDto.Token,
                    DeviceType = deviceTokenDto.DeviceType,
                    DeviceInfo = deviceTokenDto.DeviceInfo,
                    IsActive = true, // Explicit set to fix NULL issue
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow
                };

                await deviceTokenRepo.AddAsync(newDeviceToken);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Registered new device token for account {AccountId}, device type: {DeviceType}", 
                    accountId, deviceTokenDto.DeviceType);

                return _mapper.Map<DeviceTokenResponseDto>(newDeviceToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device token for account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<bool> RemoveDeviceTokenAsync(int accountId, string token)
        {
            try
            {
                var deviceTokenRepo = _unitOfWork.GetRepository<DeviceToken>();
                var deviceToken = await deviceTokenRepo.GetAsync(
                    dt => dt.Token == token && dt.AccountId == accountId);

                if (deviceToken == null)
                {
                    _logger.LogWarning("Device token not found for removal: Account {AccountId}", accountId);
                    return false;
                }

                deviceTokenRepo.Delete(deviceToken);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Removed device token for account {AccountId}", accountId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing device token for account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<List<DeviceTokenResponseDto>> GetUserDeviceTokensAsync(int accountId)
        {
            try
            {
                var deviceTokenRepo = _unitOfWork.GetRepository<DeviceToken>();
                var tokens = await deviceTokenRepo.FindAsync(
                    dt => dt.AccountId == accountId && dt.IsActive);

                return _mapper.Map<List<DeviceTokenResponseDto>>(tokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device tokens for account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<List<string>> GetUserActiveTokensAsync(int accountId)
        {
            try
            {
                var deviceTokenRepo = _unitOfWork.GetRepository<DeviceToken>();
                var tokens = await deviceTokenRepo.FindAsync(
                    dt => dt.AccountId == accountId && dt.IsActive);

                return tokens.Select(dt => dt.Token).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active tokens for account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<bool> DeactivateDeviceTokenAsync(string token)
        {
            try
            {
                var deviceTokenRepo = _unitOfWork.GetRepository<DeviceToken>();
                var deviceToken = await deviceTokenRepo.GetAsync(dt => dt.Token == token);

                if (deviceToken == null)
                {
                    _logger.LogWarning("Device token not found for deactivation: {Token}", MaskToken(token));
                    return false;
                }

                deviceToken.IsActive = false;
                deviceToken.UpdatedAt = DateTime.UtcNow;

                deviceTokenRepo.Update(deviceToken);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deactivated device token for account {AccountId}", deviceToken.AccountId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating device token {Token}", MaskToken(token));
                throw;
            }
        }

        public async Task<int> CleanupInactiveTokensAsync(int daysInactive = 30)
        {
            try
            {
                var deviceTokenRepo = _unitOfWork.GetRepository<DeviceToken>();
                var cutoffDate = DateTime.UtcNow.AddDays(-daysInactive);

                var inactiveTokens = await deviceTokenRepo.FindAsync(
                    dt => dt.LastUsedAt < cutoffDate || (!dt.IsActive && dt.UpdatedAt < cutoffDate));

                if (inactiveTokens.Any())
                {
                    foreach (var token in inactiveTokens)
                    {
                        deviceTokenRepo.Delete(token);
                    }

                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} inactive device tokens", inactiveTokens.Count());
                    return inactiveTokens.Count();
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up inactive device tokens");
                throw;
            }
        }

        public async Task UpdateTokenLastUsedAsync(string token)
        {
            try
            {
                var deviceTokenRepo = _unitOfWork.GetRepository<DeviceToken>();
                var deviceToken = await deviceTokenRepo.GetAsync(dt => dt.Token == token);

                if (deviceToken != null)
                {
                    deviceToken.LastUsedAt = DateTime.UtcNow;
                    deviceTokenRepo.Update(deviceToken);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last used time for token {Token}", MaskToken(token));
                // Không throw exception để không ảnh hưởng đến luồng chính
            }
        }

        public async Task<List<int>> GetAccountIdsUsingTokenAsync(string token)
        {
            try
            {
                var deviceTokenRepo = _unitOfWork.GetRepository<DeviceToken>();
                var tokens = await deviceTokenRepo.FindAsync(dt => dt.Token == token && dt.IsActive);
                
                return tokens.Select(dt => dt.AccountId).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account IDs for token {Token}", MaskToken(token));
                throw;
            }
        }

        public async Task<bool> TransferDeviceTokenAsync(string token, int fromAccountId, int toAccountId)
        {
            try
            {
                var deviceTokenRepo = _unitOfWork.GetRepository<DeviceToken>();
                var deviceToken = await deviceTokenRepo.GetAsync(
                    dt => dt.Token == token && dt.AccountId == fromAccountId);

                if (deviceToken == null)
                {
                    _logger.LogWarning("Device token not found for transfer from account {FromAccountId} to {ToAccountId}", 
                        fromAccountId, toAccountId);
                    return false;
                }

                deviceToken.AccountId = toAccountId;
                deviceToken.UpdatedAt = DateTime.UtcNow;
                deviceToken.LastUsedAt = DateTime.UtcNow;

                deviceTokenRepo.Update(deviceToken);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Transferred device token from account {FromAccountId} to account {ToAccountId}", 
                    fromAccountId, toAccountId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring device token from account {FromAccountId} to {ToAccountId}", 
                    fromAccountId, toAccountId);
                throw;
            }
        }

        public async Task<int?> GetDeviceTokenIdByTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return null;

                var deviceTokenRepo = _unitOfWork.GetRepository<DeviceToken>();
                var deviceToken = await deviceTokenRepo.GetAllQueryable()
                    .Where(dt => dt.Token == token && dt.IsActive)
                    .FirstOrDefaultAsync();

                return deviceToken?.DeviceTokenId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device token ID for token {Token}", MaskToken(token));
                throw;
            }
        }

        private string MaskToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length < 10)
                return "***";
            
            return $"{token.Substring(0, 6)}...{token.Substring(token.Length - 4)}";
        }
    }
}
