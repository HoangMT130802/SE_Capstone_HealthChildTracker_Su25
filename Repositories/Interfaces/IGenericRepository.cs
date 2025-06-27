using Microsoft.EntityFrameworkCore;
using Repositories.Models.QueryModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        // Get methods
        IQueryable<TEntity> GetAllQueryable(string includeProperties = "");
        Task<TEntity> GetByIdAsync(int id);
        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);       
        Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, string includeProperties = "");
        Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, string includeProperties = "");
        Task<IEnumerable<TEntity>> GetAllAsync(string includeProperties = "");
        Task<QueryResultModel<List<TEntity>>> GetAllAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            string include = "",
            int? pageIndex = null,
            int? pageSize = null
        );

        // Add methods
        Task AddAsync(TEntity entity);
        Task AddRangeAsync(List<TEntity> entities);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        
        // Update methods
        void Update(TEntity entity);
        void UpdateRange(List<TEntity> entities);
        
        // Delete methods
        void Delete(TEntity entity);
        void HardDelete(TEntity entity);
        void HardDeleteRange(List<TEntity> entities);
        Task DeleteAsync(TEntity entity);
    }
}
