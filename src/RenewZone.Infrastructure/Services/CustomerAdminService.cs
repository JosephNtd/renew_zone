using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;
using RenewZone.Infrastructure.Identity;

namespace RenewZone.Infrastructure.Services;

/// <inheritdoc cref="ICustomerAdminService" />
public class CustomerAdminService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    ILogger<CustomerAdminService> logger) : ICustomerAdminService
{
    /// <summary>Mốc "khóa vô thời hạn" của Identity — quy ước là một ngày rất xa trong tương lai.</summary>
    private static readonly DateTimeOffset ForeverLock = new(new DateTime(9999, 12, 31), TimeSpan.Zero);

    public async Task<PagedResult<CustomerListItem>> GetListAsync(CustomerListRequest request)
    {
        // CHỈ tài khoản có role Customer. Quản trị viên không phải khách hàng — trộn chung
        // thì admin sẽ tự khóa nhầm chính mình khỏi trang quản trị.
        var customerRoleId = await db.Roles
            .Where(r => r.Name == AppRoles.Customer)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var query =
            from user in db.Users
            join userRole in db.UserRoles on user.Id equals userRole.UserId
            where userRole.RoleId == customerRoleId
            select user;

        if (request.NormalizedSearch is { } search)
        {
            query = query.Where(u =>
                (u.UserName != null && u.UserName.Contains(search))
                || (u.FullName != null && u.FullName.Contains(search))
                || (u.Email != null && u.Email.Contains(search))
                || (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
        }

        if (request.IsLocked is { } locked)
        {
            query = locked
                ? query.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.Now)
                : query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.Now);
        }

        var total = await query.CountAsync();

        query = request.SortBy switch
        {
            "name" => request.SortDesc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
            "phone" => request.SortDesc ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName),
            _ => request.SortDesc ? query.OrderBy(u => u.CreatedAt) : query.OrderByDescending(u => u.CreatedAt)
        };

        var items = await query
            .AsNoTracking()
            .Skip(request.Skip)
            .Take(request.SafePageSize)
            .Select(u => new CustomerListItem(
                u.Id,
                u.UserName!,
                u.FullName,
                u.Email,

                // Truy vấn con tính ngay trong SQL — nạp đơn về rồi đếm bằng C# sẽ là N+1
                db.Orders.Count(o => o.UserId == u.Id),
                db.Orders
                    .Where(o => o.UserId == u.Id && o.Status == OrderStatus.Completed)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0m,
                u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.Now,
                u.LockoutEnd == null ? null : u.LockoutEnd.Value.LocalDateTime,
                u.CreatedAt))
            .ToListAsync();

