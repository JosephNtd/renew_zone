using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;
using RenewZone.Web.Areas.Admin.Models;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>CRUD trang tĩnh (giới thiệu, chính sách, sửa chữa…).</summary>
public class PageController(IPageAdminService pages) : BaseAdminController
{
    [HttpGet("/admin/trang-tinh")]
    public async Task<IActionResult> Index()
    {
        return View(await pages.GetPagesAsync());
    }

    [HttpGet("/admin/trang-tinh/them")]
    public IActionResult Create()
    {
        SetCrumb();
        return View("Form", new StaticPageFormViewModel());
    }

    [HttpPost("/admin/trang-tinh/them")]
    public async Task<IActionResult> Create(StaticPageFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            SetCrumb();
            return View("Form", model);
        }

        var result = await pages.SavePageAsync(model.ToInput());

        if (!result.Ok)
        {
            ModelState.AddModelError(nameof(model.Slug), result.Message);
            SetCrumb();
            return View("Form", model);
        }

        ToastSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/admin/trang-tinh/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var page = await pages.GetPageAsync(id);

        if (page is null)
        {
            return NotFound();
        }

        SetCrumb();
        return View("Form", StaticPageFormViewModel.FromEntity(page));
    }

    [HttpPost("/admin/trang-tinh/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id, StaticPageFormViewModel model)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            SetCrumb();
            return View("Form", model);
        }

        var result = await pages.SavePageAsync(model.ToInput());

        if (!result.Ok)
        {
            ModelState.AddModelError(nameof(model.Slug), result.Message);
            SetCrumb();
            return View("Form", model);
        }

        ToastSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/trang-tinh/{id:int}/xoa")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await pages.DeletePageAsync(id);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index));
    }

    private void SetCrumb()
    {
        ViewData["Crumb"] = "Trang tĩnh";
        ViewData["CrumbUrl"] = Url.Action(nameof(Index));
    }
}

/// <summary>CRUD banner, nhóm theo vị trí hiển thị trên website.</summary>
public class BannerController(IPageAdminService pages) : BaseAdminController
{
    [HttpGet("/admin/banner")]
    public async Task<IActionResult> Index()
    {
        return View(await pages.GetBannersAsync());
    }

    [HttpGet("/admin/banner/them")]
    public IActionResult Create()
    {
        SetCrumb();
        return View("Form", new BannerFormViewModel());
    }

    [HttpPost("/admin/banner/them")]
    public Task<IActionResult> Create(BannerFormViewModel model) => SaveAsync(model);

    [HttpGet("/admin/banner/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var banner = await pages.GetBannerAsync(id);

        if (banner is null)
        {
            return NotFound();
        }

        SetCrumb();
        return View("Form", BannerFormViewModel.FromEntity(banner));
    }

    [HttpPost("/admin/banner/{id:int}/sua")]
    public Task<IActionResult> Edit(int id, BannerFormViewModel model)
    {
        model.Id = id;
        return SaveAsync(model);
    }

    [HttpPost("/admin/banner/{id:int}/xoa")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await pages.DeleteBannerAsync(id);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/banner/{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var result = await pages.ToggleBannerAsync(id);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    [HttpPost("/admin/banner/sap-xep")]
    public async Task<IActionResult> Reorder([FromBody] ReorderInput? input)
    {
        if (input?.Ids is null || input.Ids.Length == 0)
        {
            return BadRequest(new { ok = false, message = "Dữ liệu sắp xếp không hợp lệ." });
        }

        var result = await pages.ReorderBannersAsync(input.Ids);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    private async Task<IActionResult> SaveAsync(BannerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            SetCrumb();
            return View("Form", model);
        }

        var result = await pages.SaveBannerAsync(model.ToInput());

        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            SetCrumb();
            return View("Form", model);
        }

        ToastSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    private void SetCrumb()
    {
        ViewData["Crumb"] = "Banner";
        ViewData["CrumbUrl"] = Url.Action(nameof(Index));
    }

    public record ReorderInput(int[] Ids);
}

/// <summary>CRUD đánh giá khách hàng (carousel trang chủ).</summary>
public class TestimonialController(IPageAdminService pages) : BaseAdminController
{
    [HttpGet("/admin/danh-gia")]
    public async Task<IActionResult> Index()
    {
        return View(await pages.GetTestimonialsAsync());
    }

    [HttpGet("/admin/danh-gia/them")]
    public IActionResult Create()
    {
        SetCrumb();
        return View("Form", new TestimonialFormViewModel());
    }

    [HttpPost("/admin/danh-gia/them")]
    public Task<IActionResult> Create(TestimonialFormViewModel model) => SaveAsync(model);

    [HttpGet("/admin/danh-gia/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var testimonial = await pages.GetTestimonialAsync(id);

        if (testimonial is null)
        {
            return NotFound();
        }

        SetCrumb();
        return View("Form", TestimonialFormViewModel.FromEntity(testimonial));
    }

    [HttpPost("/admin/danh-gia/{id:int}/sua")]
    public Task<IActionResult> Edit(int id, TestimonialFormViewModel model)
    {
        model.Id = id;
        return SaveAsync(model);
    }

    [HttpPost("/admin/danh-gia/{id:int}/xoa")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await pages.DeleteTestimonialAsync(id);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/danh-gia/{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var result = await pages.ToggleTestimonialAsync(id);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    private async Task<IActionResult> SaveAsync(TestimonialFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            SetCrumb();
            return View("Form", model);
        }

        var result = await pages.SaveTestimonialAsync(model.ToInput());

        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            SetCrumb();
            return View("Form", model);
        }

        ToastSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    private void SetCrumb()
    {
        ViewData["Crumb"] = "Đánh giá";
        ViewData["CrumbUrl"] = Url.Action(nameof(Index));
    }
}
