using AutoMapper;
using Contracts.DTOs.FacilityRating;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Services.Interfaces;
using System;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class FacilityRatingService : IFacilityRatingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<FacilityRatingService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FacilityRatingService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<FacilityRatingService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        private async Task<int> GetCurrentAccountIdAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var accountIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(accountIdClaim, out int accountId))
                {
                    return accountId;
                }
            }
            return 0;
        }

        private async Task<int> GetCurrentMemberIdAsync()
        {
            var accountId = await GetCurrentAccountIdAsync();
            if (accountId == 0)
            {
                throw new UnauthorizedAccessException("Không thể xác định AccountId của người dùng hiện tại từ token");
            }

            var memberRepository = _unitOfWork.GetRepository<Member>();
            var member = await memberRepository.GetAsync(m => m.AccountId == accountId);
            if (member == null)
            {
                _logger.LogWarning($"No Member found for AccountId {accountId}. User may need to register as a member.");
                throw new UnauthorizedAccessException($"Không tìm thấy Member gắn với AccountId {accountId}");
            }
            return member.MemberId;
        }

        private void ValidateRatingValues(FacilityRating rating)
        {
            if (rating.ServiceQuality < 1 || rating.ServiceQuality > 5)
                throw new ArgumentException("ServiceQuality must be between 1 and 5");
            if (rating.FacilityCleanliness < 1 || rating.FacilityCleanliness > 5)
                throw new ArgumentException("FacilityCleanliness must be between 1 and 5");
            if (rating.StaffAttitude < 1 || rating.StaffAttitude > 5)
                throw new ArgumentException("StaffAttitude must be between 1 and 5");

            // Tính Rating trung bình và làm tròn
            rating.Rating = (int)Math.Round((rating.ServiceQuality + rating.FacilityCleanliness + rating.StaffAttitude) / 3.0);
        }

        public async Task<FacilityRatingDTO> CreateFacilityRatingAsync(CreateFacilityRatingDTO ratingDto)
        {
            try
            {
                _logger.LogInformation($"Creating rating for FacilityId: {ratingDto.FacilityId}");

                var memberId = await GetCurrentMemberIdAsync();
                if (memberId <= 0)
                {
                    throw new UnauthorizedAccessException("Invalid MemberId derived from AccountId");
                }

                var rating = _mapper.Map<FacilityRating>(ratingDto);
                rating.MemberId = memberId;

                ValidateRatingValues(rating);

                var facilityRepository = _unitOfWork.GetRepository<VaccinationFacility>();
                var facility = await facilityRepository.GetAsync(f => f.FacilityId == ratingDto.FacilityId);
                if (facility == null)
                {
                    throw new KeyNotFoundException($"Facility with ID {ratingDto.FacilityId} not found");
                }

                var ratingRepository = _unitOfWork.GetRepository<FacilityRating>();
                await ratingRepository.AddAsync(rating);
                await _unitOfWork.SaveChangesAsync();

                var savedRating = await ratingRepository.GetAsync(r => r.RatingId == rating.RatingId,
                    includeProperties: "Facility,Member");
                return _mapper.Map<FacilityRatingDTO>(savedRating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating rating for FacilityId {ratingDto?.FacilityId}");
                throw;
            }
        }

        public async Task<FacilityRatingDTO> GetFacilityRatingByIdAsync(int ratingId)
        {
            try
            {
                _logger.LogInformation($"Retrieving rating with ID: {ratingId}");
                var ratingRepository = _unitOfWork.GetRepository<FacilityRating>();
                var rating = await ratingRepository.GetAsync(r => r.RatingId == ratingId,
                    includeProperties: "Facility,Member");
                if (rating == null)
                {
                    throw new KeyNotFoundException($"Rating with ID {ratingId} not found");
                }
                return _mapper.Map<FacilityRatingDTO>(rating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving rating with ID {ratingId}");
                throw;
            }
        }

        public async Task<QueryResultModel<IEnumerable<FacilityRatingDTO>>> GetFacilityRatingsAsync(int? facilityId = null, int? memberId = null, int? pageIndex = null, int? pageSize = null)
        {
            try
            {
                _logger.LogInformation($"Retrieving ratings for FacilityId: {facilityId}, MemberId: {memberId}");
                var ratingRepository = _unitOfWork.GetRepository<FacilityRating>();
                Expression<Func<FacilityRating, bool>>? filter = null;
                if (facilityId.HasValue || memberId.HasValue)
                {
                    filter = r => (!facilityId.HasValue || r.FacilityId == facilityId) && (!memberId.HasValue || r.MemberId == memberId);
                }

                var result = await ratingRepository.GetAllAsync(
                    filter: filter,
                    include: "Facility,Member",
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var ratingDtos = _mapper.Map<IEnumerable<FacilityRatingDTO>>(result.Data);
                return new QueryResultModel<IEnumerable<FacilityRatingDTO>>
                {
                    TotalCount = result.TotalCount,
                    Data = ratingDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving ratings for FacilityId {facilityId}, MemberId {memberId}");
                throw;
            }
        }

        public async Task<FacilityRatingDTO> UpdateFacilityRatingAsync(int ratingId, UpdateFacilityRatingDTO ratingDto)
        {
            try
            {
                _logger.LogInformation($"Updating rating with ID: {ratingId}");
                var memberId = await GetCurrentMemberIdAsync();
                var ratingRepository = _unitOfWork.GetRepository<FacilityRating>();
                var rating = await ratingRepository.GetAsync(r => r.RatingId == ratingId);
                if (rating == null)
                {
                    throw new KeyNotFoundException($"Rating with ID {ratingId} not found");
                }
                if (rating.MemberId != memberId)
                {
                    throw new UnauthorizedAccessException("You are not authorized to update this rating");
                }

                _mapper.Map(ratingDto, rating);
                ValidateRatingValues(rating);
                rating.UpdatedAt = DateTime.UtcNow;

                ratingRepository.Update(rating);
                await _unitOfWork.SaveChangesAsync();

                var updatedRating = await ratingRepository.GetAsync(r => r.RatingId == ratingId,
                    includeProperties: "Facility,Member");
                return _mapper.Map<FacilityRatingDTO>(updatedRating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating rating with ID {ratingId}");
                throw;
            }
        }

        public async Task DeleteFacilityRatingAsync(int ratingId)
        {
            try
            {
                _logger.LogInformation($"Deleting rating with ID: {ratingId}");
                var memberId = await GetCurrentMemberIdAsync();
                var ratingRepository = _unitOfWork.GetRepository<FacilityRating>();
                var rating = await ratingRepository.GetAsync(r => r.RatingId == ratingId);
                if (rating == null)
                {
                    throw new KeyNotFoundException($"Rating with ID {ratingId} not found");
                }
                if (rating.MemberId != memberId)
                {
                    throw new UnauthorizedAccessException("You are not authorized to delete this rating");
                }

                ratingRepository.Delete(rating);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting rating with ID {ratingId}");
                throw;
            }
        }

        public async Task<double> GetAverageRatingByFacilityAsync(int facilityId)
        {
            try
            {
                _logger.LogInformation($"Calculating average rating for FacilityId: {facilityId}");
                var ratingRepository = _unitOfWork.GetRepository<FacilityRating>();
                var ratings = await ratingRepository.GetAllAsync(
                    filter: r => r.FacilityId == facilityId,
                    include: "Facility"
                );

                if (!ratings.Data.Any())
                {
                    return 0.0; // Trả về 0 nếu không có rating
                }

                var average = ratings.Data.Average(r => r.Rating);
                return Math.Round(average, 2); // Làm tròn 2 chữ số thập phân
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating average rating for FacilityId {facilityId}");
                throw;
            }
        }
    }
}