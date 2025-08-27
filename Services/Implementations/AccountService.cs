using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Contracts.DTOs.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Common;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Threading.Tasks;
using Repositories.Models;

namespace Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork; private readonly IMapper _mapper; private readonly ILogger _logger; private readonly Cloudinary _cloudinary;

        private async Task<string> UploadImageToCloudinary(IFormFile image)
        {
            if (image == null || image.Length == 0)
                throw new ArgumentException("Image is required");

            using var stream = image.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(image.FileName, stream),
                Folder = "account_images"
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Failed to upload image to Cloudinary");
            }
            return uploadResult.SecureUrl.AbsoluteUri;
        }

        public AccountService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AccountService> logger, IOptions<CloudinarySettings> cloudinaryConfig)
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
                config.ApiKey, // Sửa lỗi: Sử dụng ApiKey thay vì CloudName
                config.ApiSecret
            ));
        }

        public async Task<AccountDTO> UpdateAccountAsync(UpdateAccountDTO request, int currentUserId)
        {
            try
            {
                // Validate input
                var accountRepository = _unitOfWork.GetRepository<Repositories.Entities.Account>();
                var account = await accountRepository.GetAsync(a => a.AccountId == currentUserId);

                if (account == null)
                {
                    throw new UnauthorizedAccessException("Tài khoản không tồn tại");
                }
                using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // Lưu CreatedAt gốc
                    var originalCreatedAt = account.CreatedAt;

                    // Ánh xạ DTO sang entity
                    _mapper.Map(request, account);

                    // Khôi phục các trường không cho phép cập nhật
                    account.CreatedAt = originalCreatedAt;
                    account.Role = account.Role;
                    account.Status = account.Status;
                    account.UpdatedAt = DateTime.UtcNow;

                    // Xử lý ảnh nếu có
                    if (request.Image != null)
                    {
                        account.ImageUrl = await UploadImageToCloudinary(request.Image);
                    }

                    accountRepository.Update(account);
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var response = _mapper.Map<AccountDTO>(account);

                    _logger.LogInformation($"Account {account.AccountName} updated successfully");
                    return response;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating account {currentUserId}");
                throw;
            }
        }
        public async Task<AccountDTO> GetCurrentAccountAsync(int currentUserId)
        {
            try
            {
                var accountRepository = _unitOfWork.GetRepository<Repositories.Entities.Account>();
                var account = await accountRepository.GetAsync(a => a.AccountId == currentUserId);

                if (account == null)
                {
                    throw new KeyNotFoundException("Tài khoản không tồn tại");
                }

                var response = _mapper.Map<AccountDTO>(account);
                _logger.LogInformation($"Retrieved account info for {account.AccountName}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving account {currentUserId}");
                throw;
            }
        }
    }
}
