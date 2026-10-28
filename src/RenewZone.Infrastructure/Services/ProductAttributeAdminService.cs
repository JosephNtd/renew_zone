using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="IProductAttributeAdminService"/>.</summary>
public class ProductAttributeAdminService(
    AppDbContext db,
    ILogger<ProductAttributeAdminService> logger) : IProductAttributeAdminService
{
    private const int MaxLength = 100;

    public async Task<IReadOnlyList<AttributeItem>> GetAllAsync()
    {
        // Đếm thẳng trên bảng nhóm/giá trị biến thể (không đi qua Product) nên sản phẩm trong
        // Thùng rác vẫn được tính — đúng với ràng buộc Restrict của DB khi xóa.
        var attributes = await db.ProductAttributes.AsNoTracking()
            .OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.DisplayOrder,
                ProductCount = db.ProductOptionGroups.Count(g => g.AttributeId == a.Id),
                Values = a.Values
                    .OrderBy(v => v.DisplayOrder).ThenBy(v => v.Value)
                    .Select(v => new AttributeValueItem(
                        v.Id, v.Value, v.DisplayOrder,
                        db.ProductOptionValues.Count(o => o.AttributeValueId == v.Id)))
                    .ToList()
            })
            .ToListAsync();

        return [.. attributes.Select(a => new AttributeItem(a.Id, a.Name, a.DisplayOrder, a.ProductCount, a.Values))];
    }

    public async Task<AdminResult> SaveAttributeAsync(AttributeInput input)
    {
        var name = input.Name?.Trim() ?? "";
        if (name.Length == 0 || name.Length > MaxLength)
        {
            return AdminResult.Fail($"Tên thuộc tính không được để trống và tối đa {MaxLength} ký tự.");
        }

        // So sánh chạy trên SQL Server (collation không phân biệt hoa thường) ⇒ "màu sắc" trùng "Màu sắc"
        if (await db.ProductAttributes.AnyAsync(a => a.Name == name && a.Id != input.Id))
        {
            return AdminResult.Fail($"Thuộc tính “{name}” đã có trong danh sách.");
        }

        var attribute = input.Id is { } id
            ? await db.ProductAttributes.FirstOrDefaultAsync(a => a.Id == id)
            : null;

        if (input.Id is not null && attribute is null)
        {
            return AdminResult.Fail("Không tìm thấy thuộc tính.");
        }

        if (attribute is null)
        {
            attribute = new ProductAttribute();
            db.ProductAttributes.Add(attribute);
        }

        attribute.Name = name;
        attribute.DisplayOrder = input.DisplayOrder;
        await db.SaveChangesAsync();

        return AdminResult.Success(input.Id is null
            ? $"Đã thêm thuộc tính “{name}”."
            : $"Đã lưu thuộc tính “{name}”.", attribute.Id);
    }

    public async Task<AdminResult> DeleteAttributeAsync(int id)
    {
        var attribute = await db.ProductAttributes.FirstOrDefaultAsync(a => a.Id == id);
        if (attribute is null)
        {
            return AdminResult.Fail("Không tìm thấy thuộc tính.");
        }

        var used = await db.ProductOptionGroups.CountAsync(g => g.AttributeId == id);
        if (used > 0)
        {
            return AdminResult.Fail(
                $"Không xóa được — thuộc tính “{attribute.Name}” đang dùng ở {used} sản phẩm " +
                "(kể cả sản phẩm trong Thùng rác). Hãy gỡ khỏi các sản phẩm đó trước.");
        }

        db.ProductAttributes.Remove(attribute); // xóa cứng, cascade xuống các giá trị
        await db.SaveChangesAsync();

        logger.LogInformation("Xóa thuộc tính {Name} (id {Id})", attribute.Name, id);

        return AdminResult.Success($"Đã xóa thuộc tính “{attribute.Name}”.");
    }

    public async Task<AdminResult> SaveValueAsync(AttributeValueInput input)
    {
        var text = input.Value?.Trim() ?? "";
        if (text.Length == 0 || text.Length > MaxLength)
        {
            return AdminResult.Fail($"Giá trị không được để trống và tối đa {MaxLength} ký tự.");
        }

        var attribute = await db.ProductAttributes.AsNoTracking().FirstOrDefaultAsync(a => a.Id == input.AttributeId);
        if (attribute is null)
        {
            return AdminResult.Fail("Thuộc tính không hợp lệ.");
        }

        if (await db.ProductAttributeValues.AnyAsync(v =>
                v.AttributeId == input.AttributeId && v.Value == text && v.Id != input.Id))
        {
            return AdminResult.Fail($"“{text}” đã có trong thuộc tính “{attribute.Name}”.");
        }

        var value = input.Id is { } id
            ? await db.ProductAttributeValues.FirstOrDefaultAsync(v => v.Id == id)
            : null;

        if (input.Id is not null && value is null)
        {
            return AdminResult.Fail("Không tìm thấy giá trị.");
        }

        if (value is null)
        {
            value = new ProductAttributeValue { AttributeId = input.AttributeId };
            db.ProductAttributeValues.Add(value);
        }
        else if (value.AttributeId != input.AttributeId)
        {
            // Đổi thuộc tính của giá trị đang dùng sẽ làm nhóm "Màu sắc" của sản phẩm chứa giá trị "256GB"
            return AdminResult.Fail("Không chuyển giá trị sang thuộc tính khác được. Hãy thêm giá trị mới.");
        }

        value.Value = text;
        value.DisplayOrder = input.DisplayOrder;
        await db.SaveChangesAsync();

        return AdminResult.Success(input.Id is null
            ? $"Đã thêm “{text}” vào “{attribute.Name}”."
            : $"Đã lưu “{text}”. Sản phẩm đang dùng giá trị này sẽ hiện tên mới.", value.Id);
    }

    public async Task<AdminResult> DeleteValueAsync(int id)
    {
        var value = await db.ProductAttributeValues.FirstOrDefaultAsync(v => v.Id == id);
        if (value is null)
        {
            return AdminResult.Fail("Không tìm thấy giá trị.");
        }

        var used = await db.ProductOptionValues.CountAsync(v => v.AttributeValueId == id);
        if (used > 0)
        {
            return AdminResult.Fail(
                $"Không xóa được — “{value.Value}” đang dùng ở {used} sản phẩm " +
                "(kể cả sản phẩm trong Thùng rác). Hãy gỡ khỏi các sản phẩm đó trước.");
        }

        db.ProductAttributeValues.Remove(value);
        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã xóa “{value.Value}”.");
    }
}
