using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Models;

namespace SmartShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MemberController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<MemberController> _logger;

        public MemberController(UserManager<AppUser> userManager, ILogger<MemberController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            var model = new List<MemberListItem>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                model.Add(new MemberListItem
                {
                    Id         = u.Id,
                    Email      = u.Email ?? string.Empty,
                    FullName   = u.FullName,
                    Role       = roles.FirstOrDefault() ?? "User",
                    CreatedAt  = u.CreatedAt,
                    IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isCurrentlyLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

            if (isCurrentlyLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                _logger.LogInformation("Account unlocked: {Email}", user.Email);
                TempData["Success"] = $"Tài khoản {user.Email} đã được kích hoạt lại.";
            }
            else
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                _logger.LogInformation("Account locked: {Email}", user.Email);
                TempData["Success"] = $"Tài khoản {user.Email} đã bị vô hiệu hoá.";
            }

            return RedirectToAction(nameof(Index));
        }

        public class MemberListItem
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public bool IsLockedOut { get; set; }
        }
    }
}
