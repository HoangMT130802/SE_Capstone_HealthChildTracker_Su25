using Contracts.DTOs.FacilityRating;
using Repositories.Models.QueryModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IFacilityRatingService
    {
        Task<FacilityRatingDTO> CreateFacilityRatingAsync(CreateFacilityRatingDTO ratingDto);
        Task<FacilityRatingDTO> GetFacilityRatingByIdAsync(int ratingId);
        Task<QueryResultModel<IEnumerable<FacilityRatingDTO>>> GetFacilityRatingsAsync(int? facilityId = null, int? memberId = null, int? pageIndex = null, int? pageSize = null);
        Task<FacilityRatingDTO> UpdateFacilityRatingAsync(int ratingId, UpdateFacilityRatingDTO ratingDto);
        Task DeleteFacilityRatingAsync(int ratingId);
        Task<double> GetAverageRatingByFacilityAsync(int facilityId);
    }
}
