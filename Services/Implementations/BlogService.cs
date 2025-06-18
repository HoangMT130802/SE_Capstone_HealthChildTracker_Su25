using AutoMapper;
using BusinessLogic.DTOs.Blog;
using BusinessLogic.Services.Interfaces;
using DataAccess.Entities;
using DataAccess.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementations
{
    public class BlogService : IBlogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<BlogService> _logger;

        public BlogService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<BlogService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BlogDTO> CreateBlogAsync(int authorId, CreateBlogDTO blogDto)
        {
            try
            {
                // Kiểm tra quyền của user
                var userRepo = _unitOfWork.GetRepository<User>();
                var user = await userRepo.GetAsync(u => u.UserId == authorId);

                if (user == null || (user.Role != "Admin" && user.Role != "Doctor"))
                    throw new UnauthorizedAccessException("Chỉ Admin và Doctor mới có quyền tạo blog");

                var blog = _mapper.Map<Blog>(blogDto);
                blog.AuthorId = authorId;

                var blogRepo = _unitOfWork.GetRepository<Blog>();
                await blogRepo.AddAsync(blog);
                await _unitOfWork.SaveChangesAsync();

                return await GetBlogByIdAsync(blog.BlogId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo blog");
                throw;
            }
        }

        public async Task<BlogDTO> UpdateBlogAsync(int blogId, int userId, string userRole, UpdateBlogDTO blogDto)
        {
            try
            {
                var blogRepo = _unitOfWork.GetRepository<Blog>();
                var blog = await blogRepo.GetAsync(b => b.BlogId == blogId, includeProperties: "Author");

                if (blog == null)
                    throw new KeyNotFoundException("Không tìm thấy blog");

                
                if (userRole != "Admin" && (userRole != "Doctor" || blog.AuthorId != userId))
                    throw new UnauthorizedAccessException("Bạn không có quyền cập nhật blog này");

                _mapper.Map(blogDto, blog);
                blogRepo.Update(blog);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<BlogDTO>(blog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật blog {BlogId}", blogId);
                throw;
            }
        }

        public async Task DeleteBlogAsync(int blogId, int userId, string userRole)
        {
            try
            {
                var blogRepo = _unitOfWork.GetRepository<Blog>();
                var blog = await blogRepo.GetAsync(b => b.BlogId == blogId);

                if (blog == null)
                    throw new KeyNotFoundException("Không tìm thấy blog");

                // Admin có thể xóa tất cả, Doctor chỉ xóa được blog của mình
                if (userRole == "Admin" || (userRole == "Doctor" && blog.AuthorId == userId))
                {
                    blogRepo.Delete(blog);
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền xóa blog này");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa blog {BlogId}", blogId);
                throw;
            }
        }

        public async Task<BlogDTO> GetBlogByIdAsync(int blogId)
        {
            try
            {
                var blogRepo = _unitOfWork.GetRepository<Blog>();
                var blog = await blogRepo.GetAsync(
                    b => b.BlogId == blogId,
                    includeProperties: "Author"
                );

                if (blog == null)
                    throw new KeyNotFoundException("Không tìm thấy blog");

                return _mapper.Map<BlogDTO>(blog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin blog {BlogId}", blogId);
                throw;
            }
        }

        public async Task<IEnumerable<BlogDTO>> GetAllBlogsAsync()
        {
            try
            {
                var blogRepo = _unitOfWork.GetRepository<Blog>();
                var blogs = await blogRepo.GetAllAsync(                
                    includeProperties: "Author"
                );

                return _mapper.Map<IEnumerable<BlogDTO>>(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách blog");
                throw;
            }
        }

        public async Task<IEnumerable<BlogDTO>> GetBlogsByAuthorIdAsync(int authorId)
        {
            try
            {
                var blogRepo = _unitOfWork.GetRepository<Blog>();
                var blogs = await blogRepo.FindAsync(
                    b => b.AuthorId == authorId,
                    includeProperties: "Author"
                );

                return _mapper.Map<IEnumerable<BlogDTO>>(blogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách blog của tác giả {AuthorId}", authorId);
                throw;
            }
        }
    }
}
