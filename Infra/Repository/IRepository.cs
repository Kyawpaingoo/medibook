using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Infra.Repository
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id);
        IQueryable<T> GetAll();
        IQueryable<T> GetWithoutTracking();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        // Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
        Task<T> AddAsync(T entity);
        Task<T> AddReturnAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        Task<List<T>> AddRangeReturnAsync(IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        Task<T> UpdateReturnAsync(T entity);
        Task UpdateRangeAsync(IEnumerable<T> entities);
        Task RemoveAsync(T entity);
        Task RemoveRangeAsync(IEnumerable<T> entities);
        Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? filter = null);
    }
}
