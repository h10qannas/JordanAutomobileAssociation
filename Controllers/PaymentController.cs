using JAA.Data;
using JAA.Models;
using JAA.Services;
using JAA.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JAA.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PaymentService _paymentService;
        private readonly InvoiceService _invoiceService;

        public PaymentController(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            PaymentService paymentService,
            InvoiceService invoiceService)
        {
            _db             = db;
            _userManager    = userManager;
            _paymentService = paymentService;
            _invoiceService = invoiceService;
        }

        [HttpGet]
        public async Task<IActionResult> SimulatedPayment(
            int requestId,
            int? inspectionPaymentId,
            int? repairPaymentId,
            decimal amount,
            string paymentType)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _db.ServiceRequests.FindAsync(requestId);

            if (request == null || request.CustomerId != userId)
                return NotFound();

            var vm = new SimulatedPaymentViewModel
            {
                ServiceRequestId    = requestId,
                InspectionPaymentId = inspectionPaymentId,
                RepairPaymentId     = repairPaymentId,
                Amount              = amount,
                PaymentType         = paymentType,
                Description         = paymentType == "Inspection"
                    ? $"Inspection / Dispatch Fee — Request #{requestId}"
                    : $"Repair Payment — Request #{requestId}"
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessSimulatedPayment(
            int requestId,
            int? inspectionPaymentId,
            int? repairPaymentId,
            string paymentType,
            string cardNumber,
            string cardHolder,
            string expiry,
            string cvv)
        {
            var userId  = _userManager.GetUserId(User)!;
            var request = await _db.ServiceRequests.FindAsync(requestId);

            if (request == null || request.CustomerId != userId)
                return NotFound();

            var transactionRef = $"SIM-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            if (paymentType == "Inspection" && inspectionPaymentId.HasValue)
            {
                await _paymentService.ConfirmOnlineInspectionPaymentAsync(inspectionPaymentId.Value, transactionRef);
                request.Status    = RequestStatus.InspectionPaid;
                request.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                TempData["Success"] = "Inspection fee paid successfully. Your request is now live.";
            }
            else if (paymentType == "Repair" && repairPaymentId.HasValue)
            {
                await _paymentService.ConfirmOnlineRepairPaymentAsync(repairPaymentId.Value, transactionRef);
                await _invoiceService.GetOrCreateInvoiceAsync(requestId);

                TempData["Success"] = "Repair payment confirmed. The mechanic will proceed.";
            }

            return RedirectToAction("TrackRequest", "Customer", new { id = requestId });
        }
    }
}
