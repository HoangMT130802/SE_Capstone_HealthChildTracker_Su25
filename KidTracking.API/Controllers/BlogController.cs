using Contracts.DTOs.Blog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly IBlogService _blogService;
        private readonly ILogger<BlogController> _logger;

        public BlogController(IBlogService blogService, ILogger<BlogController> logger)
        {
            _blogService = blogService ?? throw new ArgumentNullException(nameof(blogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateBlog([FromForm] CreateBlogDTO blogDto)
        {
            if (!IsAdmin())
            {
                return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
            }
            if (blogDto == null)
            {
                return BadRequest("Blog data is required");
            }

            try
            {
                var blog = await _blogService.CreateBlogAsync(blogDto);
                return Ok(blog);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating blog");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBlogById(int id)
        {
            try
            {
                var blog = await _blogService.GetBlogByIdAsync(id);
                return Ok(blog);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting blog {id}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBlogs([FromQuery] string status = null, [FromQuery] int? pageIndex = null, [FromQuery] int? pageSize = null)
        {
            try
            {
                var blogs = await _blogService.GetBlogsAsync(status, pageIndex, pageSize);
                return Ok(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting blogs with status {status}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateBlog(int id, [FromForm] UpdateBlogDTO blogDto)
        {
            if (!IsAdmin())
            {
                return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
            }
            if (blogDto == null)
            {
                return BadRequest("Blog data is required");
            }

            try
            {
                var blog = await _blogService.UpdateBlogAsync(id, blogDto);
                return Ok(blog);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating blog {id}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            try
            {
                if (!IsAdmin())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }
                await _blogService.DeleteBlogAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting blog {id}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
