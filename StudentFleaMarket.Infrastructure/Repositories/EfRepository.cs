using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Application.Common.Interfaces;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Infrastructure.Persistence;

namespace StudentFleaMarket.Infrastructure.Repositories;

public class EfRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext DbContext;
    protected readonly DbSet<T> DbSet;

    public EfRepository(AppDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FindAsync(new object[] { id }, ct);

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await DbSet.AnyAsync(predicate, ct);

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(predicate, ct);

    public virtual async Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null
            ? await DbSet.ToListAsync(ct)
            : await DbSet.Where(predicate).ToListAsync(ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await DbSet.AddAsync(entity, ct);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        => await DbSet.AddRangeAsync(entities, ct);

    public virtual void Update(T entity) => DbSet.Update(entity);

    public virtual void Remove(T entity) => DbSet.Remove(entity);

    public virtual async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await DbContext.SaveChangesAsync(ct);
}

public class UserRepository : EfRepository<ApplicationUser>, IUserRepository
{
    public UserRepository(AppDbContext dbContext) : base(dbContext) { }

    public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default)
        => DbSet.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<ApplicationUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => DbSet.FirstOrDefaultAsync(u => u.Username != null && u.Username.ToLower() == username.ToLower(), ct);

    public Task<ApplicationUser?> GetByUsernameOrEmailAsync(string identifier, CancellationToken ct = default)
    {
        var key = identifier.Trim();

        // Nếu chứa '@' thì coi là email, ngược lại coi là username.
        return key.Contains('@')
            ? GetByEmailAsync(key.ToLowerInvariant(), ct)
            : GetByUsernameAsync(key, ct);
    }
}

public class ServiceAccountRepository : EfRepository<ServiceAccount>, IServiceAccountRepository
{
    public ServiceAccountRepository(AppDbContext dbContext) : base(dbContext) { }

    public Task<ServiceAccount?> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken ct = default)
        => DbSet.FirstOrDefaultAsync(s => s.ApiKeyHash == apiKeyHash, ct);
}
