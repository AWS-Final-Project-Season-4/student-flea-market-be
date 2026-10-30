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

    /// <summary>
    /// Lấy danh sách tài khoản người dùng (không tính Admin) kèm trạng thái
    /// quyền bán, phân trang ở tầng DB. Dùng cho màn hình Admin duyệt người bán.
    /// </summary>
    /// <param name="status">
    /// null = lấy tất cả; nếu có giá trị thì lọc theo SellerStatus tương ứng.
    /// </param>
    Task<(List<ApplicationUser> Users, int TotalCount)> ListSellerRequestsAsync(
        int? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách tài khoản cho màn hình quản lý người dùng của Admin,
    /// phân trang ở tầng DB. Hỗ trợ lọc theo trạng thái tài khoản và tìm kiếm.
    /// </summary>
    /// <param name="status">
    /// null = lấy tất cả; nếu có giá trị thì lọc theo UserStatus tương ứng.
    /// </param>
    /// <param name="keyword">
    /// Tìm theo họ tên / email / username (không phân biệt hoa thường).
    /// </param>
    Task<(List<ApplicationUser> Users, int TotalCount)> ListForAdminAsync(
        int? status,
        string? keyword,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy nhanh trạng thái tài khoản (không load toàn bộ entity).
    /// Dùng bởi middleware để chặn tài khoản bị cấm ngay khi còn access token hợp lệ.
    /// Trả về null nếu không tìm thấy tài khoản.
    /// </summary>
    Task<(int Status, string? BanReason)?> GetStatusAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Lấy nhanh vai trò của tài khoản từ DB (không load toàn bộ entity).
    /// Dùng bởi RoleSyncMiddleware để việc cấp / thu hồi quyền Admin
    /// có hiệu lực tức thì mà không cần đăng nhập lại.
    /// Trả về null nếu không tìm thấy tài khoản.
    /// </summary>
    Task<int?> GetRoleAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Repository riêng cho ServiceAccount (AI Service).
/// </summary>
public interface IServiceAccountRepository : IRepository<ServiceAccount>
{
    Task<ServiceAccount?> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken ct = default);
}
