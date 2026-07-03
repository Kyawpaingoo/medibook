using Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Infra.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly BookingDBContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(BookingDBContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual IQueryable<T> GetAll()
        {
            return _dbSet.AsQueryable();
        }
        public IQueryable<T> GetWithoutTracking()
        {
            return _dbSet.AsQueryable().AsNoTracking();
        }
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        //public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        //{
        //    return await _dbSet.FirstOrDefaultAsync(predicate);
        //}

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();
            return await _dbSet.CountAsync(predicate);
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            var entry = await _dbSet.AddAsync(entity);
            return entry.Entity;
        }
        public virtual async Task<T> AddReturnAsync(T entity)
        {
            var entry = await _dbSet.AddAsync(entity);
            return entry.Entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public virtual async Task<List<T>> AddRangeReturnAsync(IEnumerable<T> entities)
        {
            var entityList = entities.ToList();
            await _dbSet.AddRangeAsync(entityList);
            return entityList;
        }

        public virtual Task UpdateAsync(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        public virtual Task<T> UpdateReturnAsync(T entity)
        {
            var entry = _dbSet.Update(entity);
            return Task.FromResult(entry.Entity);
        }

        public virtual Task UpdateRangeAsync(IEnumerable<T> entities)
        {

            _dbSet.UpdateRange(entities);
            return Task.CompletedTask;
        }

        public virtual Task RemoveAsync(T entity)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public virtual Task RemoveRangeAsync(IEnumerable<T> entities)
        {
            ArgumentNullException.ThrowIfNull(entities);

            _dbSet.RemoveRange(entities);
            return Task.CompletedTask;
        }

        public virtual async Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
                query = query.Where(filter);

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
