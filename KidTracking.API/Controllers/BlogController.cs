using BusinessLogic.DTOs.Blog;
using BusinessLogic.Services.Interfaces;
using DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HealthChildTracker_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    
    public class BlogController : ControllerBase
    {
        private readonly IBlogService _blogService;
        private readonly ILogger<BlogController> _logger;

        public BlogController(IBlogService blogService, ILogger<BlogController> logger)
        {
            _blogService = blogService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Không thể xác thực người dùng");
            }
            return userId;
        }

        private string GetUserRole()
        {
            var roleClaim = User?.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleClaim))
            {
                throw new UnauthorizedAccessException("Không thể xác định vai trò người dùng");
            }
            return roleClaim;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<BlogDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<BlogDTO>>> GetAllBlogs()
        {
            try
            {
                var blogs = await _blogService.GetAllBlogsAsync();
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách blog");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách blog" });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BlogDTO), StatusCodes.Status200OK)]
        public async Task<ActionResult<BlogDTO>> GetBlogById(int id)
        {
            try
            {
                var blog = await _blogService.GetBlogByIdAsync(id);
                if (blog == null)
                {
                    return NotFound(new { message = $"Không tìm thấy blog với ID {id}" });
                }
                return Ok(blog);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Không tìm thấy blog với ID {Id}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin blog {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy thông tin blog" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(typeof(BlogDTO), StatusCodes.Status201Created)]
        public async Task<ActionResult<BlogDTO>> CreateBlog([FromBody] CreateBlogDTO blogDTO)
        {
            try
            {
                var userId = GetCurrentUserId();
                var createdBlog = await _blogService.CreateBlogAsync(userId, blogDTO);
                return CreatedAtAction(nameof(GetBlogById), new { id = createdBlog.BlogId }, createdBlog);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Người dùng không có quyền tạo blog");
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Dữ liệu không hợp lệ khi tạo blog");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo blog mới");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo blog" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(typeof(BlogDTO), StatusCodes.Status200OK)]
        public async Task<ActionResult<BlogDTO>> UpdateBlog(int id, [FromBody] UpdateBlogDTO blogDTO)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetUserRole();
                var updatedBlog = await _blogService.UpdateBlogAsync(id, userId, userRole, blogDTO);
                return Ok(updatedBlog);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Người dùng không có quyền cập nhật blog");
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Không tìm thấy blog với ID {Id}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật blog {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật blog" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetUserRole();
                await _blogService.DeleteBlogAsync(id, userId, userRole);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Người dùng không có quyền xóa blog");
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Không tìm thấy blog với ID {Id}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa blog {Id}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xóa blog" });
            }
        }
    }
}