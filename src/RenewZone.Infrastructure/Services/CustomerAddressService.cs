using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Account;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <inheritdoc cref="ICustomerAddressService" />
public class CustomerAddressService(AppDbContext db) : ICustomerAddressService
{
    /// <summary>Trần số địa chỉ mỗi tài khoản — sổ địa chỉ là tiện ích, không phải kho dữ liệu.</summary>
    private const int MaxAddressesPerUser = 10;

    public async Task<IReadOnlyList<CustomerAddress>> GetMyAddressesAsync(string userId) =>
        await db.CustomerAddresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault) // mặc định luôn đứng đầu
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

    public Task<CustomerAddress?> GetAsync(string userId, int id) =>
        db.CustomerAddresses.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

    public Task<CustomerAddress?> GetDefaultAsync(string userId) =>
        db.CustomerAddresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<AccountOpResult> SaveAsync(string userId, AddressInput input)
    {
        var receiver = (input.ReceiverName ?? "").Trim();
        var phone = (input.Phone ?? "").Trim();
        var line = (input.AddressLine ?? "").Trim();

        // Chốt chặn ở SERVER, không dựa vào DataAnnotations của ViewModel tầng Web
        if (receiver.Length is < 2 or > 150)
        {
            return AccountOpResult.Fail("Tên người nhận cần từ 2 đến 150 ký tự.");
        }
        if (line.Length is < 5 or > 500)
        {
            return AccountOpResult.Fail("Địa chỉ cần từ 5 đến 500 ký tự.");
        }

        CustomerAddress address;

        if (input.Id > 0)
        {
            var existing = await db.CustomerAddresses
                .FirstOrDefaultAsync(a => a.Id == input.Id && a.UserId == userId);

            if (existing is null)
            {
                return AccountOpResult.Fail("Không tìm thấy địa chỉ này trong sổ địa chỉ của bạn.");
            }

            address = existing;
        }
        else
        {
            if (await db.CustomerAddresses.CountAsync(a => a.UserId == userId) >= MaxAddressesPerUser)
            {
                return AccountOpResult.Fail(
                    $"Bạn chỉ lưu được tối đa {MaxAddressesPerUser} địa chỉ. Hãy xóa bớt địa chỉ cũ.");
            }

            address = new CustomerAddress { UserId = userId };
            db.CustomerAddresses.Add(address);
        }

        address.Label = string.IsNullOrWhiteSpace(input.Label) ? null : input.Label.Trim();
        address.ReceiverName = receiver;
        address.Phone = phone;
        address.AddressLine = line;
        address.IsDefault = input.IsDefault;

        await EnsureSingleDefaultAsync(userId, address);

        await db.SaveChangesAsync();
        return AccountOpResult.Success(input.Id > 0 ? "Đã cập nhật địa chỉ." : "Đã thêm địa chỉ mới.");
    }

    public async Task<AccountOpResult> DeleteAsync(string userId, int id)
    {
        var address = await db.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address is null)
        {
            return AccountOpResult.Fail("Không tìm thấy địa chỉ này trong sổ địa chỉ của bạn.");
        }

        var wasDefault = address.IsDefault;
        db.CustomerAddresses.Remove(address); // CustomerAddress KHÔNG xóa mềm ⇒ DELETE thật

        await db.SaveChangesAsync();

        // Xóa mất địa chỉ mặc định ⇒ chỉ định lại, nếu không trang đặt hàng sẽ không chọn sẵn gì cả
        if (wasDefault)
        {
            var next = await db.CustomerAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (next is not null)
            {
                next.IsDefault = true;
                await db.SaveChangesAsync();
            }
        }

        return AccountOpResult.Success("Đã xóa địa chỉ.");
    }

    public async Task<AccountOpResult> SetDefaultAsync(string userId, int id)
    {
        var address = await db.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address is null)
        {
            return AccountOpResult.Fail("Không tìm thấy địa chỉ này trong sổ địa chỉ của bạn.");
        }

        address.IsDefault = true;
        await EnsureSingleDefaultAsync(userId, address);
        await db.SaveChangesAsync();

        return AccountOpResult.Success("Đã đặt làm địa chỉ mặc định.");
    }

    /// <summary>
    /// Mỗi tài khoản luôn có ĐÚNG một địa chỉ mặc định — cùng triết lý <c>EnsureSingleDefault</c>
    /// của biến thể sản phẩm (CLAUDE.md mục 4.9): nếu để lọt hai cái, trang đặt hàng sẽ chọn sẵn
    /// địa chỉ nào tùy vào thứ tự sắp xếp của DB, tức là ngẫu nhiên với người dùng.
    /// </summary>
    private async Task EnsureSingleDefaultAsync(string userId, CustomerAddress current)
    {
        if (current.IsDefault)
        {
            // Hạ cờ mọi địa chỉ khác. Dùng change tracker (không ExecuteUpdate) để nằm CÙNG
            // SaveChanges với thao tác đang làm — không bao giờ có khoảnh khắc 0 hoặc 2 mặc định.
            var others = await db.CustomerAddresses
                .Where(a => a.UserId == userId && a.IsDefault && a.Id != current.Id)
                .ToListAsync();

            foreach (var other in others)
            {
                other.IsDefault = false;
            }

            return;
        }

        // Địa chỉ ĐẦU TIÊN của khách luôn là mặc định dù họ không tích ô — không thì sổ địa chỉ
        // có mục nhưng trang đặt hàng vẫn trống trơn, trông như tính năng hỏng.
        var hasOtherDefault = await db.CustomerAddresses
            .AnyAsync(a => a.UserId == userId && a.IsDefault && a.Id != current.Id);

        if (!hasOtherDefault)
        {
            current.IsDefault = true;
        }
    }
}
