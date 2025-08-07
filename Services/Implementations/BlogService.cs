using AutoMapper;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Contracts.DTOs.Blog;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Repositories.Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class BlogService : IBlogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<BlogService> _logger;
        private readonly Cloudinary _cloudinary;

        public BlogService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<BlogService> logger, IOptions<CloudinarySettings> cloudinaryConfig)
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

            using var stream = image.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(image.FileName, stream),
                Folder = "blog_images"
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.AbsoluteUri;
        }

        public async Task<BlogDTO> CreateBlogAsync(CreateBlogDTO blogDto)
        {
            try
            {
                _logger.LogInformation($"Creating blog with Title: {blogDto.Title}");

                var blog = _mapper.Map<Blog>(blogDto);
                if (blogDto.Image != null)
                {
                    blog.Image = await UploadImageToCloudinary(blogDto.Image);
                }

                var blogRepository = _unitOfWork.GetRepository<Blog>();
                await blogRepository.AddAsync(blog);
                await _unitOfWork.SaveChangesAsync();

                var savedBlog = await blogRepository.GetAsync(b => b.BlogId == blog.BlogId);
                return _mapper.Map<BlogDTO>(savedBlog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating blog with Title: {blogDto?.Title}");
                throw;
            }
        }

        public async Task<BlogDTO> GetBlogByIdAsync(int blogId)
        {
            try
            {
                _logger.LogInformation($"Retrieving blog with ID: {blogId}");
                var blogRepository = _unitOfWork.GetRepository<Blog>();
                var blog = await blogRepository.GetAsync(b => b.BlogId == blogId);
                if (blog == null)
                {
                    throw new KeyNotFoundException($"Blog with ID {blogId} not found");
                }
                return _mapper.Map<BlogDTO>(blog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving blog with ID {blogId}");
                throw;
            }
        }

        public async Task<QueryResultModel<IEnumerable<BlogDTO>>> GetBlogsAsync(string status = null, int? pageIndex = null, int? pageSize = null)
        {
            try
            {
                _logger.LogInformation($"Retrieving blogs with status: {status ?? "all"}");
                var blogRepository = _unitOfWork.GetRepository<Blog>();
                Expression<Func<Blog, bool>>? filter = null;
                if (!string.IsNullOrEmpty(status))
                {
                    filter = b => b.Status == status;
                }

                var result = await blogRepository.GetAllAsync(
                    filter: filter,
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var blogDtos = _mapper.Map<IEnumerable<BlogDTO>>(result.Data);
                return new QueryResultModel<IEnumerable<BlogDTO>>
                {
                    TotalCount = result.TotalCount,
                    Data = blogDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving blogs with status {status}");
                throw;
            }
        }

        public async Task<BlogDTO> UpdateBlogAsync(int blogId, UpdateBlogDTO blogDto)
        {
            try
            {
                _logger.LogInformation($"Updating blog with ID: {blogId}");
                var blogRepository = _unitOfWork.GetRepository<Blog>();
                var blog = await blogRepository.GetAsync(b => b.BlogId == blogId);
                if (blog == null)
                {
                    throw new KeyNotFoundException($"Blog with ID {blogId} not found");
                }

                _mapper.Map(blogDto, blog);
                if (blogDto.Image != null)
                {
                    if (!string.IsNullOrEmpty(blog.Image))
                    {
                        var publicId = blog.Image.Split('/').Last().Split('.').First();
                        await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                    }
                    blog.Image = await UploadImageToCloudinary(blogDto.Image);
                }

                blog.UpdatedAt = DateTime.UtcNow;
                blogRepository.Update(blog);
                await _unitOfWork.SaveChangesAsync();

                var updatedBlog = await blogRepository.GetAsync(b => b.BlogId == blogId);
                return _mapper.Map<BlogDTO>(updatedBlog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating blog with ID {blogId}");
                throw;
            }
        }

        public async Task DeleteBlogAsync(int blogId)
        {
            try
            {
                _logger.LogInformation($"Deleting blog with ID: {blogId}");
                var blogRepository = _unitOfWork.GetRepository<Blog>();
                var blog = await blogRepository.GetAsync(b => b.BlogId == blogId);
                if (blog == null)
                {
                    throw new KeyNotFoundException($"Blog with ID {blogId} not found");
                }

                if (!string.IsNullOrEmpty(blog.Image))
                {
                    var publicId = blog.Image.Split('/').Last().Split('.').First();
                    await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                }

                blogRepository.Delete(blog);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting blog with ID {blogId}");
                throw;
            }
        }
    }
}
