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

namespace JAA.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ShopService _shopService;
        private readonly MechanicService _mechanicService;
        private readonly PaymentService _paymentService;
        private readonly TestimonialService _testimonialService;
        private readonly IStringLocalizer<SharedResources> _l;

        public AdminController(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            ShopService shopService,
            MechanicService mechanicService,
            PaymentService paymentService,
            TestimonialService testimonialService,
            IStringLocalizer<SharedResources> localizer)
        {
            _db                 = db;
            _userManager        = userManager;
            _shopService        = shopService;
            _mechanicService    = mechanicService;
            _paymentService     = paymentService;
            _testimonialService = testimonialService;
            _l                  = localizer;
        }

        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.UtcNow.Date;

            var liveRequests = await _db.ServiceRequests
                .Where(r => r.Status != RequestStatus.Completed &&
                            r.Status != RequestStatus.Cancelled &&
                            r.Status != RequestStatus.Rejected &&
                            r.Status != RequestStatus.QuotationRejected)
                .Include(r => r.Customer)
                .Include(r => r.Shop)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            var pendingShops = await _db.RepairShops
                .Where(s => s.ShopStatus == ShopStatus.Pending)
                .Include(s => s.Owner)
                .Take(5)
                .ToListAsync();

            var pendingMechanicsCount = await _db.Mechanics
                .CountAsync(m => m.Status == MechanicStatus.Pending);

            var pendingRefundsCount = await _db.Refunds
                .CountAsync(r => r.Status == RefundStatus.Pending);

            var avgRating = await _db.Feedbacks.AnyAsync()
                ? await _db.Feedbacks.AverageAsync(f => (double)f.Rating) : 0;

            var vm = new AdminDashboardViewModel
            {
                TotalUsers             = await _db.Users.CountAsync(),
                TotalShops             = await _db.RepairShops.CountAsync(),
                VerifiedShops          = await _db.RepairShops.CountAsync(s => s.ShopStatus == ShopStatus.Approved),
                TotalCustomers         = await _db.Users.CountAsync(u => u.Role == UserRole.Customer),
                TotalRequests          = await _db.ServiceRequests.CountAsync(),
                ActiveRequests         = liveRequests.Count,
                CompletedRequests      = await _db.ServiceRequests.CountAsync(r => r.Status == RequestStatus.Completed),
                AverageRating          = Math.Round(avgRating, 1),
                LiveRequests           = liveRequests,
                UnverifiedShops        = pendingShops,
                PendingMechanicsCount  = pendingMechanicsCount,
                PendingRefundsCount    = pendingRefundsCount,
                ActiveRequestsToday    = await _db.ServiceRequests
                    .CountAsync(r => r.CreatedAt >= today && r.Status != RequestStatus.Cancelled),
                CompletedToday         = await _db.ServiceRequests
                    .CountAsync(r => r.UpdatedAt.Date == today && r.Status == RequestStatus.Completed)
            };
            return View(vm);
        }

        public Task<IActionResult> Index() => Dashboard();

        public async Task<IActionResult> Users(string? search)
        {
            var query = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            ViewBag.Search = search;
            return View(users);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            TempData["Success"] = string.Format(
                user.IsActive ? _l["Msg.UserActivated"].Value : _l["Msg.UserDeactivated"].Value,
                user.FullName);
            return RedirectToAction("Users");
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            await _userManager.DeleteAsync(user);
            TempData["Success"] = _l["Msg.UserDeleted"].Value;
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Shops(string? search)
        {
            var query = _db.RepairShops
                .Include(s => s.Owner)
                .Include(s => s.Feedbacks)
                .Include(s => s.Mechanics)
                .Include(s => s.ServiceRequests)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.ShopName.Contains(search) || s.City.Contains(search));

            var shops = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
            ViewBag.Search = search;
            return View(shops);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyShop(int id)
        {
            var shop = await _db.RepairShops.FindAsync(id);
            if (shop == null) return NotFound();
            shop.IsVerified = !shop.IsVerified;
            shop.ShopStatus = shop.IsVerified ? ShopStatus.Approved : ShopStatus.Pending;
            await _db.SaveChangesAsync();
            TempData["Success"] = shop.IsVerified ? _l["Msg.ShopVerified"].Value : _l["Msg.ShopUnverified"].Value;
            return RedirectToAction("Shops");
        }

        // ── Shop Approval Workflow ─────────────────────────────────────────

        public async Task<IActionResult> PendingShops()
        {
            var shops = await _shopService.GetPendingShopsAsync();
            return View(shops);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveShop(int id)
        {
            await _shopService.ApproveShopAsync(id);
            TempData["Success"] = _l["Msg.ShopApproved"].Value;
            return RedirectToAction("PendingShops");
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectShop(int id, string? reason)
        {
            await _shopService.RejectShopAsync(id, reason);
            TempData["Success"] = _l["Msg.ShopRejected"].Value;
            return RedirectToAction("PendingShops");
        }

        public async Task<IActionResult> ViewCertificate(int shopId)
        {
            var shop = await _db.RepairShops
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.Id == shopId);

            if (shop == null || string.IsNullOrEmpty(shop.BusinessCertificateUrl))
                return NotFound();

            return View(shop);
        }

        // ── Mechanic Approval Workflow ─────────────────────────────────────

        public async Task<IActionResult> PendingMechanics()
        {
            var mechanics = await _mechanicService.GetPendingMechanicsAsync();
            return View(mechanics);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMechanic(int id)
        {
            await _mechanicService.ApproveMechanicAsync(id);
            TempData["Success"] = _l["Msg.MechanicApproved"].Value;
            return RedirectToAction("PendingMechanics");
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectMechanic(int id)
        {
            await _mechanicService.RejectMechanicAsync(id);
            TempData["Success"] = _l["Msg.MechanicRejected"].Value;
            return RedirectToAction("PendingMechanics");
        }

        // ── Requests ───────────────────────────────────────────────────────

        public async Task<IActionResult> Requests(string? statusFilter, string? search, string? sort)
        {
            var query = _db.ServiceRequests
                .Include(r => r.Customer)
                .Include(r => r.Shop)
                .Include(r => r.Mechanic)
                .Include(r => r.Payment)
                .Include(r => r.InspectionPayment)
                .Include(r => r.RepairPayment)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter) &&
                Enum.TryParse<RequestStatus>(statusFilter, out var parsed))
                query = query.Where(r => r.Status == parsed);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(r => r.Customer!.FullName.Contains(search) ||
                                         r.SituationDescription.Contains(search));

            query = sort switch
            {
                "oldest" => query.OrderBy(r => r.CreatedAt),
                _        => query.OrderByDescending(r => r.CreatedAt)
            };

            var vm = new AdminRequestsViewModel
            {
                Requests     = await query.ToListAsync(),
                StatusFilter = statusFilter,
                Search       = search,
                Sort         = sort
            };
            return View(vm);
        }

        // ── Refunds ────────────────────────────────────────────────────────

        public async Task<IActionResult> Refunds()
        {
            var pending   = await _paymentService.GetPendingRefundsAsync();
            var processed = await _db.Refunds
                .Where(r => r.Status != RefundStatus.Pending)
                .Include(r => r.ServiceRequest).ThenInclude(sr => sr.Customer)
                .Include(r => r.ProcessedByAdmin)
                .OrderByDescending(r => r.ProcessedAt)
                .Take(50)
                .ToListAsync();

            return View(new RefundListViewModel
            {
                PendingRefunds   = pending,
                ProcessedRefunds = processed
            });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefund(int id, bool approve, string? notes)
        {
            var adminId = _userManager.GetUserId(User)!;
            await _paymentService.ProcessRefundAsync(id, adminId, approve, notes);
            TempData["Success"] = approve ? _l["Msg.RefundApproved"].Value : _l["Msg.RefundRejected"].Value;
            return RedirectToAction("Refunds");
        }

        // ── Settings ───────────────────────────────────────────────────────

        public async Task<IActionResult> Settings()
        {
            var settings = await _db.SystemSettings.OrderBy(s => s.Id).ToListAsync();
            return View(settings);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSetting(int id, string value)
        {
            var setting = await _db.SystemSettings.FindAsync(id);
            if (setting == null) return NotFound();
            setting.Value = value;
            await _db.SaveChangesAsync();
            TempData["Success"] = string.Format(_l["Msg.SettingUpdated"].Value, setting.Key);
            return RedirectToAction("Settings");
        }

        // ── Reports ────────────────────────────────────────────────────────

        public async Task<IActionResult> Reports()
        {
            var completedRequests = await _db.ServiceRequests
                .Where(r => r.Status == RequestStatus.Completed)
                .Include(r => r.Shop)
                .Include(r => r.Customer)
                .Include(r => r.Mechanic)
                .Include(r => r.Payment)
                .Include(r => r.InspectionPayment)
                .Include(r => r.RepairPayment)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();

            var feedbacks = await _db.Feedbacks
                .Include(f => f.Shop)
                .Include(f => f.Customer)
                .Include(f => f.Mechanic)
                .OrderByDescending(f => f.CreatedAt)
                .Take(20)
                .ToListAsync();

            var legacyRevenue          = completedRequests.Where(r => r.Payment != null).Sum(r => r.Payment!.Amount);
            var inspectionPlatformShare = await _db.InspectionPayments
                .Where(p => p.Status == PaymentStatus.Paid)
                .SumAsync(p => (decimal?)p.PlatformShare) ?? 0;
            var totalRepairRevenue = await _db.RepairPayments
                .Where(p => p.Status == PaymentStatus.Paid)
                .SumAsync(p => (decimal?)p.QuotedAmount) ?? 0;
            var totalJAACommission = await _db.RepairPayments
                .Where(p => p.Status == PaymentStatus.Paid)
                .SumAsync(p => (decimal?)p.CommissionAmount) ?? 0;
            var totalShopRevenue = await _db.RepairPayments
                .Where(p => p.Status == PaymentStatus.Paid)
                .SumAsync(p => (decimal?)p.ShopAmount) ?? 0;

            var vm = new AdminReportsViewModel
            {
                CompletedRequests             = completedRequests,
                RecentFeedbacks               = feedbacks,
                TotalRevenue                  = legacyRevenue + inspectionPlatformShare + totalJAACommission,
                TotalRepairRevenue            = totalRepairRevenue,
                TotalJAACommission            = totalJAACommission,
                TotalShopRevenue              = totalShopRevenue,
                TotalInspectionPlatformRevenue = inspectionPlatformShare,
                TotalCompletedRequests        = completedRequests.Count,
                AverageRating                 = feedbacks.Count > 0
                    ? Math.Round(feedbacks.Average(f => (double)f.Rating), 1) : 0
            };
            return View(vm);
        }

        // ── Payment Verifications ─────────────────────────────────────────

        public async Task<IActionResult> PaymentVerifications()
        {
            var verifications = await _db.PaymentVerifications
                .Include(v => v.ServiceRequest)
                    .ThenInclude(r => r.Customer)
                .Include(v => v.ServiceRequest)
                    .ThenInclude(r => r.Shop)
                .Include(v => v.ServiceRequest)
                    .ThenInclude(r => r.Mechanic)
                .Include(v => v.VerifiedBy)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return View(verifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveVerification(
            int verificationId, string resolution, decimal? finalAmount, string? adminNotes)
        {
            var adminId      = _userManager.GetUserId(User)!;
            var verification = await _db.PaymentVerifications
                .Include(v => v.ServiceRequest)
                    .ThenInclude(r => r.RepairPayment)
                .Include(v => v.ServiceRequest)
                    .ThenInclude(r => r.Mechanic)
                .FirstOrDefaultAsync(v => v.Id == verificationId);

            if (verification == null)
            {
                TempData["Error"] = "Verification record not found.";
                return RedirectToAction("PaymentVerifications");
            }

            decimal approvedAmount = resolution switch
            {
                "mechanic" => verification.MechanicReportedAmount,
                "customer" => verification.CustomerConfirmedAmount ?? verification.MechanicReportedAmount,
                "manual"   => finalAmount ?? verification.MechanicReportedAmount,
                _          => verification.MechanicReportedAmount
            };

            verification.FinalApprovedAmount = approvedAmount;
            verification.Status              = VerificationStatus.Resolved;
            verification.VerifiedById        = adminId;
            verification.AdminNotes          = adminNotes;
            verification.VerificationDate    = DateTime.UtcNow;
            verification.UpdatedAt           = DateTime.UtcNow;

            var request = verification.ServiceRequest;
            if (request.RepairPayment != null)
            {
                var commRate = request.RepairPayment.CommissionRate;
                request.RepairPayment.QuotedAmount     = approvedAmount;
                request.RepairPayment.TotalAmount      = approvedAmount;
                request.RepairPayment.CommissionAmount = Math.Round(approvedAmount * commRate, 2);
                request.RepairPayment.ShopAmount       = Math.Round(approvedAmount - request.RepairPayment.CommissionAmount, 2);
                request.RepairPayment.Status           = PaymentStatus.Paid;
                request.RepairPayment.PaidAt           = DateTime.UtcNow;
            }

            if (request.MechanicId.HasValue)
            {
                var mechanic = await _db.Mechanics.FindAsync(request.MechanicId);
                if (mechanic != null) mechanic.IsAvailable = true;
            }

            request.Status    = RequestStatus.Completed;
            request.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var invoiceService = HttpContext.RequestServices.GetRequiredService<InvoiceService>();
            await invoiceService.GetOrCreateInvoiceAsync(request.Id);

            TempData["Success"] = $"Request #{request.Id} resolved — {approvedAmount:F2} JOD approved.";
            return RedirectToAction("PaymentVerifications");
        }

        // ── Mechanic Details (admin view) ──────────────────────────────────

        public async Task<IActionResult> MechanicDetails(int id)
        {
            var mechanic = await _db.Mechanics
                .Include(m => m.Shop)
                .Include(m => m.ServiceRequests)
                .Include(m => m.Feedbacks)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mechanic == null) return NotFound();

            var avgRating = mechanic.Feedbacks.Any()
                ? Math.Round(mechanic.Feedbacks.Average(f => (double)f.Rating), 1) : 0.0;

            var vm = new MechanicDetailViewModel
            {
                Mechanic      = mechanic,
                AverageRating = avgRating,
                TotalJobs     = mechanic.ServiceRequests.Count,
                CompletedJobs = mechanic.ServiceRequests.Count(r => r.Status == RequestStatus.Completed)
            };
            return View(vm);
        }

        // ── Testimonials ───────────────────────────────────────────────────

        public async Task<IActionResult> Testimonials(TestimonialStatus? status)
        {
            var vm = await _testimonialService.GetAdminViewAsync(status);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveTestimonial(int id)
        {
            var adminId = _userManager.GetUserId(User)!;
            await _testimonialService.ApproveAsync(id, adminId);
            TempData["Success"] = "Testimonial approved and is now publicly visible.";
            return RedirectToAction(nameof(Testimonials));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectTestimonial(int id, string? reason)
        {
            var adminId = _userManager.GetUserId(User)!;
            await _testimonialService.RejectAsync(id, adminId, reason);
            TempData["Success"] = "Testimonial rejected.";
            return RedirectToAction(nameof(Testimonials));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideTestimonial(int id)
        {
            await _testimonialService.HideAsync(id);
            TempData["Success"] = "Testimonial hidden from public view.";
            return RedirectToAction(nameof(Testimonials));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeaturedTestimonial(int id)
        {
            await _testimonialService.ToggleFeaturedAsync(id);
            return RedirectToAction(nameof(Testimonials));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTestimonial(int id)
        {
            await _testimonialService.DeleteAsync(id);
            TempData["Success"] = "Testimonial deleted.";
            return RedirectToAction(nameof(Testimonials));
        }
    }
}
