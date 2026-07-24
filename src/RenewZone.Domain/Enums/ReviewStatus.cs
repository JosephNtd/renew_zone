namespace RenewZone.Domain.Enums;

/// <summary>
/// Vòng đời một đánh giá sản phẩm. Khách gửi ⇒ Pending; admin duyệt ⇒ Approved (hiện trên PDP)
/// hoặc từ chối ⇒ Rejected (giữ lại để lần sau không phải xét lại, nhưng không hiện ra web khách).
/// Chỉ đánh giá Approved mới được tính vào điểm trung bình trên card + PDP.
/// </summary>
public enum ReviewStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
