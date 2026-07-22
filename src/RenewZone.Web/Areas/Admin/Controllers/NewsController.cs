using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Web.Areas.Admin.Models;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>CRUD bài viết "Tin công nghệ".</summary>
public class NewsController(INewsAdminService news) : BaseAdminController
{
    [HttpGet("/admin/bai-viet")]
    public async Task<IActionResult> Index(
        int? chuyenMuc = null, string? trangThai = null, string? q = null, int page = 1)
    {
        bool? isActive = trangThai switch { "hien" => true, "an" => false, _ => null };

        var result = await news.GetListAsync(new NewsListRequest
        {
            CategoryId = chuyenMuc,
            IsActive = isActive,
            Search = q,
            Page = page,
            PageSize = 20
        });

        return View(new NewsListViewModel
        {
            Result = result,
            Categories = await news.GetCategoriesAsync(),
            CategoryId = chuyenMuc,
            IsActive = isActive,
            Search = q
        });
    }

    [HttpGet("/admin/bai-viet/them")]
    public async Task<IActionResult> Create()
    {
        var model = new NewsFormViewModel { Categories = await news.GetCategoriesAsync() };
        SetCrumb();
        return View("Form", model);
    }

    [HttpPost("/admin/bai-viet/them")]
    public async Task<IActionResult> Create(NewsFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(model);
        }

        var result = await news.CreateAsync(model.ToInput());

        if (!result.Ok)
        {
            ModelState.AddModelError(nameof(model.Slug), result.Message);
            return await RedisplayAsync(model);
        }

        ToastSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/admin/bai-viet/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await news.GetAsync(id);

        if (article is null)
        {
            return NotFound();
        }

        var model = NewsFormViewModel.FromEntity(article);
        model.Categories = await news.GetCategoriesAsync();

        SetCrumb();
        return View("Form", model);
    }

    [HttpPost("/admin/bai-viet/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id, NewsFormViewModel model)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(model);
        }

        var result = await news.UpdateAsync(model.ToInput());

        if (!result.Ok)
        {
            ModelState.AddModelError(nameof(model.Slug), result.Message);
            return await RedisplayAsync(model);
        }

        ToastSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/bai-viet/{id:int}/xoa")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await news.DeleteAsync(id);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/bai-viet/{id:int}/toggle")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await news.ToggleActiveAsync(id);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    private async Task<IActionResult> RedisplayAsync(NewsFormViewModel model)
    {
        model.Categories = await news.GetCategoriesAsync();
        SetCrumb();
        return View("Form", model);
    }

    private void SetCrumb()
    {
        ViewData["Crumb"] = "Bài viết";
        ViewData["CrumbUrl"] = Url.Action(nameof(Index));
    }
}

/// <summary>CRUD chuyên mục tin — màn đơn giản nên form nằm ngay trên trang danh sách.</summary>
public class NewsCategoryController(INewsAdminService news) : BaseAdminController
{
    [HttpGet("/admin/chuyen-muc-tin")]
    public async Task<IActionResult> Index()
    {
        return View(await news.GetCategoriesAsync());
    }

    [HttpPost("/admin/chuyen-muc-tin/luu")]
    public async Task<IActionResult> Save(NewsCategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ToastError("Dữ liệu chưa hợp lệ — kiểm tra lại tên và slug.");
            return RedirectToAction(nameof(Index));
        }

        var result = await news.SaveCategoryAsync(model.ToInput());

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/chuyen-muc-tin/{id:int}/xoa")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await news.DeleteCategoryAsync(id);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/chuyen-muc-tin/sap-xep")]
    public async Task<IActionResult> Reorder([FromBody] ReorderInput? input)
    {
        if (input?.Ids is null || input.Ids.Length == 0)
        {
            return BadRequest(new { ok = false, message = "Dữ liệu sắp xếp không hợp lệ." });
        }

        var result = await news.ReorderCategoriesAsync(input.Ids);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    public record ReorderInput(int[] Ids);
}
