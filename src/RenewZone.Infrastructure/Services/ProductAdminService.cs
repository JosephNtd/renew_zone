using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="IProductAdminService"/>.</summary>
public class ProductAdminService(
    AppDbContext db,
    ISlugService slugs,
    ILogger<ProductAdminService> logger) : IProductAdminService
{
    public async Task<PagedResult<ProductListItem>> GetListAsync(ProductListRequest request)
    {
        var query = db.Products.AsNoTracking().AsQueryable();

        if (request.CategoryId is { } categoryId)
        {
            // Chọn danh mục CHA thì phải lấy cả sản phẩm của các dòng con — nếu không admin
            // bấm "iPhone" sẽ thấy danh sách rỗng vì sản phẩm đều gắn vào series con
            query = query.Where(p => p.CategoryId == categoryId || p.Category.ParentId == categoryId);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(p => p.IsActive == isActive);
        }

        if (request.HasImage is { } hasImage)
        {
            query = hasImage ? query.Where(p => p.Images.Any()) : query.Where(p => !p.Images.Any());
        }

        if (request.HasDescription is { } hasDescription)
        {
            query = hasDescription
                ? query.Where(p => p.DescriptionHtml != null && p.DescriptionHtml != "")
                : query.Where(p => p.DescriptionHtml == null || p.DescriptionHtml == "");
        }

        if (request.NormalizedSearch is { } search)
        {
            query = query.Where(p => p.Name.Contains(search) || p.Slug.Contains(search));
        }

        var totalItems = await query.CountAsync();

        query = (request.SortBy, request.SortDesc) switch
        {
            ("ten", false) => query.OrderBy(p => p.Name),
            ("ten", true) => query.OrderByDescending(p => p.Name),
            ("gia", false) => query.OrderBy(p => p.SalePrice ?? p.OriginalPrice),
            ("gia", true) => query.OrderByDescending(p => p.SalePrice ?? p.OriginalPrice),
            ("luotxem", false) => query.OrderBy(p => p.ViewCount),
            ("luotxem", true) => query.OrderByDescending(p => p.ViewCount),
            ("yeuthich", false) => query.OrderBy(p => db.WishlistItems.Count(w => w.ProductId == p.Id)),
            ("yeuthich", true) => query.OrderByDescending(p => db.WishlistItems.Count(w => w.ProductId == p.Id)),
            ("daban", false) => query.OrderBy(p => db.OrderItems
                .Where(i => i.ProductId == p.Id && i.Order.Status == OrderStatus.Completed)
                .Sum(i => (int?)i.Quantity) ?? 0),
            ("daban", true) => query.OrderByDescending(p => db.OrderItems
                .Where(i => i.ProductId == p.Id && i.Order.Status == OrderStatus.Completed)
                .Sum(i => (int?)i.Quantity) ?? 0),
            ("ngay", false) => query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt) // mặc định: mới thêm lên đầu
        };

        var items = await query
            .Skip(request.Skip)
            .Take(request.SafePageSize)
            .Select(p => new ProductListItem(
                p.Id, p.Name, p.Slug, p.Category.Name,
                p.Images.Where(i => i.IsMain).Select(i => i.Url).FirstOrDefault()
                    ?? p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault(),
                p.OriginalPrice, p.SalePrice, p.IsContactPrice,
                p.IsActive, p.IsFeatured, p.IsBestSeller, p.IsInstallment0Percent,
                p.ViewCount, p.Images.Count,
                db.WishlistItems.Count(w => w.ProductId == p.Id),
                // "Đã bán" chỉ tính đơn Hoàn thành: đơn đang xử lý còn có thể bị hủy
                db.OrderItems
                    .Where(i => i.ProductId == p.Id && i.Order.Status == OrderStatus.Completed)
                    .Sum(i => (int?)i.Quantity) ?? 0))
            .ToListAsync();

        return new PagedResult<ProductListItem>
        {
            Items = items,
            Page = request.SafePage,
            PageSize = request.SafePageSize,
            TotalItems = totalItems
        };
    }

    public async Task<Product?> GetForEditAsync(int id) =>
        await db.Products
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.OptionGroups.OrderBy(g => g.DisplayOrder))
                .ThenInclude(g => g.Values.OrderBy(v => v.DisplayOrder))
            .AsSplitQuery() // 2 collection cùng lúc ⇒ tránh tích Descartes (bài học Giai đoạn 3)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<AdminResult> CreateAsync(ProductInput input)
    {
        var validation = await ValidateAsync(input, null);
        if (validation is not null)
        {
            return validation;
        }

        var product = new Product
        {
            CategoryId = input.CategoryId,
            Name = input.Name.Trim(),
            Slug = slugs.Slugify(input.Slug),
            OriginalPrice = input.OriginalPrice,
            SalePrice = input.SalePrice,
            IsContactPrice = input.IsContactPrice,
            ShortDescription = input.ShortDescription,
            DescriptionHtml = input.DescriptionHtml,
            SpecsHtml = input.SpecsHtml,
            IsInstallment0Percent = input.IsInstallment0Percent,
            IsFeatured = input.IsFeatured,
            IsBestSeller = input.IsBestSeller,
            IsActive = input.IsActive
        };

        foreach (var group in BuildGroups(input.OptionGroups))
        {
            product.OptionGroups.Add(group);
        }

        db.Products.Add(product);
        await db.SaveChangesAsync();

        logger.LogInformation("Tạo sản phẩm {Name} (id {Id})", product.Name, product.Id);

        return AdminResult.Success($"Đã thêm sản phẩm “{product.Name}”.", product.Id);
    }

    public async Task<AdminResult> UpdateAsync(ProductInput input)
    {
        var product = await db.Products
            .Include(p => p.OptionGroups)
                .ThenInclude(g => g.Values)
            .FirstOrDefaultAsync(p => p.Id == input.Id);

        if (product is null)
        {
            return AdminResult.Fail("Không tìm thấy sản phẩm.");
        }

        var validation = await ValidateAsync(input, product.Id);
        if (validation is not null)
        {
            return validation;
        }

        product.CategoryId = input.CategoryId;
        product.Name = input.Name.Trim();
        product.Slug = slugs.Slugify(input.Slug);
        product.OriginalPrice = input.OriginalPrice;
        product.SalePrice = input.SalePrice;
        product.IsContactPrice = input.IsContactPrice;
        product.ShortDescription = input.ShortDescription;
        product.DescriptionHtml = input.DescriptionHtml;
        product.SpecsHtml = input.SpecsHtml;
        product.IsInstallment0Percent = input.IsInstallment0Percent;
        product.IsFeatured = input.IsFeatured;
        product.IsBestSeller = input.IsBestSeller;
        product.IsActive = input.IsActive;

        SyncOptionGroups(product, input.OptionGroups);

        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã lưu sản phẩm “{product.Name}”.", product.Id);
    }

    /// <summary>Trả về null khi hợp lệ; ngược lại là lỗi nghiệp vụ để controller hiện lên form.</summary>
    private async Task<AdminResult?> ValidateAsync(ProductInput input, int? currentId)
    {
        var slug = slugs.Slugify(input.Slug);

        var check = await slugs.CheckAsync(slug, SlugTargetType.Product, currentId);
        if (!check.IsAvailable)
        {
            return AdminResult.Fail(check.Message!);
        }

        // Danh mục phải tồn tại: id lạ do sửa form sẽ làm SaveChanges ném lỗi khóa ngoại thô
        if (!await db.Categories.AnyAsync(c => c.Id == input.CategoryId))
        {
            return AdminResult.Fail("Danh mục không hợp lệ.");
        }

        // Sản phẩm "Liên hệ" không hiện giá nên không cần kiểm giá (Lưu ý 6.5)
        if (!input.IsContactPrice && input.SalePrice is { } sale && sale > input.OriginalPrice)
        {
            return AdminResult.Fail("Giá khuyến mãi không được cao hơn giá gốc.");
        }

        return await ValidateOptionGroupsAsync(input.OptionGroups);
    }

    /// <summary>
    /// Biến thể chỉ được CHỌN từ danh sách chung (Giai đoạn 10): mọi id thuộc tính/giá trị form gửi lên
    /// phải có thật và giá trị phải đúng thuộc tính của nhóm — không tin dữ liệu form (có thể bị sửa tay).
    /// </summary>
    private async Task<AdminResult?> ValidateOptionGroupsAsync(IReadOnlyList<OptionGroupInput> groups)
    {
        if (groups.Count == 0)
        {
            return null;
        }

        var attributeIds = groups.Select(g => g.AttributeId).Distinct().ToList();
        var attributes = await db.ProductAttributes.AsNoTracking()
            .Where(a => attributeIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name);

        var valueIds = groups.SelectMany(g => g.Values).Select(v => v.AttributeValueId).Distinct().ToList();
        var valueOwners = await db.ProductAttributeValues.AsNoTracking()
            .Where(v => valueIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.AttributeId);

        var usedAttributes = new HashSet<int>();
        foreach (var group in groups)
        {
            if (!attributes.TryGetValue(group.AttributeId, out var name))
            {
                return AdminResult.Fail("Có nhóm biến thể chưa chọn thuộc tính hoặc thuộc tính không còn tồn tại.");
            }

            // Một sản phẩm chỉ có một nhóm cho mỗi thuộc tính, không thì PDP hiện 2 hàng "Màu sắc"
            if (!usedAttributes.Add(group.AttributeId))
            {
                return AdminResult.Fail($"Thuộc tính “{name}” bị chọn ở 2 nhóm. Mỗi thuộc tính chỉ dùng một nhóm.");
            }

            // Nhóm biến thể phải có ít nhất 1 giá trị, nếu không PDP hiện một nhóm trống vô nghĩa
            if (group.Values.Count == 0)
            {
                return AdminResult.Fail($"Nhóm biến thể “{name}” chưa có giá trị nào.");
            }

            var usedValues = new HashSet<int>();
            foreach (var value in group.Values)
            {
                if (!valueOwners.TryGetValue(value.AttributeValueId, out var owner) || owner != group.AttributeId)
                {
                    return AdminResult.Fail($"Nhóm “{name}” có giá trị không thuộc danh sách của thuộc tính này.");
                }

                if (!usedValues.Add(value.AttributeValueId))
                {
                    return AdminResult.Fail($"Nhóm “{name}” có một giá trị bị chọn 2 lần.");
                }
            }
        }

        return null;
    }

    private static List<ProductOptionGroup> BuildGroups(IReadOnlyList<OptionGroupInput> inputs)
    {
        var result = new List<ProductOptionGroup>();

        for (var i = 0; i < inputs.Count; i++)
        {
            var groupInput = inputs[i];

            var group = new ProductOptionGroup
            {
                AttributeId = groupInput.AttributeId,
                DisplayOrder = i
            };

            for (var v = 0; v < groupInput.Values.Count; v++)
            {
                var value = groupInput.Values[v];
                group.Values.Add(new ProductOptionValue
                {
                    AttributeValueId = value.AttributeValueId,
                    PriceAdjustment = value.PriceAdjustment,
                    IsDefault = value.IsDefault,
                    DisplayOrder = v
                });
            }

            EnsureSingleDefault(group);
            result.Add(group);
        }

        return result;
    }

    /// <summary>
    /// Đồng bộ nhóm/giá trị biến thể theo form: sửa cái còn, thêm cái mới, XÓA CỨNG cái bị gỡ.
    /// <para>
    /// Cố ý GIỮ LẠI Id của những giá trị không bị xóa: id option nằm trong key dòng giỏ hàng
    /// (<c>productId-optionValueIds</c>) và trong session của khách đang mua — tạo lại toàn bộ
    /// (xóa hết rồi thêm mới) sẽ làm giỏ hàng của mọi khách đang mở trang hỏng ngay lập tức.
    /// </para>
    /// </summary>
    private void SyncOptionGroups(Product product, IReadOnlyList<OptionGroupInput> inputs)
    {
        var keptGroupIds = inputs.Where(g => g.Id is not null).Select(g => g.Id!.Value).ToHashSet();

        // Nhóm bị gỡ khỏi form ⇒ xóa cứng (ProductOptionGroup không xóa mềm, cascade xuống Values)
        foreach (var removed in product.OptionGroups.Where(g => !keptGroupIds.Contains(g.Id)).ToList())
        {
            db.ProductOptionGroups.Remove(removed);
        }

        for (var i = 0; i < inputs.Count; i++)
        {
            var groupInput = inputs[i];

            var group = groupInput.Id is { } groupId
                ? product.OptionGroups.FirstOrDefault(g => g.Id == groupId)
                : null;

            if (group is null)
            {
                group = new ProductOptionGroup { AttributeId = groupInput.AttributeId, DisplayOrder = i };
                product.OptionGroups.Add(group);
            }
            else
            {
                group.AttributeId = groupInput.AttributeId;
                group.DisplayOrder = i;
            }

            SyncValues(group, groupInput.Values);
            EnsureSingleDefault(group);
        }
    }

    private void SyncValues(ProductOptionGroup group, IReadOnlyList<OptionValueInput> inputs)
    {
        var keptValueIds = inputs.Where(v => v.Id is not null).Select(v => v.Id!.Value).ToHashSet();

        foreach (var removed in group.Values.Where(v => !keptValueIds.Contains(v.Id)).ToList())
        {
            db.ProductOptionValues.Remove(removed);
        }

        for (var i = 0; i < inputs.Count; i++)
        {
            var valueInput = inputs[i];
            var value = valueInput.Id is { } valueId
                ? group.Values.FirstOrDefault(v => v.Id == valueId)
                : null;

            if (value is null)
            {
                group.Values.Add(new ProductOptionValue
                {
                    AttributeValueId = valueInput.AttributeValueId,
                    PriceAdjustment = valueInput.PriceAdjustment,
                    IsDefault = valueInput.IsDefault,
                    DisplayOrder = i
                });
            }
            else
            {
                value.AttributeValueId = valueInput.AttributeValueId;
                value.PriceAdjustment = valueInput.PriceAdjustment;
                value.IsDefault = valueInput.IsDefault;
                value.DisplayOrder = i;
            }
        }
    }

    /// <summary>
    /// Mỗi nhóm phải có ĐÚNG một giá trị mặc định — PDP dùng nó để chọn sẵn khi mở trang và
    /// CartService dùng khi khách không chọn gì. Không có cái nào ⇒ lấy giá trị đầu.
    /// </summary>
    private static void EnsureSingleDefault(ProductOptionGroup group)
    {
        var defaults = group.Values.Where(v => v.IsDefault).ToList();

        if (defaults.Count == 1)
        {
            return;
        }

        foreach (var value in group.Values)
        {
            value.IsDefault = false;
        }

        var chosen = defaults.FirstOrDefault() ?? group.Values.FirstOrDefault();
        if (chosen is not null)
        {
            chosen.IsDefault = true;
        }
    }

    public async Task<AdminResult> DeleteAsync(int id)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return AdminResult.Fail("Không tìm thấy sản phẩm.");
        }

        // Xóa MỀM: đơn hàng cũ giữ snapshot tên/giá nên lịch sử không mất (OrderItem.ProductId
        // vẫn trỏ tới bản ghi, chỉ là bị query filter ẩn đi)
        db.Products.Remove(product);
        await db.SaveChangesAsync();

        logger.LogInformation("Xóa mềm sản phẩm {Name} (id {Id})", product.Name, id);

        return AdminResult.Success($"Đã chuyển sản phẩm “{product.Name}” vào Thùng rác.");
    }

    public async Task<AdminResult> ToggleActiveAsync(int id)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return AdminResult.Fail("Không tìm thấy sản phẩm.");
        }

        product.IsActive = !product.IsActive;
        await db.SaveChangesAsync();

        return AdminResult.Success(product.IsActive
            ? $"Đã hiện “{product.Name}” trên website."
            : $"Đã ẩn “{product.Name}” khỏi website.");
    }

    public async Task<IReadOnlyList<ProductImageItem>> GetImagesAsync(int productId) =>
        await db.ProductImages.AsNoTracking()
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new ProductImageItem(i.Id, i.Url, i.DisplayOrder, i.IsMain))
            .ToListAsync();

    public async Task<AdminResult> AddImagesAsync(int productId, IReadOnlyList<string> urls)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId))
        {
            return AdminResult.Fail("Không tìm thấy sản phẩm.");
        }

        var existing = await db.ProductImages.Where(i => i.ProductId == productId).ToListAsync();
        var nextOrder = existing.Count == 0 ? 0 : existing.Max(i => i.DisplayOrder) + 1;
        var hasMain = existing.Any(i => i.IsMain);

        foreach (var url in urls)
        {
            db.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                Url = url,
                DisplayOrder = nextOrder++,
                // Ảnh đầu tiên tự thành ảnh chính — card sản phẩm luôn có ảnh, admin không phải nhớ bấm
                IsMain = !hasMain
            });

            hasMain = true;
        }

        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã thêm {urls.Count} ảnh.");
    }

    public async Task<(AdminResult Result, string? RemovedUrl)> DeleteImageAsync(int productId, int imageId)
    {
        var image = await db.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);

        if (image is null)
        {
            return (AdminResult.Fail("Không tìm thấy ảnh."), null);
        }

        var url = image.Url;
        var wasMain = image.IsMain;

        db.ProductImages.Remove(image); // ProductImage KHÔNG xóa mềm ⇒ xóa cứng thật
        await db.SaveChangesAsync();

        // Xóa mất ảnh chính thì phải chỉ định ảnh chính mới, không thì card sản phẩm mất ảnh
        if (wasMain)
        {
            var next = await db.ProductImages
                .Where(i => i.ProductId == productId)
                .OrderBy(i => i.DisplayOrder)
                .FirstOrDefaultAsync();

            if (next is not null)
            {
                next.IsMain = true;
                await db.SaveChangesAsync();
            }
        }

        return (AdminResult.Success("Đã xóa ảnh."), url);
    }

    public async Task<AdminResult> ReorderImagesAsync(int productId, int[] orderedIds)
    {
        var images = await db.ProductImages
            .Where(i => i.ProductId == productId && orderedIds.Contains(i.Id))
            .ToListAsync();

        for (var i = 0; i < orderedIds.Length; i++)
        {
            var image = images.FirstOrDefault(x => x.Id == orderedIds[i]);
            if (image is not null)
            {
                image.DisplayOrder = i;
            }
        }

        await db.SaveChangesAsync();

        return AdminResult.Success("Đã lưu thứ tự ảnh.");
    }

    public async Task<AdminResult> SetMainImageAsync(int productId, int imageId)
    {
        var images = await db.ProductImages.Where(i => i.ProductId == productId).ToListAsync();

        if (images.All(i => i.Id != imageId))
        {
            return AdminResult.Fail("Không tìm thấy ảnh.");
        }

        foreach (var image in images)
        {
            image.IsMain = image.Id == imageId;
        }

        await db.SaveChangesAsync();

        return AdminResult.Success("Đã đặt làm ảnh chính.");
    }
}
