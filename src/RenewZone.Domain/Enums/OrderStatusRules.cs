namespace RenewZone.Domain.Enums;

/// <summary>
/// Luồng trạng thái đơn hàng hợp lệ.
/// <code>
///         ┌──────────────────────── Cancelled ◄──────┐
///         │                                          │
///    New ─┴─► Confirmed ──► Shipping ──► Completed   │
///     │           │                                  │
///     └───────────┴──────────────────────────────────┘
/// </code>
/// <para>
/// Đặt ở Domain vì đây là NGHIỆP VỤ, không phải trang trí giao diện: dropdown ở client chỉ hiện
/// lựa chọn hợp lệ cho dễ dùng, nhưng đó KHÔNG phải biện pháp bảo vệ — service bắt buộc gọi
/// <see cref="CanTransition"/> và từ chối bước nhảy sai kể cả khi bị gọi thẳng bằng curl.
/// </para>
/// </summary>
public static class OrderStatusRules
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.New] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Shipping, OrderStatus.Cancelled],
        [OrderStatus.Shipping] = [OrderStatus.Completed, OrderStatus.Cancelled],

        // Hai trạng thái kết thúc: đã chốt sổ thì không quay lại được nữa
        [OrderStatus.Completed] = [],
        [OrderStatus.Cancelled] = []
    };

    public static IReadOnlyList<OrderStatus> AllowedNext(OrderStatus current) =>
        Allowed.TryGetValue(current, out var next) ? next : [];

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        AllowedNext(from).Contains(to);

    /// <summary>
    /// Khách hàng được TỰ hủy đơn của mình hay không (Giai đoạn 9). Hẹp hơn quyền của admin:
    /// hàng đã rời kho (<see cref="OrderStatus.Shipping"/>) thì phải gọi shop để phối hợp với
    /// bên vận chuyển, chứ không được tự bấm hủy. Admin vẫn hủy được ở mọi chặng chưa kết thúc.
    /// </summary>
    public static bool CustomerCanCancel(OrderStatus current) =>
        current is OrderStatus.New or OrderStatus.Confirmed;

    /// <summary>Các chặng của thanh timeline (không gồm Cancelled — hủy là nhánh rẽ, không phải chặng).</summary>
    public static readonly OrderStatus[] Pipeline =
    [
        OrderStatus.New, OrderStatus.Confirmed, OrderStatus.Shipping, OrderStatus.Completed
    ];
}
