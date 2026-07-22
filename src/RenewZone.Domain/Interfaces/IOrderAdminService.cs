using RenewZone.Domain.Enums;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Domain.Interfaces;

/// <summary>Quản lý đơn hàng phía admin. Đơn KHÔNG xóa được — hủy đơn là đổi trạng thái.</summary>
public interface IOrderAdminService
{
    Task<PagedResult<OrderListItem>> GetListAsync(OrderListRequest request);

    Task<OrderDetail?> GetDetailAsync(int id);

    /// <summary>Số đơn theo từng trạng thái, cho các tab nhanh ở đầu danh sách.</summary>
    Task<IReadOnlyDictionary<OrderStatus, int>> GetStatusCountsAsync();

    /// <summary>
    /// Đổi trạng thái. BẮT BUỘC kiểm bảng chuyển hợp lệ (<see cref="OrderStatusRules"/>) —
    /// dropdown ở client chỉ để dễ dùng, không phải biện pháp bảo vệ.
    /// </summary>
    Task<ChangeStatusResult> ChangeStatusAsync(int id, OrderStatus newStatus);
}
