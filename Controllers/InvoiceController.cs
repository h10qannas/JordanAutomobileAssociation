using JAA.Models;
using JAA.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JAA.Controllers
{
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly InvoiceService _invoiceService;

        public InvoiceController(
            UserManager<ApplicationUser> userManager,
            InvoiceService invoiceService)
        {
            _userManager    = userManager;
            _invoiceService = invoiceService;
        }

        public async Task<IActionResult> View(int requestId)
        {
            var userId = _userManager.GetUserId(User)!;
            var data   = await _invoiceService.GetInvoiceDataAsync(requestId);

            if (data == null) return NotFound();

            // Customers see only their own invoices; shops/admins see their related ones
            if (User.IsInRole("Customer") && data.Request.CustomerId != userId)
                return Forbid();

            if (User.IsInRole("ShopOwner") && data.Request.Shop?.OwnerId != userId)
                return Forbid();

            return View(data);
        }

        public async Task<IActionResult> Print(int requestId)
        {
            var userId = _userManager.GetUserId(User)!;
            var data   = await _invoiceService.GetInvoiceDataAsync(requestId);

            if (data == null) return NotFound();

            if (User.IsInRole("Customer") && data.Request.CustomerId != userId)
                return Forbid();

            if (User.IsInRole("ShopOwner") && data.Request.Shop?.OwnerId != userId)
                return Forbid();

            return View(data);
        }
    }
}
