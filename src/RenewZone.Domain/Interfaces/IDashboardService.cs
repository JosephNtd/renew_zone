using RenewZone.Domain.Models.Admin;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Số liệu tổng quan cho <c>/admin</c>. KHÔNG cache: admin mở dashboard chính là lúc muốn biết
/// tình hình NGAY BÂY GIỜ — số liệu cũ 5 phút ở đây tệ hơn là chậm thêm vài chục mili giây.
/// </summary>
public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync();
}
