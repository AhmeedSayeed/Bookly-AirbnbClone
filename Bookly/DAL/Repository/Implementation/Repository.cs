using DAL.Models.Common;
using DAL.Repository.Interfaces;
using DAL.Specification.Interfaces;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DAL.Repository.Implementation
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly AppDbContext _db;
        protected readonly DbSet<TEntity> _dbSet;
        public Repository(AppDbContext db)
        {
            _db = db;
            _dbSet = db.Set<TEntity>();
        }

        #region Retriving

        public async Task<TEntity?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public async Task<List<TEntity>> GetAllAsync()
            => await _dbSet.ToListAsync();
        public async Task<List<TEntity>> GetAllAsNoTrackedAsync()
            => await _dbSet.AsNoTracking().ToListAsync();

        public IQueryable<TEntity> GetAllAsIQueryable() => _dbSet;


        public virtual Task<TResult?> GetAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>> filter,
            params Expression<Func<TEntity, object>>[] Includes) where TResult : class
        {
            IQueryable<TEntity> query = _dbSet;

            query = query.Where(filter);

            foreach (var include in Includes)
                query = query.Include(include);

            return query.Select(selector).FirstOrDefaultAsync();
        }

        public async Task<List<TResult>> GetAllAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            params Expression<Func<TEntity, object>>[] Includes) where TResult : class
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
                query = query.Where(filter);

            if (Includes != null)
                foreach (var include in Includes)
                    query = query.Include(include);

            if (orderBy != null)
                query = orderBy(query);

            return await query.Select(selector).ToListAsync();
        }


        public async Task<PagedResult<TResult>> GetAllPaginatedAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector,
            int pageNumber = 1, int pageSize = 10,
            Expression<Func<TEntity, bool>>? filter = null,
            bool exbandable = false,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            params Expression<Func<TEntity, object>>[] Includes) where TResult : class
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 5) pageSize = 5;

            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                if (exbandable)
                    query = query.AsExpandableEFCore().Where(filter);
                query = query.Where(filter);
            }


            if (Includes != null)
                foreach (var include in Includes)
                    query = query.Include(include);

            if (orderBy != null)
                query = orderBy(query);

            var totalRecords = await query.CountAsync();

            query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            var result = query.Select(selector);

            return new PagedResult<TResult>
            {
                PageIndex = pageNumber,
                PageSize = pageSize,
                TotalCount = totalRecords,
                Items = await result.ToListAsync()
            };
        }

        public async Task<PagedResult<TResult>> GetAllPaginatedEnhancedAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector,
            int pageNumber = 1,
            int pageSize = 10,
            Expression<Func<TEntity, bool>>? filter = null,
            bool expandable = false,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null)
            where TResult : class
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 5) pageSize = 5;

            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                if (expandable)
                    query = query.AsExpandableEFCore().Where(filter);
                else
                    query = query.Where(filter);
            }

            if (include != null)
                query = include(query);

            if (orderBy != null)
                query = orderBy(query);

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            var result = query.Select(selector);

            return new PagedResult<TResult>
            {
                PageIndex = pageNumber,
                PageSize = pageSize,
                TotalCount = totalRecords,
                Items = await result.ToListAsync()
            };
        }



        public async Task<List<object>> GetBySpecificationAsync(ISpecification<TEntity> specification)
        {
            IQueryable<TEntity> query = _dbSet;

            if (specification.Criteria != null)
                query = query.Where(specification.Criteria);

            if (specification.OrderBy != null)
                query = query.OrderBy(specification.OrderBy);

            if (specification.OrderByDescending != null)
                query = query.OrderByDescending(specification.OrderByDescending);

            if (specification.Includes.Any())
            {
                foreach (var include in specification.Includes)
                    query = query.Include(include);
            }

            return await query.Select(specification.Selector).ToListAsync();
        }

        #endregion

        #region Add_Update_Delete
        public async Task AddAsync(TEntity entity)
            => await _dbSet.AddAsync(entity);
        public void Update(TEntity entity)
            => _dbSet.Update(entity);

        public void Delete(int id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
            {
                if (entity is ISoftDeletable deletableEntity)
                {
                    deletableEntity.IsDeleted = true;
                    deletableEntity.DeletedAt = DateTime.Now;
                    _dbSet.Update(entity);
                }
                else
                {
                    _dbSet.Remove(entity);
                }
            }
        }
        public async Task BulkDeleteAsync(Expression<Func<TEntity, bool>> filter)
            => await _dbSet.Where(filter).ExecuteDeleteAsync();

        #endregion

        #region Count
        public async Task<int> Count()
            => await _dbSet.CountAsync();

        public async Task<int> Count(Expression<Func<TEntity, bool>> filter)
            => await _dbSet.Where(filter).CountAsync();
        #endregion

        #region Existance
        public async Task<bool> IsExistAsync(int id)
            => await _dbSet.FindAsync(id) == null ? false : true;

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? filter = null)
        {
            if (filter != null)
                return await _dbSet.AnyAsync(filter);
            return await _dbSet.AnyAsync();
        }

        public async Task<bool> AllAsync(Expression<Func<TEntity, bool>> filter)
            => await _dbSet.AllAsync(filter);

        #endregion


        public async Task<int> SaveAsync() => await _db.SaveChangesAsync();


    }
}
