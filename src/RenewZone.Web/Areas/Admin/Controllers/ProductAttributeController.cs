using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;
using RenewZone.Web.Areas.Admin.Models;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// Danh sách chung các thuộc tính biến thể (Giai đoạn 10). Màn đơn giản nên form nằm ngay trên
/// trang danh sách, giống màn Chuyên mục tin.
/// </summary>
public class ProductAttributeController(IProductAttributeAdminService attributes) : BaseAdminController
{
    [HttpGet("/admin/thuoc-tinh")]
    public async Task<IActionResult> Index()
    {
        ViewData["Crumb"] = "Sản phẩm";
        ViewData["CrumbUrl"] = Url.Action("Index", "Product");
        return View(await attributes.GetAllAsync());
    }

    [HttpPost("/admin/thuoc-tinh/luu")]
    public async Task<IActionResult> Save(AttributeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ToastError("Tên thuộc tính không hợp lệ (bắt buộc, tối đa 100 ký tự).");
            return RedirectToAction(nameof(Index));
        }

        Toast(await attributes.SaveAttributeAsync(model.ToInput()));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/thuoc-tinh/{id:int}/xoa")]
    public async Task<IActionResult> Delete(int id)
    {
        Toast(await attributes.DeleteAttributeAsync(id));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/thuoc-tinh/gia-tri/luu")]
    public async Task<IActionResult> SaveValue(AttributeValueFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ToastError("Giá trị không hợp lệ (bắt buộc, tối đa 100 ký tự).");
            return RedirectToAction(nameof(Index));
        }

        Toast(await attributes.SaveValueAsync(model.ToInput()));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/thuoc-tinh/gia-tri/{id:int}/xoa")]
    public async Task<IActionResult> DeleteValue(int id)
    {
        Toast(await attributes.DeleteValueAsync(id));
        return RedirectToAction(nameof(Index));
    }

    private void Toast(Domain.Models.Admin.AdminResult result)
    {
        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }
    }
}
