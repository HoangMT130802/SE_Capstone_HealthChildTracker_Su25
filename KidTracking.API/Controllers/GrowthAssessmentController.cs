using AutoMapper;
using Contracts.DTOs.GrowthAssessment;
using Contracts.DTOs.GrowthRecord;
using Services.Interfaces;
using Repositories.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrowthAssessmentController : ControllerBase
    {
        private readonly IGrowthAssessmentService _assessmentService;
        private readonly IGrowthRecordService _recordService;
        private readonly IMapper _mapper;
        private readonly ILogger<GrowthAssessmentController> _logger;

        public GrowthAssessmentController(
            IGrowthAssessmentService assessmentService,
            IGrowthRecordService recordService,
            IMapper mapper,
            ILogger<GrowthAssessmentController> logger)
        {
            _assessmentService = assessmentService;
            _recordService = recordService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Đánh giá tăng trưởng dựa trên bản ghi cụ thể
        /// </summary>
        [HttpGet("record/{recordId}")]
        [ProducesResponseType(typeof(GrowthAssessmentDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GrowthAssessmentDTO>> AssessGrowthByRecordId(int recordId)
        {
            try
            {
                // Lấy growth record DTO
                var recordDto = await _recordService.GetGrowthRecordByIdAsync(recordId);
                if (recordDto == null)
                {
                    return NotFound($"Không tìm thấy bản ghi tăng trưởng với ID {recordId}");
                }

                // Sử dụng AutoMapper để chuyển đổi từ DTO sang entity
                var recordEntity = _mapper.Map<GrowthRecord>(recordDto);

                // Thực hiện đánh giá
                var assessment = await _assessmentService.AssessGrowthAsync(recordEntity);
                return Ok(assessment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đánh giá tăng trưởng cho bản ghi {RecordId}", recordId);
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu");
            }
        }

        /// <summary>
        /// Đánh giá tăng trưởng cho bản ghi mới nhất của trẻ
        /// </summary>
        [HttpGet("child/{childId}/latest")]
        [ProducesResponseType(typeof(GrowthAssessmentDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GrowthAssessmentDTO>> AssessLatestGrowthByChildId(int childId)
        {
            try
            {
                // Lấy tất cả bản ghi của trẻ
                var recordDtos = await _recordService.GetAllGrowthRecordsByChildIdAsync(childId);
                if (!recordDtos.Any())
                {
                    return NotFound($"Không tìm thấy bản ghi tăng trưởng nào cho trẻ với ID {childId}");
                }

                // Lấy bản ghi mới nhất theo CreatedAt
                var latestRecordDto = recordDtos
                    .OrderByDescending(r => r.CreatedAt)
                    .First();

                // Sử dụng AutoMapper để chuyển đổi từ DTO sang entity
                var recordEntity = _mapper.Map<GrowthRecord>(latestRecordDto);

                // Thực hiện đánh giá
                var assessment = await _assessmentService.AssessGrowthAsync(recordEntity);
                return Ok(assessment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đánh giá tăng trưởng cho trẻ {ChildId}", childId);
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu");
            }
        }
    }
}