        return new PagedResult<CustomerListItem>
        {
            Items = items,
            Page = request.SafePage,
            PageSize = request.SafePageSize,
            TotalItems = total
        };
    }

    public async Task<CustomerDetail?> GetDetailAsync(string userId)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return null;
        }

        var orders = db.Orders.AsNoTracking().Where(o => o.UserId == userId);

        var recent = await orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new OrderListItem(
                o.Id, o.Code, o.CustomerName, o.Phone, o.Items.Count,
                o.TotalAmount, o.PaymentMethod, o.Status, o.CreatedAt))
            .ToListAsync();

        var addresses = await db.CustomerAddresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .Select(a => new CustomerAddressView(
                a.Label, a.ReceiverName, a.Phone, a.AddressLine, a.IsDefault))
            .ToListAsync();

        return new CustomerDetail(
            user.Id,
            user.UserName!,
            user.FullName,
            user.Email,
            user.DateOfBirth,
            user.Gender,
            user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.Now,
            user.LockoutEnd?.LocalDateTime,
            user.AccessFailedCount,
            user.CreatedAt,
            await orders.CountAsync(),
            await orders.CountAsync(o => o.Status == OrderStatus.Completed),
            await orders.Where(o => o.Status == OrderStatus.Completed)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m,

            // Đánh giá đã xóa mềm bị query filter loại sẵn — con số này là "đang còn hiệu lực"
            await db.ProductReviews.CountAsync(r => r.UserId == userId),
            await db.WishlistItems.CountAsync(w => w.UserId == userId),
            recent,
            addresses);
    }

    public async Task<CustomerOpResult> SetLockedAsync(string userId, bool locked)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return CustomerOpResult.Fail("Không tìm thấy tài khoản này.");
        }

        // Chốt an toàn: màn này chỉ liệt kê khách hàng, nhưng ai đó gọi thẳng POST với id của
        // một quản trị viên thì sẽ tự khóa mình ra khỏi trang quản trị — không có đường quay lại.
        if (await userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            return CustomerOpResult.Fail("Không thể khóa tài khoản quản trị.");
        }

        // Identity chỉ chặn đăng nhập khi LockoutEnabled = true; thiếu dòng này thì đặt
        // LockoutEnd xong tài khoản vẫn đăng nhập được như thường.
        await userManager.SetLockoutEnabledAsync(user, true);

        var result = await userManager.SetLockoutEndDateAsync(user, locked ? ForeverLock : null);
        if (!result.Succeeded)
        {
            return CustomerOpResult.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        if (!locked)
        {
            // Mở khóa mà không xóa bộ đếm thì lần nhập sai kế tiếp là khóa lại ngay
            await userManager.ResetAccessFailedCountAsync(user);
        }

        logger.LogInformation("Admin {Action} tài khoản khách {Phone}", locked ? "khóa" : "mở khóa", user.UserName);

        return CustomerOpResult.Success(locked
            ? $"Đã khóa tài khoản {user.UserName}. Khách sẽ không đăng nhập được nữa."
            : $"Đã mở khóa tài khoản {user.UserName}.");
    }

    public async Task<CustomerOpResult> ResetPasswordAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return CustomerOpResult.Fail("Không tìm thấy tài khoản này.");
        }

        if (await userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            return CustomerOpResult.Fail("Không thể đặt lại mật khẩu tài khoản quản trị tại đây.");
        }

        var temporary = GenerateTemporaryPassword();

        // Dùng token reset thay vì RemovePassword + AddPassword: một thao tác nguyên tử, và
        // nếu lỗi giữa chừng thì tài khoản không rơi vào trạng thái KHÔNG CÓ mật khẩu nào.
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, temporary);

        if (!result.Succeeded)
        {
            return CustomerOpResult.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        // Đặt lại mật khẩu cũng là cách gỡ kẹt cho khách đang bị khóa vì nhập sai nhiều lần
        await userManager.ResetAccessFailedCountAsync(user);

        // Ghi log CÓ tên tài khoản nhưng TUYỆT ĐỐI không ghi mật khẩu — log là nơi bị đọc lại
        // nhiều nhất, và mật khẩu nằm trong log thì coi như đã lộ.
        logger.LogInformation("Admin đặt lại mật khẩu cho tài khoản khách {Phone}", user.UserName);

        return CustomerOpResult.Success(
            $"Đã đặt lại mật khẩu cho {user.UserName}. Hãy đọc mật khẩu tạm cho khách và nhắc họ đổi lại ngay.",
            temporary);
    }

    public async Task<int> CountAsync()
    {
        var customerRoleId = await db.Roles
            .Where(r => r.Name == AppRoles.Customer)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        return await db.UserRoles.CountAsync(ur => ur.RoleId == customerRoleId);
    }

    /// <summary>
    /// Mật khẩu tạm 10 ký tự sinh bằng bộ sinh số ngẫu nhiên AN TOÀN (<see cref="RandomNumberGenerator"/>),
    /// không phải <c>Random</c> — <c>Random</c> gieo theo thời gian nên đoán được, mà đây là thứ
    /// duy nhất bảo vệ tài khoản trong khoảng thời gian giữa lúc cấp lại và lúc khách đổi.
    /// Bỏ các ký tự dễ đọc nhầm (0/O, 1/l/I) vì mật khẩu này được đọc qua điện thoại.
    /// </summary>
    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var chars = new char[10];

        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(chars);
    }
}
