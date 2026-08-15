namespace RenewZone.Domain.Models.Admin;

/// <summary>Bộ lọc màn danh sách khách hàng.</summary>
public class CustomerListRequest : AdminListRequest
{
    /// <summary>null = tất cả · true = chỉ tài khoản đang bị khóa · false = chỉ tài khoản bình thường.</summary>
    public bool? IsLocked { get; init; }
}

/// <summary>
/// Một dòng trong bảng khách hàng. Số đơn + tổng chi tiêu tính sẵn ở SQL để bảng 20 dòng không
/// sinh 40 truy vấn con (N+1).
/// </summary>
public record CustomerListItem(
    string Id,
    string Phone,
    string? FullName,
    string? Email,
    int OrderCount,
    decimal TotalSpent,
    bool IsLocked,
    DateTime? LockoutEnd,
    DateTime CreatedAt);

/// <summary>Hồ sơ đầy đủ một khách hàng + dữ liệu liên quan, cho màn chi tiết.</summary>
public record CustomerDetail(
    string Id,
    string Phone,
    string? FullName,
    string? Email,
    DateTime? DateOfBirth,
    string? Gender,
    bool IsLocked,
    DateTime? LockoutEnd,
    int AccessFailedCount,
    DateTime CreatedAt,
    int OrderCount,
    int CompletedOrders,
    decimal TotalSpent,
    int ReviewCount,
    int WishlistCount,
    IReadOnlyList<OrderListItem> RecentOrders,
    IReadOnlyList<CustomerAddressView> Addresses);

/// <summary>
/// Địa chỉ của khách nhìn từ phía admin — chỉ ĐỌC. Admin không sửa sổ địa chỉ hộ khách:
/// đó là dữ liệu riêng, sửa hộ thì khách đặt hàng sai địa chỉ mà không hiểu vì sao.
/// </summary>
public record CustomerAddressView(
    string? Label,
    string ReceiverName,
    string Phone,
    string AddressLine,
    bool IsDefault);

/// <summary>
/// Kết quả thao tác quản trị trên một tài khoản.
/// <paramref name="TempPassword"/> chỉ có giá trị ở thao tác đặt lại mật khẩu và CHỈ được trả về
/// đúng một lần (không lưu ở đâu cả) — admin đọc cho khách qua điện thoại rồi thôi.
/// </summary>
public record CustomerOpResult(bool Ok, string Message, string? TempPassword = null)
{
    public static CustomerOpResult Success(string message, string? tempPassword = null) =>
        new(true, message, tempPassword);

    public static CustomerOpResult Fail(string message) => new(false, message);
}
