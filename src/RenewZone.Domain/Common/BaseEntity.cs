namespace RenewZone.Domain.Common;

/// <summary>
/// Lớp cơ sở cho mọi entity có khóa chính kiểu int.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}
