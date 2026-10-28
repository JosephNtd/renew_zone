using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;

namespace RenewZone.Web.Helpers;

/// <summary>
/// Thương hiệu hiển thị trong khu quản trị (thanh bên, trang đăng nhập, tiêu đề tab).
/// Dùng chung ĐÚNG bộ setting với web khách — chủ shop tải logo một lần là đổi cả hai nơi,
/// không phải nhớ "logo này cho web, logo kia cho admin".
/// </summary>
/// <param name="Name">Tên cửa hàng — đã qua fallback nên không bao giờ rỗng.</param>
/// <param name="Initials">Chữ tắt cho ô vuông 32px: dùng khi chưa có ảnh logo, và khi thanh bên thu gọn.</param>
/// <param name="LogoUrl">Ảnh logo nền SÁNG — trang đăng nhập (thẻ trắng). Rỗng ⇒ dùng <paramref name="Initials"/>.</param>
/// <param name="LogoDarkUrl">Ảnh logo nền TỐI — thanh bên quản trị. Rỗng ⇒ dùng chữ tắt + tên.</param>
public record AdminBrand(string Name, string Initials, string LogoUrl, string LogoDarkUrl);

public static class AdminBrandExtensions
{
    private const string FallbackName = "RenewZone";

    public static async Task<AdminBrand> GetAdminBrandAsync(this ISiteContentService content)
    {
        try
        {
            var settings = await content.GetSettingsAsync();
            var name = settings.Get(SettingKeys.StoreName, FallbackName);

            return new AdminBrand(
                name,
                InitialsOf(name),
                settings.Get(SettingKeys.LogoUrl),
                settings.Get(SettingKeys.LogoDarkUrl));
        }
        catch (Exception)
        {
            // Trang ĐĂNG NHẬP cũng gọi hàm này. Nếu database đang hỏng thì vẫn phải hiện được form
            // (và thông báo lỗi khi bấm đăng nhập) chứ không phải một trang 500 trắng trơn — cùng
            // triết lý với trang lỗi 500 tự chủ ở mục 4.11. Logo chỉ là trang trí, không đáng
            // để làm sập lối vào duy nhất của quản trị viên.
            return new AdminBrand(FallbackName, InitialsOf(FallbackName), "", "");
        }
    }

    /// <summary>
    /// Chữ đầu của 2 từ đầu tiên ("Renew Zone" ⇒ "RZ"); tên một từ ⇒ 2 ký tự đầu ("RenewZone" ⇒ "RE").
    /// Tự suy ra từ tên cửa hàng nên đổi tên trong Cấu hình là ô vuông đổi theo.
    /// </summary>
    private static string InitialsOf(string name)
    {
        var words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var initials = words.Length switch
        {
            0 => "",
            1 => new string(words[0].Take(2).ToArray()),
            _ => string.Concat(words[0][..1], words[1][..1])
        };

        return initials.Length > 0 ? initials.ToUpperInvariant() : "AD";
    }
}
