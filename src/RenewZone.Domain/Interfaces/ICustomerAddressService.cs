using RenewZone.Domain.Entities;
using RenewZone.Domain.Models.Account;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Sổ địa chỉ nhận hàng của khách (Giai đoạn 9). Mọi phương thức nhận <c>userId</c> và tự lọc —
/// xem ghi chú chống IDOR ở <see cref="ICustomerOrderService"/>.
/// </summary>
public interface ICustomerAddressService
{
    Task<IReadOnlyList<CustomerAddress>> GetMyAddressesAsync(string userId);

    Task<CustomerAddress?> GetAsync(string userId, int id);

    /// <summary>Địa chỉ chọn sẵn ở trang đặt hàng (mặc định, hoặc mới nhất nếu chưa đặt mặc định).</summary>
    Task<CustomerAddress?> GetDefaultAsync(string userId);

    /// <summary>Thêm mới khi <c>input.Id == 0</c>, ngược lại sửa. Tự bảo đảm chỉ có 1 địa chỉ mặc định.</summary>
    Task<AccountOpResult> SaveAsync(string userId, AddressInput input);

    Task<AccountOpResult> DeleteAsync(string userId, int id);

    Task<AccountOpResult> SetDefaultAsync(string userId, int id);
}
