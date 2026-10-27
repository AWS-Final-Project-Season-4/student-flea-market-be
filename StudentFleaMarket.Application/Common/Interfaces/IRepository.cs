namespace StudentFleaMarket.Application.Common.Interfaces;

using System.Linq.Expressions;
using StudentFleaMarket.Domain.Entities;

/// <summary>
/// Truy cập dữ liệu tổng quát cho Application layer.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    Task AddAsync(T entity, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    void Update(T entity);

    void Remove(T entity);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Repository riêng cho ApplicationUser (cần các truy vấn đặc biệt).
/// </summary>
public interface IUserRepository : IRepository<ApplicationUser>
{
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Tìm user theo username (không phân biệt hoa thường).
    /// </summary>
    Task<ApplicationUser?> GetByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// Tìm user theo username HOẶC email — dùng cho màn hình đăng nhập
    /// (người dùng nhập "admin" hoặc "admin@sv.edu.vn" đều được).
    /// </summary>
    Task<ApplicationUser?> GetByUsernameOrEmailAsync(string identifier, CancellationToken ct = default);
}

/// <summary>
/// Repository riêng cho ServiceAccount (AI Service).
/// </summary>
public interface IServiceAccountRepository : IRepository<ServiceAccount>
{
    Task<ServiceAccount?> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken ct = default);
}
