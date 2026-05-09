using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Models.ViewModels;
using SmartShop.Services;
using SmartShop.Utility;
using System.Security.Cryptography;

namespace SmartShop.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly EmailService _emailService;
        private readonly IRazorViewRenderer _viewRenderer;

        public AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
            ILogger<AuthController> logger, IUnitOfWork unitOfWork,
            EmailService emailService, IRazorViewRenderer viewRenderer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _viewRenderer = viewRenderer;
        }

        [HttpGet]
        public IActionResult Register() => User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Index", "Home", new { area = "Customers" })
            : View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("New customer registered: {Email}", model.Email);
                await _userManager.AddToRoleAsync(user, SD.Role_Customer);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home", new { area = "Customers" });
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return User.Identity?.IsAuthenticated == true
                ? RedirectToAction("Index", "Home", new { area = "Customers" })
                : View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in: {Email}", model.Email);
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && await _userManager.IsInRoleAsync(user, SD.Role_Admin))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home", new { area = "Customers" });
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home", new { area = "Customers" });
        }

        public IActionResult AccessDenied() => View();

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Email"] = model.Email;
                return RedirectToAction(nameof(VerifyOtp));
            }

            var otpCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var otp = new PasswordResetOtp
            {
                UserId = user.Id,
                OtpCode = otpCode,
                OtpExpiry = TimeHelper.VietnamNow().AddMinutes(10)
            };
            await _unitOfWork.PasswordResetOtps.AddAsync(otp);
            await _unitOfWork.SaveAsync();

            try
            {
                var emailModel = new OtpEmailViewModel
                {
                    FullName = user.FullName ?? user.Email ?? string.Empty,
                    OtpCode = otpCode
                };
                var body = await _viewRenderer.RenderAsync(
                    "~/Views/Auth/Emails/OtpEmail.cshtml", emailModel);
                await _emailService.SendAsync(user.Email!, user.FullName ?? user.Email!, "Mã OTP đặt lại mật khẩu SmartShop", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", model.Email);
                TempData["Error"] = "Không thể gửi email. Vui lòng thử lại sau.";
                return View(model);
            }

            TempData["Email"] = model.Email;
            return RedirectToAction(nameof(VerifyOtp));
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = TempData["Email"]?.ToString();
            if (string.IsNullOrEmpty(email))
                return RedirectToAction(nameof(ForgotPassword));

            TempData.Keep("Email");
            return View(new VerifyOtpViewModel { Email = email });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var otp = await _unitOfWork.PasswordResetOtps.GetValidOtpAsync(user.Id, model.OtpCode);
                if (otp != null)
                {
                    TempData["Email"] = model.Email;
                    TempData["OtpCode"] = model.OtpCode;
                    return RedirectToAction(nameof(ResetPassword));
                }
            }

            ModelState.AddModelError(string.Empty, "Mã OTP không đúng hoặc đã hết hạn.");
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var email = TempData["Email"]?.ToString();
            var otpCode = TempData["OtpCode"]?.ToString();
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otpCode))
                return RedirectToAction(nameof(ForgotPassword));

            TempData.Keep("Email");
            TempData.Keep("OtpCode");
            return View(new ResetPasswordViewModel { Email = email, OtpCode = otpCode });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Success"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
                return RedirectToAction(nameof(Login));
            }

            var otp = await _unitOfWork.PasswordResetOtps.GetValidOtpAsync(user.Id, model.OtpCode);
            if (otp == null)
            {
                ModelState.AddModelError(string.Empty, "Phiên đặt lại mật khẩu đã hết hạn. Vui lòng thử lại.");
                return View(model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            otp.IsUsed = true;
            _unitOfWork.PasswordResetOtps.Update(otp);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Password reset for {Email}", model.Email);
            TempData["Success"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
    }
}
