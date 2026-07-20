using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;
using RenewZone.Web.Models;
using RenewZone.Web.Models.ViewModels;

namespace RenewZone.Web.Controllers;

public class HomeController : Controller
{
    private readonly IHomeQueryService _home;

    public HomeController(IHomeQueryService home)
    {
        _home = home;
    }

    public async Task<IActionResult> Index()
    {
        var content = await _home.GetHomeContentAsync();

        var vm = new HomeIndexViewModel
        {
            HeroBanners = content.HeroBanners,
            SideBanners = content.SideBanners,
            Categories = content.Categories,
            CategorySections = content.CategorySections,
            FeaturedProducts = content.FeaturedProducts,
            BestSellers = content.BestSellers,
            Testimonials = content.Testimonials,
            LatestNews = content.LatestNews
        };

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
