using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;
using RenewZone.Web.Areas.Admin.Models;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// CRUD danh mục 2 cấp. Form theo mô hình PRG (POST → redirect → toast từ TempData)
/// nên F5 không tạo bản ghi trùng; chỉ kéo-thả và bật/tắt hiển thị dùng AJAX.
/// </summary>
public class CategoryController(ICategoryAdminService categories) : BaseAdminController
{
    [HttpGet("/admin/danh-muc")]
    public async Task<IActionResult> Index()
    {
        return View(await categories.GetTreeAsync());
    }

    [HttpGet("/admin/danh-muc/them")]
    public async Task<IActionResult> Create(int? parentId = null)
    {
        var model = new CategoryFormViewModel
        {
            ParentId = parentId,
            ParentOptions = await categories.GetParentOptionsAsync()
        };

        SetCrumb();
        return View("Form", model);
    }

    [HttpPost("/admin/danh-muc/them")]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(model);
        }

        var result = await categories.CreateAsync(model.ToInput());

        if (!result.Ok)
        {
            // Lỗi nghiệp vụ (slug trùng…) gắn vào ô Slug để hiện đúng chỗ, không phải báo chung chung
            ModelState.AddModelError(nameof(model.Slug), result.Message);
            return await RedisplayAsync(model);
        }

        ToastSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/admin/danh-muc/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await categories.GetAsync(id);

        if (category is null)
        {
            return NotFound();
        }

        var model = CategoryFormViewModel.FromEntity(category);
        model.ParentOptions = await categories.GetParentOptionsAsync(id);

        SetCrumb();
        return View("Form", model);
    }

    [HttpPost("/admin/danh-muc/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        model.Id = id; // id lấy từ ROUTE, không tin giá trị ẩn trong form

        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(model);
        }

        var result = await categories.UpdateAsync(model.ToInput());

        if (!result.Ok)
        {
            ModelState.AddModelError(nameof(model.Slug), result.Message);
            return await RedisplayAsync(model);
        }

        ToastSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/danh-muc/{id:int}/xoa")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await categories.DeleteAsync(id);

        if (result.Ok)
        {
            ToastSuccess(result.Message);
        }
        else
        {
            ToastError(result.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/danh-muc/{id:int}/toggle")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await categories.ToggleActiveAsync(id);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    [HttpPost("/admin/danh-muc/sap-xep")]
    public async Task<IActionResult> Reorder([FromBody] ReorderInput? input)
    {
        if (input?.Ids is null || input.Ids.Length == 0)
        {
            return BadRequest(new { ok = false, message = "Dữ liệu sắp xếp không hợp lệ." });
        }

        var result = await categories.ReorderAsync(input.Ids);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    private async Task<IActionResult> RedisplayAsync(CategoryFormViewModel model)
    {
        model.ParentOptions = await categories.GetParentOptionsAsync(model.Id);
        SetCrumb();
        return View("Form", model);
    }

    private void SetCrumb()
    {
        ViewData["Crumb"] = "Danh mục";
        ViewData["CrumbUrl"] = Url.Action(nameof(Index));
    }

    public record ReorderInput(int[] Ids);
}
