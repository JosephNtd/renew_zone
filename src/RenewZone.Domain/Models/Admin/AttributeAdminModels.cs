namespace RenewZone.Domain.Models.Admin;

/// <param name="ProductCount">Số nhóm biến thể đang dùng thuộc tính (kể cả sản phẩm trong Thùng rác).</param>
public record AttributeItem(
    int Id,
    string Name,
    int DisplayOrder,
    int ProductCount,
    IReadOnlyList<AttributeValueItem> Values);

/// <param name="ProductCount">Số sản phẩm đang dùng giá trị này (kể cả sản phẩm trong Thùng rác).</param>
public record AttributeValueItem(int Id, string Value, int DisplayOrder, int ProductCount);

public class AttributeInput
{
    /// <summary>Null ⇒ thêm mới.</summary>
    public int? Id { get; init; }
    public required string Name { get; init; }
    public int DisplayOrder { get; init; }
}

public class AttributeValueInput
{
    /// <summary>Null ⇒ thêm mới.</summary>
    public int? Id { get; init; }
    public int AttributeId { get; init; }
    public required string Value { get; init; }
    public int DisplayOrder { get; init; }
}
