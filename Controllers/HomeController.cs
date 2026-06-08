using JAA.Models;
using JAA.Services;
using JAA.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;

namespace JAA.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ShopService _shopService;
        private readonly TestimonialService _testimonialService;

        public HomeController(ILogger<HomeController> logger, ShopService shopService, TestimonialService testimonialService)
        {
            _logger             = logger;
            _shopService        = shopService;
            _testimonialService = testimonialService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))     return RedirectToAction("Dashboard", "Admin", new { area = "Admin" });
                if (User.IsInRole("ShopOwner")) return RedirectToAction("Dashboard", "Shop");
                return RedirectToAction("Dashboard", "Customer");
            }

            var vm = new HomeIndexViewModel();
            try
            {
                vm.Testimonials = await _testimonialService.GetHomepageTestimonialsAsync(9);
                var (avg, totalT, totalC) = await _testimonialService.GetPlatformStatsAsync();
                vm.AvgRating              = avg;
                vm.TotalTestimonials      = totalT;
                vm.TotalCompletedServices = totalC;
            }
            catch { /* Testimonials table may not exist yet — render page without it */ }

            return View(vm);
        }

        public async Task<IActionResult> Shops(
            string? city,
            double? lat,
            double? lng)
        {
            var shops  = await _shopService.GetNearbyShopsAsync(lat, lng, city);
            var cities = (await _shopService.GetNearbyShopsAsync(null, null))
                .Select(s => s.Shop.City).Distinct().OrderBy(c => c).ToList();

            var vm = new ShopsViewModel
            {
                Shops        = shops,
                SelectedCity = city,
                Cities       = cities,
                CustomerLat  = lat,
                CustomerLng  = lng
            };
            return View(vm);
        }

        /// <summary>
        /// AJAX endpoint — returns shops as JSON sorted by distance from given coordinates.
        /// Called by the Shops page when the user allows geolocation.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> NearbyShopsJson(double lat, double lng, string? city = null)
        {
            var shops = await _shopService.GetNearbyShopsAsync(lat, lng, city);
            var result = shops.Select(ns => new
            {
                id                  = ns.Shop.Id,
                name                = ns.Shop.ShopName,
                city                = ns.Shop.City,
                phone               = ns.Shop.PhoneNumber ?? "",
                logoUrl             = ns.Shop.LogoUrl,
                description         = ns.Shop.Description,
                isAvailable         = ns.IsAvailable,
                avgRating           = Math.Round(ns.AvgRating, 1),
                reviewCount         = ns.ReviewCount,
                availableMechanics  = ns.AvailableMechanics,
                distanceKm          = ns.DistanceKm.HasValue ? Math.Round(ns.DistanceKm.Value, 1) : (double?)null,
                distanceLabel       = ns.DistanceLabel,
                etaMin              = ns.EstimatedArrivalMin,
                etaLabel            = ns.EtaLabel,
                lat                 = ns.Shop.Latitude,
                lng                 = ns.Shop.Longitude,
                isVerified          = ns.Shop.IsVerified
            });
            return Json(result);
        }

        public async Task<IActionResult> ShopDetail(int id)
        {
            var shop = await _shopService.GetShopByIdAsync(id);
            if (shop == null) return NotFound();

            var completed     = await _shopService.GetCompletedJobsAsync(id);
            var avgRating     = await _shopService.GetAverageRatingAsync(id);
            var testimonials  = await _testimonialService.GetShopTestimonialsAsync(id);

            var vm = new ShopDetailViewModel
            {
                Shop          = shop,
                Feedbacks     = shop.Feedbacks.OrderByDescending(f => f.CreatedAt).ToList(),
                AverageRating = Math.Round(avgRating, 1),
                CompletedJobs = completed.Count,
                Testimonials  = testimonials
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
