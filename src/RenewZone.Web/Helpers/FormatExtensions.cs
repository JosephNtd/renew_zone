using System.Globalization;

namespace RenewZone.Web.Helpers;

/// <summary>Định dạng hiển thị dùng chung cho các view (giá tiền VND...).</summary>
public static class FormatExtensions
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    /// <summary>Định dạng giá kiểu Việt Nam: 29.990.000đ.</summary>
    public static string ToVnd(this decimal price) => price.ToString("N0", Vi) + "đ";
}
