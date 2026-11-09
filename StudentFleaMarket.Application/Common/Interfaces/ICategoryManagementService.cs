using StudentFleaMarket.Application.Common;
using StudentFleaMarket.Application.DTOs.Admin;

namespace StudentFleaMarket.Application.Common.Interfaces;

/// <summary>
/// QUẢN LÝ DANH MỤC SẢN PHẨM — phía Admin & Moderator (Feature 5 - Admin Dashboard).
///
/// 4 nhóm thao tác:
///   1. Xem danh sách (kèm trạng thái mở/đóng và số tin đăng).
///   2. Tạo / sửa / xoá danh mục.
///   3. Bật / tắt danh mục (đóng mở sàn theo danh mục).
///   4. Sắp xếp lại thứ tự hiển thị.
///
/// Tầng quyền: xem <c>StaffPermission.CanManageCategories</c>.
/// </summary>
public interface ICategoryManagementService
{
    /// <summary>
    /// Danh sách danh mục cho bảng quản lý (kèm IsActive và số tin đăng).
    /// </summary>
    Task<List<AdminCategoryDto>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Tạo danh mục mới.
    /// </summary>
    Task<AdminCategoryDto> CreateAsync(
        Guid staffUserId,
        CreateCategoryDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Cập nhật danh mục (tên / mô tả / thứ tự).
    /// </summary>
    Task<AdminCategoryDto> UpdateAsync(
        Guid staffUserId,
        int categoryId,
        UpdateCategoryDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Bật / tắt danh mục.
    ///
    /// Khi ĐÓNG: người dùng không đăng được tin mới vào danh mục đó, và danh
    /// mục không hiển thị trong danh sách công khai. Tin đăng CŨ vẫn hiển thị
    /// bình thường (đóng danh mục không có nghĩa xoá tin của người bán).
    /// </summary>
    Task<AdminCategoryDto> ToggleAsync(
        Guid staffUserId,
        int categoryId,
        ToggleCategoryDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Xoá danh mục.
    ///
    /// KHÔNG xoá được nếu danh mục còn tin đăng — vì tin đăng có FK bắt buộc
    /// tới danh mục. Khi đó phải chuyển tin sang danh mục khác trước, hoặc
    /// đóng danh mục lại. Đây là hành vi an toàn: không để mất dữ liệu tin
    /// đăng chỉ vì một thao tác quản trị.
    /// </summary>
    Task<ApiResponse<object>> DeleteAsync(
        Guid staffUserId,
        int categoryId,
        CancellationToken ct = default);

    /// <summary>
    /// Sắp xếp lại thứ tự hiển thị của nhiều danh mục.
    /// </summary>
    Task<List<AdminCategoryDto>> ReorderAsync(
        Guid staffUserId,
        ReorderCategoriesDto request,
        CancellationToken ct = default);
}
