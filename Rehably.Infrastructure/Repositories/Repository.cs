using Microsoft.EntityFrameworkCore;
using Rehably.Application.Repositories;
using Rehably.Infrastructure.Data;
using System.Linq.Expressions;

namespace Rehably.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        var filter = BuildPrimaryKeyFilter(id);
        return filter != null
            ? await _dbSet.FirstOrDefaultAsync(filter)
            : await _dbSet.FindAsync(id);
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, ct);
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet.ToListAsync(ct);
    }

    public async Task<IEnumerable<T>> GetPagedAsync(int skip, int take)
    {
        return await _dbSet.Skip(skip).Take(take).ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(ct);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        if (predicate == null)
            return await _dbSet.CountAsync(ct);
        return await _dbSet.CountAsync(predicate, ct);
    }

    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        return entities;
    }

    public Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        if (predicate == null)
            return await _dbSet.AnyAsync(ct);
        return await _dbSet.AnyAsync(predicate, ct);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        var filter = BuildPrimaryKeyFilter(id);
        return filter != null
            ? await _dbSet.AnyAsync(filter)
            : await _dbSet.FindAsync(id) != null;
    }

    public IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Builds a primary key filter expression dynamically from EF Core model metadata.
    /// This avoids using FindAsync which bypasses global query filters (tenant isolation, soft delete).
    /// </summary>
    private Expression<Func<T, bool>>? BuildPrimaryKeyFilter(Guid id)
    {
        var entityType = _context.Model.FindEntityType(typeof(T));
        var primaryKey = entityType?.FindPrimaryKey();
        if (primaryKey == null || primaryKey.Properties.Count != 1)
            return null;

        var pkProperty = primaryKey.Properties[0];
        if (pkProperty.ClrType != typeof(Guid))
            return null;

        var parameter = Expression.Parameter(typeof(T), "e");
        var property = Expression.Property(parameter, pkProperty.PropertyInfo!);
        var constant = Expression.Constant(id);
        var equals = Expression.Equal(property, constant);
        return Expression.Lambda<Func<T, bool>>(equals, parameter);
    }
}
