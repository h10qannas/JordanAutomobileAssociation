using System.Text;
using JAA.Data;
using JAA.Models;
using JAA.Resources;
using JAA.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace JAA.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IStringLocalizer<SharedResources> _l;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext db,
            IWebHostEnvironment env,
            IStringLocalizer<SharedResources> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _env = env;
            _l = localizer;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User)) return RedirectToUserDashboard();
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var resolvedUser = await _userManager.FindByNameAsync(model.Identifier)
                ?? await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.Identifier);

            if (resolvedUser != null && !resolvedUser.IsActive)
            {
                ModelState.AddModelError(string.Empty, _l["Msg.AccountDeactivated"]);
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Identifier, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                var byPhone = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == model.Identifier);
                if (byPhone != null)
                    result = await _signInManager.PasswordSignInAsync(
                        byPhone.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
            }

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                var user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.UserName == model.Identifier || u.PhoneNumber == model.Identifier);
                if (user?.Role == UserRole.Admin)
                    return RedirectToAction("Dashboard", "Admin", new { area = "Admin" });
                if (user?.Role == UserRole.ShopOwner)
                    return RedirectToAction("Dashboard", "Shop");
                return RedirectToAction("Dashboard", "Customer");
            }

            ModelState.AddModelError(string.Empty, _l["Msg.IncorrectCredentials"]);
            return View(model);
        }

        [HttpGet]
        public IActionResult ShopLogin(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User)) return RedirectToUserDashboard();
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet]
        public IActionResult ShopRegister()
        {
            if (_signInManager.IsSignedIn(User)) return RedirectToUserDashboard();
            return View(new ShopRegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShopRegister(ShopRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var normalizedPhone = model.PhoneNumber.Trim();
            var internalEmail   = normalizedPhone.Replace("+", "").Replace(" ", "") + "@jaa.jo";

            var user = new ApplicationUser
            {
                UserName       = normalizedPhone,
                Email          = internalEmail,
                FullName       = model.FullName,
                PhoneNumber    = normalizedPhone,
                Role           = UserRole.ShopOwner,
                EmailConfirmed = true,
                IsActive       = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "ShopOwner");

            // Handle certificate upload
            string? certUrl = null;
            if (model.BusinessCertificate != null && model.BusinessCertificate.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads", "certificates");
                Directory.CreateDirectory(uploads);
                var ext      = Path.GetExtension(model.BusinessCertificate.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploads, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await model.BusinessCertificate.CopyToAsync(stream);
                certUrl = $"/uploads/certificates/{fileName}";
            }

            _db.RepairShops.Add(new RepairShop
            {
                OwnerId                = user.Id,
                ShopName               = model.ShopName,
                OwnerName              = model.OwnerName,
                PhoneNumber            = model.ShopPhone,
                City                   = model.ShopCity,
                Address                = model.ShopAddress,
                BusinessCertificateUrl = certUrl,
                ShopStatus             = ShopStatus.Pending,
                IsVerified             = false
            });
            await _db.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["Info"] = _l["Msg.ShopPendingApproval"].Value;
            return RedirectToAction("Dashboard", "Shop");
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User)) return RedirectToUserDashboard();
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var normalizedPhone = model.PhoneNumber.Trim();
            var internalEmail   = normalizedPhone.Replace("+", "").Replace(" ", "") + "@jaa.jo";

            var user = new ApplicationUser
            {
                UserName       = normalizedPhone,
                Email          = internalEmail,
                FullName       = model.FullName,
                PhoneNumber    = normalizedPhone,
                Role           = UserRole.Customer,
                EmailConfirmed = true,
                IsActive       = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                await _signInManager.SignInAsync(user, isPersistent: false);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Dashboard", "Customer");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber.Trim());

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, _l["Msg.NoAccountFound"]);
                return View(model);
            }

            var rawToken    = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

            return RedirectToAction("ResetPassword", new { userId = user.Id, token = encodedToken });
        }

        [HttpGet]
        public IActionResult ResetPassword(string? userId, string? token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction("Login");
            return View(new ResetPasswordViewModel { UserId = userId, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["Error"] = _l["Msg.InvalidResetRequest"].Value;
                return RedirectToAction("Login");
            }

            var rawToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            var result   = await _userManager.ResetPasswordAsync(user, rawToken, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["ResetSuccess"] = _l["Msg.PasswordResetSuccess"].Value;
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult SetLanguage(string culture, string returnUrl = "/")
        {
            Response.Cookies.Append(
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(
                    new Microsoft.AspNetCore.Localization.RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToUserDashboard()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Dashboard", "Admin", new { area = "Admin" });
            if (User.IsInRole("ShopOwner"))
                return RedirectToAction("Dashboard", "Shop");
            return RedirectToAction("Dashboard", "Customer");
        }
    }
}
