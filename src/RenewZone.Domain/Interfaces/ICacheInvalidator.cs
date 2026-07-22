using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// "Bộ não" cache của hệ thống: biết mỗi loại entity ảnh hưởng những vùng cache nào rồi tự xóa.
/// <para>
/// <b>Vì sao cần:</b> mọi service cấp layout đều cache TTL 5 phút. Cách ngây thơ là mỗi action admin
/// tự gọi <c>Invalidate()</c> — với ~15 màn × 3 action, chỉ cần MỘT chỗ quên là sinh bug
/// "sửa xong không thấy đổi", loại bug rất khó truy vì phải đợi hết 5 phút mới hết triệu chứng.
/// Thay vào đó <c>AppDbContext.SaveChangesAsync</c> gom loại entity vừa ghi rồi gọi
/// <see cref="OnEntitiesChanged"/> — controller KHÔNG phải nhớ gì cả.
/// </para>
/// <para>
/// <b>Thêm entity có cache ở giai đoạn sau:</b> chỉ cần thêm một dòng vào bảng tra trong
/// <c>CacheInvalidator</c>, không đụng controller.
/// </para>
/// ⚠️ <c>ExecuteDelete()</c>/<c>ExecuteUpdate()</c> KHÔNG đi qua <c>SaveChanges</c> nên không kích hoạt
/// cơ chế này — chỗ nào dùng phải tự gọi <see cref="Invalidate"/>.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>Xóa cache của mọi vùng liên quan tới danh sách loại entity vừa được ghi.</summary>
    void OnEntitiesChanged(IEnumerable<Type> changedEntityTypes);

    /// <summary>Xóa cache của đúng các vùng chỉ định — dùng khi ghi DB không qua SaveChanges.</summary>
    void Invalidate(CacheScope scopes);
}
