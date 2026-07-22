using System.Reflection;
using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="ISettingAdminService"/>.</summary>
public class SettingAdminService(AppDbContext db) : ISettingAdminService
{
    /// <summary>
    /// Whitelist key hợp lệ, đọc bằng reflection từ hằng của <see cref="SettingKeys"/> —
    /// thêm setting mới ở giai đoạn sau chỉ cần thêm hằng, không phải sửa file này.
    /// </summary>
    private static readonly HashSet<string> AllowedKeys =
    [
        .. typeof(SettingKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
    ];

    public async Task<IReadOnlyDictionary<string, string?>> GetAllAsync() =>
        await db.SiteSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value);

    public async Task<AdminResult> SaveAsync(IDictionary<string, string?> values)
    {
        var existing = await db.SiteSettings.ToDictionaryAsync(s => s.Key);
        var changed = 0;

        foreach (var (key, value) in values)
        {
            // Chống chèn key rác: form bị sửa tay không được tạo setting mà không code nào đọc
            if (!AllowedKeys.Contains(key))
            {
                continue;
            }

            // Chuỗi rỗng lưu thành NULL cho nhất quán — helper Get() ở view coi cả hai là "thiếu"
            // rồi rơi về fallback, nên đừng để 2 cách biểu diễn cùng một trạng thái
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

            if (existing.TryGetValue(key, out var setting))
            {
                if (setting.Value == normalized)
                {
                    continue;
                }

                setting.Value = normalized;
                changed++;
            }
            else
            {
                db.SiteSettings.Add(new SiteSetting { Key = key, Value = normalized });
                changed++;
            }
        }

        if (changed == 0)
        {
            return AdminResult.Success("Không có thay đổi nào.");
        }

        // SaveChanges tự xóa cache SiteContent ⇒ web khách đổi theo ngay
        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã lưu {changed} thiết lập.");
    }
}
