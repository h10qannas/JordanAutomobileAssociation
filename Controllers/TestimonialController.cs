using JAA.Models;
using JAA.Resources;
using JAA.Services;
using JAA.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace JAA.Controllers
{
    [Authorize(Roles = "Customer")]
    public class TestimonialController : Controller
    {
        private readonly TestimonialService _testimonialService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResources> _l;

        public TestimonialController(
            TestimonialService testimonialService,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResources> localizer)
        {
            _testimonialService = testimonialService;
            _userManager        = userManager;
            _l                  = localizer;
        }

        // GET /Testimonial/My
        public async Task<IActionResult> My()
        {
            var userId = _userManager.GetUserId(User)!;
            var vm     = await _testimonialService.GetMyTestimonialsAsync(userId);
            return View(vm);
        }

        // GET /Testimonial/Submit/{requestId}
        [HttpGet]
        public async Task<IActionResult> Submit(int requestId)
        {
            var userId = _userManager.GetUserId(User)!;

            if (!await _testimonialService.CanSubmitAsync(userId, requestId))
            {
                TempData["Error"] = "You cannot submit a testimonial for this request.";
                return RedirectToAction(nameof(My));
            }

            // Build the display VM from the request
            var myVm = await _testimonialService.GetMyTestimonialsAsync(userId);
            var req  = myVm.EligibleRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (req == null)
            {
                TempData["Error"] = "Request not found or already has a testimonial.";
                return RedirectToAction(nameof(My));
            }

            var vm = new SubmitTestimonialViewModel
            {
                ServiceRequestId     = requestId,
                ShopId               = req.ShopId,
                MechanicId           = req.MechanicId,
                ShopName             = req.ShopName,
                MechanicName         = req.MechanicName,
                SituationDescription = req.SituationDescription,
                ServiceDate          = req.CompletedAt,
                Rating               = 5
            };
            return View(vm);
        }

        // POST /Testimonial/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(SubmitTestimonialViewModel vm)
        {
            var userId = _userManager.GetUserId(User)!;

            if (!ModelState.IsValid) return View(vm);

            var (ok, error) = await _testimonialService.SubmitAsync(vm, userId);
            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(My));
            }

            TempData["Success"] = "Thank you! Your testimonial has been submitted for review.";
            return RedirectToAction(nameof(My));
        }

        // GET /Testimonial/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var vm     = await _testimonialService.GetForEditAsync(id, userId);

            if (vm == null)
            {
                TempData["Error"] = "Testimonial not found or cannot be edited after approval.";
                return RedirectToAction(nameof(My));
            }
            return View(vm);
        }

        // POST /Testimonial/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditTestimonialViewModel vm)
        {
            var userId = _userManager.GetUserId(User)!;

            if (!ModelState.IsValid) return View(vm);

            var (ok, error) = await _testimonialService.EditAsync(vm, userId);
            if (!ok)
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(My));
            }

            TempData["Success"] = "Your testimonial has been updated.";
            return RedirectToAction(nameof(My));
        }
    }
}
