using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Contracts.DTOs.Account;
using Contracts.DTOs.Member;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Common;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountService> _logger;
        private readonly Cloudinary _cloudinary;

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
                config.ApiKey,
                config.ApiSecret
            ));
        }

        private async Task<string> UploadImageToCloudinary(IFormFile image)
        {
            if (image == null || image.Length == 0)
                throw new ArgumentException("Image is required");

            if (image.Length > 5 * 1024 * 1024)
                throw new ArgumentException("Image size must not exceed 5MB");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = System.IO.Path.GetExtension(image.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Image must be a jpg, jpeg, or png file");

            using var stream = image.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(image.FileName, stream),
                Folder = "member_images",
                UseFilename = true,
                UniqueFilename = false
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.LogError($"Cloudinary upload failed: Status={uploadResult.StatusCode}, Error={uploadResult.Error?.Message}");
                throw new InvalidOperationException($"Failed to upload image to Cloudinary: {uploadResult.Error?.Message}");
            }
            _logger.LogInformation($"Uploaded image: {uploadResult.SecureUrl}");
            return uploadResult.SecureUrl.AbsoluteUri;
        }

        private string ExtractPublicIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var segments = path.Split('/');
            var index = Array.IndexOf(segments, "member_images");
            if (index != -1 && index + 1 < segments.Length)
            {
                return $"member_images/{segments[index + 1]}";
            }
            return null;
        }

        public async Task<AccountDTO> UpdateAccountAsync(UpdateAccountDTO request, int currentUserId)
        {
            try
            {
                var accountRepository = _unitOfWork.GetRepository<Repositories.Entities.Account>();
                var account = await accountRepository.GetAsync(a => a.AccountId == currentUserId);

                if (account == null)
                {
                    throw new UnauthorizedAccessException("Tài khoản không tồn tại");
                }

                using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    var originalCreatedAt = account.CreatedAt;

                    _mapper.Map(request, account);

                    account.CreatedAt = originalCreatedAt;
                    account.Role = account.Role;
                    account.Status = account.Status;
                    account.UpdatedAt = DateTime.UtcNow;

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
                if (account.Role == "Member")
                {
                    var memberRepository = _unitOfWork.GetRepository<Member>();
                    var member = await memberRepository.GetAsync(m => m.AccountId == currentUserId);
                    if (member != null)
                    {
                        response.PhoneNumber = member.PhoneNumber;
                        response.Address = member.Address;
                    }
                }

                _logger.LogInformation($"Retrieved account info for {account.AccountName}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving account {currentUserId}");
                throw;
            }
        }

        public async Task<MemberInfoResponseDTO> UpdateMemberInfoAsync(UpdateMemberInfoDTO request, int currentUserId)
        {
            try
            {
                var memberRepository = _unitOfWork.GetRepository<Member>();
                var member = await memberRepository.GetAsync(m => m.AccountId == currentUserId, includeProperties: "Account");

                if (member == null)
                {
                    throw new UnauthorizedAccessException("Tài khoản Member không tồn tại");
                }

                using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    member.FullName = request.FullName;
                    member.PhoneNumber = request.PhoneNumber;
                    member.Address = request.Address;
                    member.UpdatedAt = DateTime.UtcNow;

                    if (request.ImageUrl != null)
                    {
                        if (!string.IsNullOrEmpty(member.Account.ImageUrl))
                        {
                            var publicId = ExtractPublicIdFromUrl(member.Account.ImageUrl);
                            if (!string.IsNullOrEmpty(publicId))
                            {
                                await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                            }
                        }
                        member.Account.ImageUrl = await UploadImageToCloudinary(request.ImageUrl);
                    }

                    memberRepository.Update(member);
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var response = _mapper.Map<MemberInfoResponseDTO>(member);
                    _logger.LogInformation($"Member {member.FullName} updated their info successfully");
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
                _logger.LogError($"Update member info failed: {ex.Message}");
                throw;
            }
        }
    }
}