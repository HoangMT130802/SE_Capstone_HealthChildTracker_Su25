using Microsoft.EntityFrameworkCore;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly HealthChildTrackerContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(HealthChildTrackerContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<TEntity>();
        }

        #region Helper Methods
        private IQueryable<TEntity> IncludeProperties(IQueryable<TEntity> query, string includeProperties)
        {
            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }
            return query;
        }
        #endregion

        #region Query Methods
        public virtual IQueryable<TEntity> GetAllQueryable(string includeProperties = "")
        {
            IQueryable<TEntity> query = _dbSet;
            return IncludeProperties(query, includeProperties);
        }

        public virtual async Task<TEntity> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            try
            {
                IQueryable<TEntity> query = _dbSet;
                if (predicate != null)
                {
                    query = query.Where(predicate);
                }
                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't retrieve count of entities: {ex.Message}");
            }
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public virtual async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, string includeProperties = "")
        {
            IQueryable<TEntity> query = _dbSet;
            query = IncludeProperties(query, includeProperties);
            return await query.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, string includeProperties = "")
        {
            IQueryable<TEntity> query = _dbSet;
            query = IncludeProperties(query, includeProperties);
            return await query.Where(predicate).ToListAsync();
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(string includeProperties = "")
        {
            IQueryable<TEntity> query = _dbSet;
            query = IncludeProperties(query, includeProperties);
            return await query.ToListAsync();
        }

        public virtual async Task<QueryResultModel<List<TEntity>>> GetAllAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            string include = "",
            int? pageIndex = null,
            int? pageSize = null)
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            int totalCount = await query.CountAsync();
            query = IncludeProperties(query, include);

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            if (pageIndex.HasValue && pageSize.HasValue)
            {
                int validPageIndex = pageIndex.Value > 0 ? pageIndex.Value - 1 : 0;
                int validPageSize = pageSize.Value > 0 ? pageSize.Value : 10;

                query = query.Skip(validPageIndex * validPageSize).Take(validPageSize);
            }

            return new QueryResultModel<List<TEntity>>()
            {
                TotalCount = totalCount,
                Data = await query.ToListAsync(),
            };
        }
        #endregion

        #region Add Methods
        public virtual async Task AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public virtual async Task AddRangeAsync(List<TEntity> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }
        #endregion

        #region Update Methods
        public virtual void Update(TEntity entity)
        {
            _dbSet.Update(entity);
        }
        public async Task DeleteAsync(TEntity entity)
        {
            _dbSet.Remove(entity);
            await Task.CompletedTask; 
        }
        public virtual void UpdateRange(List<TEntity> entities)
        {
            _dbSet.UpdateRange(entities);
        }
        #endregion

        #region Delete Methods
        public virtual void Delete(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual void HardDelete(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual void HardDeleteRange(List<TEntity> entities)
        {
            _dbSet.RemoveRange(entities);
        }
        #endregion
    }
}
