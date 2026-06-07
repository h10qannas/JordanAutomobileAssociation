using JAA.Models;
using JAA.Services;
using JAA.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace JAA.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ShopService _shopService;

        public HomeController(ILogger<HomeController> logger, ShopService shopService)
        {
            _logger      = logger;
            _shopService = shopService;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))     return RedirectToAction("Dashboard", "Admin", new { area = "Admin" });
                if (User.IsInRole("ShopOwner")) return RedirectToAction("Dashboard", "Shop");
                return RedirectToAction("Dashboard", "Customer");
            }
            return View();
        }

        public async Task<IActionResult> Shops(string? city)
        {
            var shops  = await _shopService.GetVerifiedShopsAsync(city);
            var cities = (await _shopService.GetVerifiedShopsAsync())
                .Select(s => s.City).Distinct().OrderBy(c => c).ToList();

            var vm = new ShopsViewModel
            {
                Shops        = shops,
                SelectedCity = city,
                Cities       = cities
            };
            return View(vm);
        }

        public async Task<IActionResult> ShopDetail(int id)
        {
            var shop = await _shopService.GetShopByIdAsync(id);
            if (shop == null) return NotFound();

            var completed = await _shopService.GetCompletedJobsAsync(id);
            var avgRating = await _shopService.GetAverageRatingAsync(id);

            var vm = new ShopDetailViewModel
            {
                Shop          = shop,
                Feedbacks     = shop.Feedbacks.OrderByDescending(f => f.CreatedAt).ToList(),
                AverageRating = Math.Round(avgRating, 1),
                CompletedJobs = completed.Count
            };
            return View(vm);
        }

        public IActionResult About()   => View();
        public IActionResult Contact() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
