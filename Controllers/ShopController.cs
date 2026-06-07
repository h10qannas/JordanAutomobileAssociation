using JAA.Data;
using JAA.Models;
using JAA.Resources;
using JAA.Services;
using JAA.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace JAA.Controllers
{
    [Authorize(Roles = "ShopOwner")]
    public class ShopController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ShopService _shopService;
        private readonly MechanicService _mechanicService;
        private readonly PaymentService _paymentService;
        private readonly InvoiceService _invoiceService;
        private readonly IWebHostEnvironment _env;
        private readonly IStringLocalizer<SharedResources> _l;

        public ShopController(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            ShopService shopService,
            MechanicService mechanicService,
            PaymentService paymentService,
            InvoiceService invoiceService,
            IWebHostEnvironment env,
            IStringLocalizer<SharedResources> localizer)
        {
            _db              = db;
            _userManager     = userManager;
            _shopService     = shopService;
            _mechanicService = mechanicService;
            _paymentService  = paymentService;
            _invoiceService  = invoiceService;
            _env             = env;
            _l               = localizer;
        }

        private async Task<RepairShop?> GetCurrentShopAsync()
        {
            var userId = _userManager.GetUserId(User)!;
            return await _shopService.GetShopByOwnerAsync(userId);
        }

        private IActionResult RequirePendingApproval() =>
            View("PendingApproval");

        public async Task<IActionResult> Dashboard()
        {
            var shop = await GetCurrentShopAsync();
            if (shop == null) return RedirectToAction("ManageShop");

            if (shop.ShopStatus == ShopStatus.Pending)
                return View("PendingApproval", shop);
            if (shop.ShopStatus == ShopStatus.Rejected)
                return View("Rejected", shop);

            var today     = DateTime.UtcNow.Date;
            var incoming  = await _shopService.GetIncomingRequestsAsync();
            var active    = await _shopService.GetActiveJobsAsync(shop.Id);
            var completed = await _shopService.GetCompletedJobsAsync(shop.Id);
            var avgRating = await _shopService.GetAverageRatingAsync(shop.Id);
            var reviews   = await _db.Feedbacks.CountAsync(f => f.ShopId == shop.Id);

            var mechanics         = shop.Mechanics;
            var approvedMechanics = mechanics.Count(m => m.Status == MechanicStatus.Approved);
            var availableMechanics = mechanics.Count(m => m.Status == MechanicStatus.Approved && m.IsAvailable);

            var inspectionEarnings = await _paymentService.GetShopTotalInspectionEarningsAsync(shop.Id);
            var repairEarnings     = await _paymentService.GetShopTotalRepairEarningsAsync(shop.Id);

            var vm = new ShopDashboardViewModel
            {
                Shop               = shop,
                PendingRequests    = incoming.Count,
                ActiveJobs         = active.Count,
                CompletedToday     = completed.Count(r => r.UpdatedAt.Date == today),
                TotalEarnings      = inspectionEarnings + repairEarnings,
                AverageRating      = Math.Round(avgRating, 1),
                TotalReviews       = reviews,
                ApprovedMechanics  = approvedMechanics,
                AvailableMechanics = availableMechanics,
                IsShopAvailable    = availableMechanics > 0,
                IncomingRequests   = incoming.Take(5).ToList(),
                CurrentActiveJobs  = active
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ManageShop()
        {
            var shop = await GetCurrentShopAsync();
            return View(shop);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageShop(RepairShop model)
        {
            var userId = _userManager.GetUserId(User)!;
            var shop   = await _db.RepairShops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null) return NotFound();

            shop.ShopName    = model.ShopName;
            shop.Description = model.Description;
            shop.Address     = model.Address;
            shop.City        = model.City;
            shop.PhoneNumber = model.PhoneNumber;
            shop.Latitude    = model.Latitude;
            shop.Longitude   = model.Longitude;
            shop.LogoUrl     = model.LogoUrl;

            await _db.SaveChangesAsync();
            TempData["Success"] = _l["Msg.ShopUpdated"].Value;
            return RedirectToAction("ManageShop");
        }

        public async Task<IActionResult> IncomingRequests()
        {
            var shop = await GetCurrentShopAsync();
            if (shop == null) return RedirectToAction("ManageShop");
            if (shop.ShopStatus != ShopStatus.Approved) return View("PendingApproval", shop);

            var requests = await _shopService.GetIncomingRequestsAsync();
            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var shop = await GetCurrentShopAsync();
            if (shop == null || shop.ShopStatus != ShopStatus.Approved)
            {
                TempData["Error"] = _l["Msg.ShopMustBeApproved"].Value;
                return RedirectToAction("IncomingRequests");
            }

            var availableMechanics = await _mechanicService.GetApprovedAvailableMechanicsAsync(shop.Id);
            if (!availableMechanics.Any())
            {
                TempData["Error"] = _l["Msg.NoAvailableMechanics"].Value;
                return RedirectToAction("IncomingRequests");
            }

            var request = await _db.ServiceRequests
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null || request.Status != RequestStatus.InspectionPaid || request.ShopId != null)
            {
                TempData["Error"] = _l["Msg.RequestUnavailable"].Value;
                return RedirectToAction("IncomingRequests");
            }

            var vm = new AssignMechanicViewModel
            {
                ServiceRequestId     = id,
                CustomerName         = request.Customer.FullName,
                SituationDescription = request.SituationDescription,
                CustomerLatitude     = request.CustomerLatitude,
                CustomerLongitude    = request.CustomerLongitude,
                AvailableMechanics   = availableMechanics
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(AssignMechanicViewModel model)
        {
            var shop = await GetCurrentShopAsync();
            if (shop == null || shop.ShopStatus != ShopStatus.Approved) return NotFound();

            var mechanic = await _db.Mechanics.FindAsync(model.SelectedMechanicId);
            if (mechanic == null || mechanic.ShopId != shop.Id ||
                mechanic.Status != MechanicStatus.Approved || !mechanic.IsAvailable)
            {
                TempData["Error"] = _l["Msg.MechanicUnavailable"].Value;
                return RedirectToAction("IncomingRequests");
            }

            var request = await _db.ServiceRequests.FindAsync(model.ServiceRequestId);
            if (request == null || request.Status != RequestStatus.InspectionPaid || request.ShopId != null)
            {
                TempData["Error"] = _l["Msg.RequestUnavailable"].Value;
                return RedirectToAction("IncomingRequests");
            }

            request.ShopId     = shop.Id;
            request.MechanicId = mechanic.Id;
            request.Status     = RequestStatus.Accepted;
            request.UpdatedAt  = DateTime.UtcNow;

            mechanic.IsAvailable = false;

            await _db.SaveChangesAsync();

            TempData["Success"] = string.Format(_l["Msg.RequestAccepted"].Value, mechanic.FullName);
            return RedirectToAction("ActiveJobs");
        }

        public async Task<IActionResult> ActiveJobs()
        {
            var shop = await GetCurrentShopAsync();
            if (shop == null) return RedirectToAction("ManageShop");
            if (shop.ShopStatus != ShopStatus.Approved) return View("PendingApproval", shop);

            var jobs = await _shopService.GetActiveJobsAsync(shop.Id);
            return View(jobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int requestId, RequestStatus newStatus)
        {
            var shop    = await GetCurrentShopAsync();
            var request = await _db.ServiceRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ShopId == shop!.Id);

            if (request == null) return NotFound();

            var validTransitions = new Dictionary<RequestStatus, RequestStatus>
            {
                [RequestStatus.Accepted]          = RequestStatus.MechanicArrived,
                [RequestStatus.QuotationApproved] = RequestStatus.Completed
            };

            if (!validTransitions.TryGetValue(request.Status, out var expected) || expected != newStatus)
            {
                TempData["Error"] = _l["Msg.InvalidTransition"].Value;
                return RedirectToAction("ActiveJobs");
            }

            request.Status    = newStatus;
            request.UpdatedAt = DateTime.UtcNow;

            // When completing, free up the mechanic
            if (newStatus == RequestStatus.Completed && request.MechanicId.HasValue)
            {
                var mechanic = await _db.Mechanics.FindAsync(request.MechanicId);
                if (mechanic != null) mechanic.IsAvailable = true;

                await _invoiceService.GetOrCreateInvoiceAsync(requestId);
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = newStatus == RequestStatus.Completed
                ? _l["Msg.JobComplete"].Value
                : _l["Msg.StatusUpdated"].Value;

            return RedirectToAction(newStatus == RequestStatus.Completed ? "CompletedJobs" : "ActiveJobs");
        }

        [HttpGet]
        public async Task<IActionResult> SubmitQuotation(int requestId)
        {
            var shop    = await GetCurrentShopAsync();
            var request = await _db.ServiceRequests
                .Include(r => r.Customer)
                .Include(r => r.Mechanic)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ShopId == shop!.Id);

            if (request == null || request.Status != RequestStatus.MechanicArrived)
            {
                TempData["Error"] = _l["Msg.QuotationTiming"].Value;
                return RedirectToAction("ActiveJobs");
            }

            var vm = new SubmitQuotationViewModel
            {
                ServiceRequestId     = requestId,
                CustomerName         = request.Customer.FullName,
                SituationDescription = request.SituationDescription
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuotation(SubmitQuotationViewModel model)
        {
            var shop    = await GetCurrentShopAsync();
            var request = await _db.ServiceRequests
                .Include(r => r.Mechanic)
                .FirstOrDefaultAsync(r => r.Id == model.ServiceRequestId && r.ShopId == shop!.Id);

            if (request == null || request.Status != RequestStatus.MechanicArrived)
            {
                TempData["Error"] = _l["Msg.InvalidRequest"].Value;
                return RedirectToAction("ActiveJobs");
            }

            if (!ModelState.IsValid)
            {
                model.CustomerName         = "Customer";
                model.SituationDescription = request.SituationDescription;
                return View(model);
            }

            _db.RepairQuotations.Add(new RepairQuotation
            {
                ServiceRequestId = model.ServiceRequestId,
                MechanicId       = request.MechanicId ?? 0,
                DiagnosisNotes   = model.DiagnosisNotes,
                QuotedAmount     = model.QuotedAmount,
                Status           = QuotationStatus.AwaitingApproval,
                CreatedAt        = DateTime.UtcNow
            });

            request.DiagnosisNotes = model.DiagnosisNotes;
            request.Status         = RequestStatus.Diagnosed;
            request.UpdatedAt      = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = _l["Msg.QuotationSubmitted"].Value;
            return RedirectToAction("ActiveJobs");
        }

        public async Task<IActionResult> CompletedJobs()
        {
            var shop = await GetCurrentShopAsync();
            if (shop == null) return RedirectToAction("ManageShop");

            var jobs = await _shopService.GetCompletedJobsAsync(shop.Id);
            return View(jobs);
        }

        public async Task<IActionResult> Earnings()
        {
            var shop = await GetCurrentShopAsync();
            if (shop == null) return RedirectToAction("ManageShop");

            var inspectionEarnings = await _paymentService.GetShopTotalInspectionEarningsAsync(shop.Id);
            var repairEarnings     = await _paymentService.GetShopTotalRepairEarningsAsync(shop.Id);

            var repairPayments = await _db.RepairPayments
                .Include(p => p.ServiceRequest)
                    .ThenInclude(r => r.Customer)
                .Include(p => p.ServiceRequest)
                    .ThenInclude(r => r.Mechanic)
                .Where(p => p.ServiceRequest.ShopId == shop.Id && p.Status == PaymentStatus.Paid)
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();

            var inspectionList = await _db.InspectionPayments
                .Include(p => p.ServiceRequest)
                    .ThenInclude(r => r.Customer)
                .Where(p => p.ServiceRequest.ShopId == shop.Id && p.Status == PaymentStatus.Paid)
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();

            ViewBag.TotalInspectionEarnings = inspectionEarnings;
            ViewBag.TotalRepairEarnings     = repairEarnings;
            ViewBag.TotalEarnings           = inspectionEarnings + repairEarnings;
            ViewBag.RepairPayments          = repairPayments;
            ViewBag.InspectionPayments      = inspectionList;

            return View();
        }

        public async Task<IActionResult> Reviews()
        {
            var shop = await GetCurrentShopAsync();
            if (shop == null) return RedirectToAction("ManageShop");

            var feedbacks = await _db.Feedbacks
                .Include(f => f.Customer)
                .Include(f => f.Mechanic)
                .Where(f => f.ShopId == shop.Id)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            ViewBag.AverageRating = feedbacks.Count > 0
                ? Math.Round(feedbacks.Average(f => (double)f.Rating), 1) : 0;
            ViewBag.TotalReviews  = feedbacks.Count;
            return View(feedbacks);
        }

        public async Task<IActionResult> Mechanics()
        {
            var shop = await GetCurrentShopAsync();
            if (shop == null) return RedirectToAction("ManageShop");

            var mechanics = await _mechanicService.GetShopMechanicsAsync(shop.Id);

            var vm = new MechanicListViewModel
            {
                Mechanics     = mechanics,
                ShopId        = shop.Id,
                ShopName      = shop.ShopName,
                PendingCount  = mechanics.Count(m => m.Status == MechanicStatus.Pending),
                ApprovedCount = mechanics.Count(m => m.Status == MechanicStatus.Approved),
                RejectedCount = mechanics.Count(m => m.Status == MechanicStatus.Rejected)
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> AddMechanic()
        {
            var shop = await GetCurrentShopAsync();
            if (shop == null) return RedirectToAction("ManageShop");
            return View(new AddMechanicViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMechanic(AddMechanicViewModel model)
        {
            var shop = await GetCurrentShopAsync();
            if (shop == null) return RedirectToAction("ManageShop");

            if (await _mechanicService.NationalIdExistsAsync(model.NationalId))
            {
                ModelState.AddModelError("NationalId", _l["Msg.NationalIdExists"].Value);
            }

            if (!ModelState.IsValid) return View(model);

            string? profileUrl = null;
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploads = Path.Combine(
                    _env.WebRootPath,
                    "uploads", "mechanic-profiles");
                Directory.CreateDirectory(uploads);
                var ext      = Path.GetExtension(model.ProfileImage.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                using var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
                await model.ProfileImage.CopyToAsync(stream);
                profileUrl = $"/uploads/mechanic-profiles/{fileName}";
            }

            await _mechanicService.AddMechanicAsync(new Mechanic
            {
                ShopId            = shop.Id,
                FullName          = model.FullName,
                NationalId        = model.NationalId,
                PhoneNumber       = model.PhoneNumber,
                YearsOfExperience = model.YearsOfExperience,
                ProfileImageUrl   = profileUrl,
                Status            = MechanicStatus.Pending,
                IsAvailable       = true
            });

            TempData["Success"] = _l["Msg.MechanicAdded"].Value;
            return RedirectToAction("Mechanics");
        }

        [HttpGet]
        public async Task<IActionResult> EditMechanic(int id)
        {
            var shop     = await GetCurrentShopAsync();
            var mechanic = await _mechanicService.GetMechanicByIdAsync(id);

            if (mechanic == null || mechanic.ShopId != shop?.Id) return NotFound();

            var vm = new EditMechanicViewModel
            {
                Id                    = mechanic.Id,
                FullName              = mechanic.FullName,
                PhoneNumber           = mechanic.PhoneNumber,
                YearsOfExperience     = mechanic.YearsOfExperience,
                ExistingProfileImageUrl = mechanic.ProfileImageUrl,
                NationalId            = mechanic.NationalId,
                Status                = mechanic.Status,
                IsAvailable           = mechanic.IsAvailable
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMechanic(EditMechanicViewModel model)
        {
            var shop     = await GetCurrentShopAsync();
            var mechanic = await _mechanicService.GetMechanicByIdAsync(model.Id);

            if (mechanic == null || mechanic.ShopId != shop?.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                model.ExistingProfileImageUrl = mechanic.ProfileImageUrl;
                model.NationalId              = mechanic.NationalId;
                model.Status                  = mechanic.Status;
                return View(model);
            }

            mechanic.FullName          = model.FullName;
            mechanic.PhoneNumber       = model.PhoneNumber;
            mechanic.YearsOfExperience = model.YearsOfExperience;
            mechanic.IsAvailable       = model.IsAvailable;

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploads = Path.Combine(
                    _env.WebRootPath,
                    "uploads", "mechanic-profiles");
                Directory.CreateDirectory(uploads);
                var ext      = Path.GetExtension(model.ProfileImage.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                using var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
                await model.ProfileImage.CopyToAsync(stream);
                mechanic.ProfileImageUrl = $"/uploads/mechanic-profiles/{fileName}";
            }

            await _mechanicService.UpdateMechanicAsync(mechanic);
            TempData["Success"] = _l["Msg.MechanicUpdated"].Value;
            return RedirectToAction("Mechanics");
        }
    }
}
