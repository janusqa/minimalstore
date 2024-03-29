using System.Linq.Expressions;
using Microsoft.Data.Sqlite;

namespace MinimalVilla.Data.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetAsync(Expression<Func<T, bool>> predicate, bool tracked = false);
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? predicate, bool tracked = false);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        Task RemoveAsync(T entity);
        Task RemoveRangeAsync(IEnumerable<T> entities);

        Task<IEnumerable<T>> FromSqlAsync(string sql, List<SqliteParameter> sqlParameters, bool tracked = false);
        Task<int> ExecuteSqlAsync(string sql, List<SqliteParameter> sqlParameters);
        Task<IEnumerable<U>> SqlQueryAsync<U>(string sql, List<SqliteParameter> sqlParameters);
    }
}