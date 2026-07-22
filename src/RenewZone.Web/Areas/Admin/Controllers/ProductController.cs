using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Web.Areas.Admin.Models;
using RenewZone.Web.Services;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// CRUD sản phẩm. Form chính là POST/PRG như web khách; chỉ ảnh (upload/sắp xếp/xóa/đặt ảnh chính)
/// và toggle hiển thị dùng AJAX — những thao tác không đáng phải tải lại cả form 5 tab.
/// </summary>
public class ProductController(
    IProductAdminService products,
    ICategoryAdminService categories,
    IMediaService media) : BaseAdminController
{
    [HttpGet("/admin/san-pham")]
    public async Task<IActionResult> Index(
        int? danhMuc = null,
        string? trangThai = null,
        string? coAnh = null,
        string? coMoTa = null,
        string? q = null,
        string? sort = null,
        bool desc = false,
        int page = 1)
    {
        // Querystring dùng tiếng Việt cho dễ đọc: "hien"/"an" thay vì true/false
        bool? isActive = trangThai switch { "hien" => true, "an" => false, _ => null };
        bool? hasImage = coAnh switch { "co" => true, "khong" => false, _ => null };
        bool? hasDescription = coMoTa switch { "co" => true, "khong" => false, _ => null };

        var result = await products.GetListAsync(new ProductListRequest
        {
            CategoryId = danhMuc,
            IsActive = isActive,
            HasImage = hasImage,
            HasDescription = hasDescription,
            Search = q,
            SortBy = sort,
            SortDesc = desc,
            Page = page
        });

        return View(new ProductListViewModel
        {
            Result = result,
            CategoryOptions = await BuildCategoryOptionsAsync(),
            CategoryId = danhMuc,
            IsActive = isActive,
            HasImage = hasImage,
            HasDescription = hasDescription,
            Search = q,
            SortBy = sort,
            SortDesc = desc
        });
    }

    [HttpGet("/admin/san-pham/them")]
    public async Task<IActionResult> Create()
    {
        var model = new ProductFormViewModel
        {
            CategoryOptions = await BuildCategoryOptionsAsync()
        };

        SetCrumb();
        return View("Form", model);
    }

    [HttpPost("/admin/san-pham/them")]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(model);
        }

        var result = await products.CreateAsync(model.ToInput());

        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return await RedisplayAsync(model);
        }

        // Sang thẳng form Sửa: sản phẩm mới chưa có ảnh, mà upload ảnh cần Id đã tồn tại
        ToastSuccess(result.Message + " Hãy thêm ảnh cho sản phẩm.");
        return RedirectToAction(nameof(Edit), new { id = result.Id });
    }

    [HttpGet("/admin/san-pham/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await products.GetForEditAsync(id);

        if (product is null)
        {
            return NotFound();
        }

        var model = ProductFormViewModel.FromEntity(product);
        model.CategoryOptions = await BuildCategoryOptionsAsync();

        SetCrumb();
        return View("Form", model);
    }

    [HttpPost("/admin/san-pham/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        model.Id = id; // lấy từ ROUTE, không tin field ẩn trong form

        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(model);
        }

        var result = await products.UpdateAsync(model.ToInput());

        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return await RedisplayAsync(model);
        }

        ToastSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/san-pham/{id:int}/xoa")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await products.DeleteAsync(id);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/san-pham/{id:int}/toggle")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await products.ToggleActiveAsync(id);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    // ===================== Ảnh =====================

    [HttpPost("/admin/san-pham/{id:int}/anh")]
    public async Task<IActionResult> UploadImages(int id, List<IFormFile> files)
    {
        if (files.Count == 0)
        {
            return BadRequest(new { ok = false, message = "Chưa chọn file nào." });
        }

        var saved = await media.SaveManyAsync(files, UploadProfile.Product);

        var okUrls = saved.Where(r => r.Ok).Select(r => r.RelativePath!).ToList();
        var errors = saved.Where(r => !r.Ok)
            .Select(r => $"{r.OriginalFileName}: {r.Error}")
            .ToList();

        if (okUrls.Count > 0)
        {
            await products.AddImagesAsync(id, okUrls);
        }

        // File nào lỗi thì chỉ mình nó lỗi — các file còn lại vẫn được lưu
        return Json(new
        {
            ok = okUrls.Count > 0,
            message = errors.Count == 0
                ? $"Đã tải lên {okUrls.Count} ảnh."
                : $"Đã tải lên {okUrls.Count} ảnh. {errors.Count} file bị từ chối: {string.Join("; ", errors)}",
            images = await products.GetImagesAsync(id)
        });
    }

    [HttpPost("/admin/san-pham/{id:int}/anh/sap-xep")]
    public async Task<IActionResult> ReorderImages(int id, [FromBody] ReorderInput? input)
    {
        if (input?.Ids is null || input.Ids.Length == 0)
        {
            return BadRequest(new { ok = false, message = "Dữ liệu sắp xếp không hợp lệ." });
        }

        var result = await products.ReorderImagesAsync(id, input.Ids);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    [HttpPost("/admin/san-pham/{id:int}/anh/{imageId:int}/xoa")]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var (result, removedUrl) = await products.DeleteImageAsync(id, imageId);

        // Chỉ xóa file vật lý SAU khi DB đã gỡ bản ghi thành công — ngược lại sẽ có bản ghi
        // trỏ tới file không còn tồn tại (ảnh vỡ trên web khách)
        if (result.Ok)
        {
            media.Delete(removedUrl);
        }

        return Json(new
        {
            ok = result.Ok,
            message = result.Message,
            images = await products.GetImagesAsync(id)
        });
    }

    [HttpPost("/admin/san-pham/{id:int}/anh/{imageId:int}/chinh")]
    public async Task<IActionResult> SetMainImage(int id, int imageId)
    {
        var result = await products.SetMainImageAsync(id, imageId);

        return Json(new
        {
            ok = result.Ok,
            message = result.Message,
            images = await products.GetImagesAsync(id)
        });
    }

    // ===================== Helper =====================

    /// <summary>Danh sách chọn danh mục 2 cấp (cha rồi tới các dòng con, thụt lề theo Depth).</summary>
    private async Task<IReadOnlyList<CategoryOption>> BuildCategoryOptionsAsync()
    {
        var tree = await categories.GetTreeAsync();
        var options = new List<CategoryOption>();

        foreach (var parent in tree)
        {
            options.Add(new CategoryOption(parent.Id, parent.Name, 0));
            options.AddRange(parent.Children.Select(c => new CategoryOption(c.Id, c.Name, 1)));
        }

        return options;
    }

    private async Task<IActionResult> RedisplayAsync(ProductFormViewModel model)
    {
        model.CategoryOptions = await BuildCategoryOptionsAsync();

        // Ảnh không nằm trong form (quản lý bằng AJAX riêng) nên phải nạp lại,
        // nếu không thì mỗi lần validate hỏng là tab Hình ảnh trông như mất sạch ảnh
        if (model.Id is { } id)
        {
            model.Images = await products.GetImagesAsync(id);
        }

        SetCrumb();
        return View("Form", model);
    }

    private void SetCrumb()
    {
        ViewData["Crumb"] = "Sản phẩm";
        ViewData["CrumbUrl"] = Url.Action(nameof(Index));
    }

    public record ReorderInput(int[] Ids);
}
