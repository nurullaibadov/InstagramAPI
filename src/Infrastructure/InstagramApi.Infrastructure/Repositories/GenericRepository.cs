using System.Linq.Expressions;
using InstagramApi.Application.Interfaces.Repositories;
using InstagramApi.Domain.Common;
using InstagramApi.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace InstagramApi.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FindAsync(new object[] { id }, ct);

    public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, ct);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.Where(predicate).ToListAsync(ct);

    public Task<IQueryable<T>> GetQueryableAsync()
        => Task.FromResult(_dbSet.AsQueryable());

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        var list = entities.ToList();
        await _dbSet.AddRangeAsync(list, ct);
        return list;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(T entity, CancellationToken ct = default)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.AnyAsync(predicate, ct);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate == null
            ? await _dbSet.CountAsync(ct)
            : await _dbSet.CountAsync(predicate, ct);

    public async Task<PagedResult<T>> GetPagedAsync(int page, int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken ct = default)
    {
        IQueryable<T> query = _dbSet;

        if (predicate != null) query = query.Where(predicate);

        int totalCount = await query.CountAsync(ct);

        if (orderBy != null) query = orderBy(query);
        else query = query.OrderByDescending(x => x.CreatedAt);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
