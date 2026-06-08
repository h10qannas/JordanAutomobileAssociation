using JAA.Data;
using JAA.Filters;
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
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RequestService _requestService;
        private readonly PaymentService _paymentService;
        private readonly InvoiceService _invoiceService;
        private readonly ShopService _shopService;
        private readonly IStringLocalizer<SharedResources> _l;

        public CustomerController(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            RequestService requestService,
            PaymentService paymentService,
            InvoiceService invoiceService,
            ShopService shopService,
            IStringLocalizer<SharedResources> localizer)
        {
            _db             = db;
            _userManager    = userManager;
            _requestService = requestService;
            _paymentService = paymentService;
            _invoiceService = invoiceService;
            _shopService    = shopService;
            _l              = localizer;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user     = await _userManager.GetUserAsync(User);
            var userId   = _userManager.GetUserId(User)!;
            var requests = await _requestService.GetCustomerRequestsAsync(userId);
            var active   = await _requestService.GetActiveRequestAsync(userId);

            var vm = new CustomerDashboardViewModel
            {
                UserFullName      = user?.FullName ?? string.Empty,
                TotalRequests     = requests.Count,
                CompletedRequests = requests.Count(r => r.Status == RequestStatus.Completed),
                PendingRequests   = requests.Count(r =>
                    r.Status == RequestStatus.Pending ||
                    r.Status == RequestStatus.InspectionPaid),
                ActiveRequest  = active,
                RecentRequests = requests.Take(5).ToList()
            };
            return View(vm);
        }

        public async Task<IActionResult> Map()
        {
            var userId  = _userManager.GetUserId(User);
            // Try to get coordinates from the customer's most recent active request
            double? custLat = null, custLng = null;
            if (userId != null)
            {
                var recent = await _db.ServiceRequests
                    .Where(r => r.CustomerId == userId && r.CustomerLatitude != 0)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();
                if (recent != null)
                {
                    custLat = recent.CustomerLatitude;
                    custLng = recent.CustomerLongitude;
                }
            }

            var nearby  = await _shopService.GetNearbyShopsAsync(custLat, custLng);

            var markers = nearby.Select(ns => new ShopMapMarker
            {
                Id                  = ns.Shop.Id,
                Name                = ns.Shop.ShopName,
                Lat                 = ns.Shop.Latitude ?? 0,
                Lng                 = ns.Shop.Longitude ?? 0,
                AvgRating           = Math.Round(ns.AvgRating, 1),
                ReviewCount         = ns.ReviewCount,
                AvailableMechanics  = ns.AvailableMechanics,
                IsAvailable         = ns.IsAvailable,
                City                = ns.Shop.City,
                Phone               = ns.Shop.PhoneNumber ?? "",
                LogoUrl             = ns.Shop.LogoUrl,
                DistanceKm          = ns.DistanceKm.HasValue ? Math.Round(ns.DistanceKm.Value, 1) : null,
                EstimatedArrivalMin = ns.EstimatedArrivalMin
            }).ToList();

            return View(new CustomerMapViewModel
            {
                Shops       = markers,
                CustomerLat = custLat,
                CustomerLng = custLng
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> RequestHelp()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && !user.IsActive)
                {
                    TempData["Error"] = _l["Msg.AccountDeactivatedNoRequests"].Value;
                    return RedirectToAction("Dashboard");
                }
            }
            return View();
        }

        [RequireActiveAccount]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestHelp(RequestHelpViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsActive)
            {
                TempData["Error"] = _l["Msg.AccountDeactivated2"].Value;
                return RedirectToAction("Dashboard");
            }

            if (!ModelState.IsValid) return View(model);

            var userId  = _userManager.GetUserId(User)!;
            var request = new ServiceRequest
            {
                CustomerId           = userId,
                SituationDescription = model.SituationDescription,
                CustomerLatitude     = model.Latitude,
                CustomerLongitude    = model.Longitude,
                Status               = RequestStatus.Pending,
                CreatedAt            = DateTime.UtcNow,
                UpdatedAt            = DateTime.UtcNow
            };

            await _requestService.CreateRequestAsync(request);
            return RedirectToAction("InspectionPayment", new { requestId = request.Id });
        }

        [HttpGet]
        public async Task<IActionResult> InspectionPayment(int requestId)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _requestService.GetRequestByIdAsync(requestId);

            if (request == null || request.CustomerId != userId)
                return NotFound();

            if (request.Status != RequestStatus.Pending)
                return RedirectToAction("TrackRequest", new { id = requestId });

            var (fee, shopShare, platShare) = await _paymentService.GetInspectionFeeSettingsAsync();

            var vm = new InspectionPaymentViewModel
            {
                ServiceRequestId    = requestId,
                Fee                 = fee,
                ShopShare           = shopShare,
                PlatformShare       = platShare,
                SituationDescription = request.SituationDescription
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayInspectionCash(int requestId)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _db.ServiceRequests.FindAsync(requestId);

            if (request == null || request.CustomerId != userId || request.Status != RequestStatus.Pending)
            {
                TempData["Error"] = _l["Msg.InvalidRequest"].Value;
                return RedirectToAction("Dashboard");
            }

            await _paymentService.CreateInspectionPaymentAsync(requestId, PaymentMethod.Cash);
            request.Status    = RequestStatus.InspectionPaid;
            request.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = _l["Msg.InspectionFeePaid"].Value;
            return RedirectToAction("TrackRequest", new { id = requestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayInspectionOnline(int requestId)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _db.ServiceRequests.FindAsync(requestId);

            if (request == null || request.CustomerId != userId || request.Status != RequestStatus.Pending)
            {
                TempData["Error"] = _l["Msg.InvalidRequest"].Value;
                return RedirectToAction("Dashboard");
            }

            var payment = await _paymentService.CreateInspectionPaymentAsync(requestId, PaymentMethod.Online);

            return RedirectToAction("SimulatedPayment", "Payment", new
            {
                requestId           = requestId,
                inspectionPaymentId = payment.Id,
                amount              = payment.Amount,
                paymentType         = "Inspection"
            });
        }

        [HttpGet]
        public async Task<IActionResult> QuotationApproval(int requestId)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _requestService.GetRequestByIdAsync(requestId);

            if (request == null || request.CustomerId != userId)
                return NotFound();

            if (request.Status != RequestStatus.Diagnosed || request.RepairQuotation == null)
                return RedirectToAction("TrackRequest", new { id = requestId });

            var quoted = request.RepairQuotation.QuotedAmount;

            var vm = new QuotationApprovalViewModel
            {
                ServiceRequestId       = requestId,
                QuotationId            = request.RepairQuotation.Id,
                DiagnosisNotes         = request.RepairQuotation.DiagnosisNotes,
                QuotedAmount           = quoted,
                InspectionAlreadyPaid  = request.InspectionPayment?.Amount ?? 0,
                MechanicName           = request.Mechanic?.FullName ?? "",
                ShopName               = request.Shop?.ShopName ?? "",
                SituationDescription   = request.SituationDescription,
                VatRate                = 0m,
                VatAmount              = 0m,
                TotalWithVat           = quoted
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveQuotationCash(int requestId, int quotationId)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _db.ServiceRequests
                .Include(r => r.RepairQuotation)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.CustomerId != userId ||
                request.Status != RequestStatus.Diagnosed || request.RepairQuotation?.Id != quotationId)
            {
                TempData["Error"] = _l["Msg.InvalidOperation"].Value;
                return RedirectToAction("Dashboard");
            }

            request.RepairQuotation.Status             = QuotationStatus.Approved;
            request.RepairQuotation.CustomerResponseAt = DateTime.UtcNow;

            await _paymentService.CreateRepairPaymentAsync(
                requestId, quotationId, request.RepairQuotation.QuotedAmount, PaymentMethod.Cash);

            request.Status    = RequestStatus.QuotationApproved;
            request.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Auto-generate invoice
            await _invoiceService.GetOrCreateInvoiceAsync(requestId);

            TempData["Success"] = _l["Msg.RepairApproved"].Value;
            return RedirectToAction("TrackRequest", new { id = requestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveQuotationOnline(int requestId, int quotationId)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _db.ServiceRequests
                .Include(r => r.RepairQuotation)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.CustomerId != userId ||
                request.Status != RequestStatus.Diagnosed || request.RepairQuotation?.Id != quotationId)
            {
                TempData["Error"] = _l["Msg.InvalidOperation"].Value;
                return RedirectToAction("Dashboard");
            }

            request.RepairQuotation.Status             = QuotationStatus.Approved;
            request.RepairQuotation.CustomerResponseAt = DateTime.UtcNow;

            var payment = await _paymentService.CreateRepairPaymentAsync(
                requestId, quotationId, request.RepairQuotation.QuotedAmount, PaymentMethod.Online);

            request.Status    = RequestStatus.QuotationApproved;
            request.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return RedirectToAction("SimulatedPayment", "Payment", new
            {
                requestId      = requestId,
                repairPaymentId = payment.Id,
                amount         = payment.TotalAmount,
                paymentType    = "Repair"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectQuotation(int requestId, int quotationId, string? reason)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _db.ServiceRequests
                .Include(r => r.RepairQuotation)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null || request.CustomerId != userId ||
                request.Status != RequestStatus.Diagnosed || request.RepairQuotation?.Id != quotationId)
            {
                TempData["Error"] = _l["Msg.InvalidOperation"].Value;
                return RedirectToAction("Dashboard");
            }

            request.RepairQuotation.Status                  = QuotationStatus.Rejected;
            request.RepairQuotation.CustomerResponseAt      = DateTime.UtcNow;
            request.RepairQuotation.CustomerRejectionReason = reason;
            request.Status    = RequestStatus.QuotationRejected;
            request.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Info"] = _l["Msg.QuotationRejected"].Value;
            return RedirectToAction("TrackRequest", new { id = requestId });
        }

        // ── Payment Confirmation ─────────────────────────────────────────
        [RequireActiveAccount]
        [HttpGet]
        public async Task<IActionResult> ConfirmPayment(int requestId)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _db.ServiceRequests
                .Include(r => r.Shop)
                .Include(r => r.Mechanic)
                .Include(r => r.RepairQuotation)
                .Include(r => r.PaymentVerification)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.CustomerId == userId);

            if (request == null || request.Status != RequestStatus.AwaitingConfirmation
                || request.PaymentVerification == null)
                return RedirectToAction("TrackRequest", new { id = requestId });

            return View(request);
        }

        [RequireActiveAccount]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int requestId, decimal customerAmount)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _db.ServiceRequests
                .Include(r => r.Mechanic)
                .Include(r => r.RepairPayment)
                .Include(r => r.PaymentVerification)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.CustomerId == userId);

            if (request == null || request.Status != RequestStatus.AwaitingConfirmation
                || request.PaymentVerification == null)
            {
                TempData["Error"] = _l["Msg.InvalidOperation"].Value;
                return RedirectToAction("Dashboard");
            }

            var verification = request.PaymentVerification;
            verification.CustomerConfirmedAmount = customerAmount;
            verification.UpdatedAt = DateTime.UtcNow;

            var diff = Math.Abs(verification.MechanicReportedAmount - customerAmount);
            bool amountsMatch = diff <= 0.01m;

            if (amountsMatch)
            {
                // Verified — finalise the payment and complete the request
                verification.Status              = VerificationStatus.Verified;
                verification.FinalApprovedAmount = verification.MechanicReportedAmount;
                verification.VerificationDate    = DateTime.UtcNow;

                // Update the RepairPayment total to the verified amount
                if (request.RepairPayment != null)
                {
                    var commRate = request.RepairPayment.CommissionRate;
                    var finalAmt = verification.FinalApprovedAmount.Value;
                    request.RepairPayment.QuotedAmount      = finalAmt;
                    request.RepairPayment.TotalAmount       = finalAmt;
                    request.RepairPayment.CommissionAmount  = Math.Round(finalAmt * commRate, 2);
                    request.RepairPayment.ShopAmount        = Math.Round(finalAmt - request.RepairPayment.CommissionAmount, 2);
                    request.RepairPayment.Status            = PaymentStatus.Paid;
                    request.RepairPayment.PaidAt            = DateTime.UtcNow;
                }

                // Free mechanic and generate invoice
                if (request.MechanicId.HasValue)
                {
                    var mechanic = await _db.Mechanics.FindAsync(request.MechanicId);
                    if (mechanic != null) mechanic.IsAvailable = true;
                }

                request.Status    = RequestStatus.Completed;
                request.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                await _invoiceService.GetOrCreateInvoiceAsync(requestId);

                TempData["Success"] = _l["Msg.PaymentVerified"].Value;
                return RedirectToAction("TrackRequest", new { id = requestId });
            }
            else
            {
                // Mismatch — flag for admin review
                verification.Status    = VerificationStatus.Flagged;
                verification.UpdatedAt = DateTime.UtcNow;

                request.Status    = RequestStatus.UnderReview;
                request.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                TempData["Info"] = _l["Msg.AmountMismatchFlagged"].Value;
                return RedirectToAction("TrackRequest", new { id = requestId });
            }
        }

        public async Task<IActionResult> TrackRequest(int id)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _requestService.GetRequestByIdAsync(id);

            if (request == null || request.CustomerId != userId) return NotFound();

            var steps = new List<(string Label, string Icon, string Description)>
            {
                (_l["Status.Pending"],                   "bi-clock",          _l["StepDesc.Pending"].Value),
                (_l["Status.InspectionPaid"],            "bi-credit-card",    _l["StepDesc.InspectionPaid"].Value),
                (_l["Status.Accepted"],                  "bi-handshake",      _l["StepDesc.Accepted"].Value),
                (_l["Status.MechanicArrived"],           "bi-geo-alt-fill",   _l["StepDesc.MechanicArrived"].Value),
                (_l["StepLabel.QuotationSent"],          "bi-file-text",      _l["StepDesc.QuotationSent"].Value),
                (_l["StepLabel.RepairApproved"],         "bi-tools",          _l["StepDesc.RepairApproved"].Value),
                (_l["Status.AwaitingConfirmation"],      "bi-person-check",   _l["StepDesc.AwaitingConfirmation"].Value),
                (_l["Status.Completed"],                 "bi-check-circle",   _l["StepDesc.Completed"].Value)
            };

            var stepIndex = request.Status switch
            {
                RequestStatus.Pending                => 0,
                RequestStatus.InspectionPaid         => 1,
                RequestStatus.Accepted               => 2,
                RequestStatus.MechanicArrived        => 3,
                RequestStatus.Diagnosed              => 4,
                RequestStatus.QuotationApproved      => 5,
                RequestStatus.AwaitingConfirmation   => 6,
                RequestStatus.UnderReview            => 6,
                RequestStatus.Completed              => 7,
                _                                   => 0
            };

            var vm = new TrackRequestViewModel
            {
                Request          = request,
                CurrentStepIndex = stepIndex,
                Steps            = steps
            };
            return View(vm);
        }

        public async Task<IActionResult> History()
        {
            var userId   = _userManager.GetUserId(User)!;
            var requests = await _requestService.GetCustomerRequestsAsync(userId);
            return View(new CustomerHistoryViewModel { Requests = requests });
        }

        [RequireActiveAccount]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var userId  = _userManager.GetUserId(User)!;
            var success = await _requestService.CancelRequestAsync(id, userId);

            TempData[success ? "Success" : "Error"] = success
                ? _l["Msg.RequestCancelledSuccess"].Value
                : _l["Msg.CannotCancel"].Value;
            return RedirectToAction("Dashboard");
        }

        [RequireActiveAccount]
        [HttpGet]
        public async Task<IActionResult> Feedback(int requestId)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _requestService.GetRequestByIdAsync(requestId);

            if (request == null || request.CustomerId != userId)
                return RedirectToAction("History");

            if (request.Status != RequestStatus.Completed)
            {
                TempData["Error"] = _l["Msg.FeedbackOnlyAfterComplete"].Value;
                return RedirectToAction("History");
            }

            if (request.Feedback != null)
            {
                TempData["Info"] = _l["Msg.AlreadyRated"].Value;
                return RedirectToAction("History");
            }

            var vm = new FeedbackViewModel
            {
                ServiceRequestId     = requestId,
                ShopName             = request.Shop?.ShopName ?? "Shop",
                MechanicName         = request.Mechanic?.FullName,
                SituationDescription = request.SituationDescription
            };
            return View(vm);
        }

        [RequireActiveAccount]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Feedback(FeedbackViewModel model)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _requestService.GetRequestByIdAsync(model.ServiceRequestId);

            if (request == null || request.CustomerId != userId ||
                request.Status != RequestStatus.Completed || request.Feedback != null)
                return RedirectToAction("History");

            if (!ModelState.IsValid)
            {
                model.ShopName     = request.Shop?.ShopName ?? "Shop";
                model.MechanicName = request.Mechanic?.FullName;
                return View(model);
            }

            _db.Feedbacks.Add(new Feedback
            {
                ServiceRequestId = model.ServiceRequestId,
                CustomerId       = userId,
                ShopId           = request.ShopId!.Value,
                MechanicId       = request.MechanicId,
                Rating           = model.Rating,
                Comment          = model.Comment,
                CreatedAt        = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = _l["Msg.FeedbackThankYou"].Value;
            return RedirectToAction("History");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user     = await _userManager.GetUserAsync(User);
            var userId   = _userManager.GetUserId(User)!;
            var requests = await _requestService.GetCustomerRequestsAsync(userId);

            var vm = new ProfileViewModel
            {
                FullName          = user!.FullName,
                Email             = user.Email!,
                PhoneNumber       = user.PhoneNumber,
                City              = user.City,
                MemberSince       = user.CreatedAt,
                IsActive          = user.IsActive,
                TotalRequests     = requests.Count,
                CompletedRequests = requests.Count(r => r.Status == RequestStatus.Completed),
                RecentRequests    = requests.Take(5).ToList()
            };
            return View(vm);
        }

        [RequireActiveAccount]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var userId   = _userManager.GetUserId(User)!;
                var user     = await _userManager.GetUserAsync(User);
                var requests = await _requestService.GetCustomerRequestsAsync(userId);
                model.City              = user?.City ?? string.Empty;
                model.MemberSince       = user?.CreatedAt ?? DateTime.UtcNow;
                model.IsActive          = user?.IsActive ?? true;
                model.TotalRequests     = requests.Count;
                model.CompletedRequests = requests.Count(r => r.Status == RequestStatus.Completed);
                model.RecentRequests    = requests.Take(5).ToList();
                return View(model);
            }

            var appUser         = await _userManager.GetUserAsync(User);
            appUser!.FullName   = model.FullName;
            appUser.PhoneNumber = model.PhoneNumber;
            await _userManager.UpdateAsync(appUser);
            TempData["Success"] = _l["Msg.ProfileUpdated"].Value;
            return RedirectToAction("Profile");
        }
    }
}
