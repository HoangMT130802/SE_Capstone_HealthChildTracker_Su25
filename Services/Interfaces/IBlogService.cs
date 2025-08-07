using Contracts.DTOs.Blog;
using Repositories.Models.QueryModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IBlogService
    {
        Task<BlogDTO> CreateBlogAsync(CreateBlogDTO blogDto);
        Task<BlogDTO> GetBlogByIdAsync(int blogId);
        Task<QueryResultModel<IEnumerable<BlogDTO>>> GetBlogsAsync(string status = null, int? pageIndex = null, int? pageSize = null);
        Task<BlogDTO> UpdateBlogAsync(int blogId, UpdateBlogDTO blogDto);
        Task DeleteBlogAsync(int blogId);
    }
}
