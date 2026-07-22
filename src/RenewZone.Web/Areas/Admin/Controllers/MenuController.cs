using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Web.Areas.Admin.Models;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>CRUD menu 2 cấp cho 3 nhóm: menu chính + 2 cột link ở footer.</summary>
public class MenuController(INavigationAdminService menus) : BaseAdminController
{
    [HttpGet("/admin/menu")]
    public async Task<IActionResult> Index(MenuGroup nhom = MenuGroup.MainMenu)
    {
        var counts = new Dictionary<MenuGroup, int>();

        foreach (var group in Enum.GetValues<MenuGroup>())
        {
            var tree = await menus.GetTreeAsync(group);
            counts[group] = tree.Count + tree.Sum(n => n.Children.Count);
        }

        return View(new MenuIndexViewModel
        {
            Group = nhom,
            Tree = await menus.GetTreeAsync(nhom),
            Counts = counts
        });
    }

    [HttpGet("/admin/menu/them")]
    public async Task<IActionResult> Create(MenuGroup nhom = MenuGroup.MainMenu, int? parentId = null)
    {
        var model = new MenuFormViewModel
        {
            Group = nhom,
            ParentId = parentId,
            ParentOptions = await menus.GetParentOptionsAsync(nhom)
        };

        SetCrumb();
        return View("Form", model);
    }

    [HttpPost("/admin/menu/them")]
    public Task<IActionResult> Create(MenuFormViewModel model) => SaveAsync(model);

    [HttpGet("/admin/menu/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await menus.GetAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        var model = MenuFormViewModel.FromEntity(item);
        model.ParentOptions = await menus.GetParentOptionsAsync(item.Group, id);

        SetCrumb();
        return View("Form", model);
    }

    [HttpPost("/admin/menu/{id:int}/sua")]
    public Task<IActionResult> Edit(int id, MenuFormViewModel model)
    {
        model.Id = id;
        return SaveAsync(model);
    }

    [HttpPost("/admin/menu/{id:int}/xoa")]
    public async Task<IActionResult> Delete(int id, MenuGroup nhom = MenuGroup.MainMenu)
    {
        var result = await menus.DeleteAsync(id);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index), new { nhom });
    }

    [HttpPost("/admin/menu/{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var result = await menus.ToggleActiveAsync(id);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    [HttpPost("/admin/menu/sap-xep")]
    public async Task<IActionResult> Reorder([FromBody] ReorderInput? input)
    {
        if (input?.Ids is null || input.Ids.Length == 0)
        {
            return BadRequest(new { ok = false, message = "Dữ liệu sắp xếp không hợp lệ." });
        }

        var result = await menus.ReorderAsync(input.Ids);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    private async Task<IActionResult> SaveAsync(MenuFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await RedisplayAsync(model);
        }

        var result = await menus.SaveAsync(model.ToInput());

        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return await RedisplayAsync(model);
        }

        ToastSuccess(result.Message);
        return RedirectToAction(nameof(Index), new { nhom = model.Group });
    }

    private async Task<IActionResult> RedisplayAsync(MenuFormViewModel model)
    {
        model.ParentOptions = await menus.GetParentOptionsAsync(model.Group, model.Id);
        SetCrumb();
        return View("Form", model);
    }

    private void SetCrumb()
    {
        ViewData["Crumb"] = "Menu";
        ViewData["CrumbUrl"] = Url.Action(nameof(Index));
    }

    public record ReorderInput(int[] Ids);
}

/// <summary>CRUD khối nội dung theo GroupKey (cam kết PDP, trấn an giỏ, kênh liên hệ).</summary>
public class ContentBlockController(IContentBlockAdminService blocks) : BaseAdminController
{
    [HttpGet("/admin/khoi-noi-dung")]
    public async Task<IActionResult> Index(string? nhom = null)
    {
        var groupKey = string.IsNullOrWhiteSpace(nhom)
            ? ContentBlockGroups.ProductCommitments
            : nhom;

        // Nhóm lạ trong querystring ⇒ về nhóm mặc định thay vì trả trang rỗng khó hiểu
        if (ContentBlockIndexViewModel.Groups.All(g => g.Key != groupKey))
        {
            groupKey = ContentBlockGroups.ProductCommitments;
        }

        return View(new ContentBlockIndexViewModel
        {
            GroupKey = groupKey,
            Blocks = await blocks.GetByGroupAsync(groupKey)
        });
    }

    [HttpGet("/admin/khoi-noi-dung/them")]
    public IActionResult Create(string nhom)
    {
        SetCrumb();
        return View("Form", new ContentBlockFormViewModel { GroupKey = nhom });
    }

    [HttpPost("/admin/khoi-noi-dung/them")]
    public Task<IActionResult> Create(ContentBlockFormViewModel model) => SaveAsync(model);

    [HttpGet("/admin/khoi-noi-dung/{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var block = await blocks.GetAsync(id);

        if (block is null)
        {
            return NotFound();
        }

        SetCrumb();
        return View("Form", ContentBlockFormViewModel.FromEntity(block));
    }

    [HttpPost("/admin/khoi-noi-dung/{id:int}/sua")]
    public Task<IActionResult> Edit(int id, ContentBlockFormViewModel model)
    {
        model.Id = id;
        return SaveAsync(model);
    }

    [HttpPost("/admin/khoi-noi-dung/{id:int}/xoa")]
    public async Task<IActionResult> Delete(int id, string nhom)
    {
        var result = await blocks.DeleteAsync(id);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index), new { nhom });
    }

    [HttpPost("/admin/khoi-noi-dung/{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var result = await blocks.ToggleActiveAsync(id);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    [HttpPost("/admin/khoi-noi-dung/sap-xep")]
    public async Task<IActionResult> Reorder([FromBody] ReorderInput? input)
    {
        if (input?.Ids is null || input.Ids.Length == 0)
        {
            return BadRequest(new { ok = false, message = "Dữ liệu sắp xếp không hợp lệ." });
        }

        var result = await blocks.ReorderAsync(input.Ids);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    private async Task<IActionResult> SaveAsync(ContentBlockFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            SetCrumb();
            return View("Form", model);
        }

        var result = await blocks.SaveAsync(model.ToInput());

        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            SetCrumb();
            return View("Form", model);
        }

        ToastSuccess(result.Message);
        return RedirectToAction(nameof(Index), new { nhom = model.GroupKey });
    }

    private void SetCrumb()
    {
        ViewData["Crumb"] = "Khối nội dung";
        ViewData["CrumbUrl"] = Url.Action(nameof(Index));
    }

    public record ReorderInput(int[] Ids);
}
