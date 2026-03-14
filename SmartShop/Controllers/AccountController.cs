using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Models;

namespace SmartShop.Controllers
{
    [Authorize]  // Bắt buộc phải đăng nhập
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public AccountController(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET /account → redirect sang đơn hàng
        public IActionResult Index() => RedirectToAction(nameof(Orders));

        // ===== Đơn hàng =====
        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // ===== Địa chỉ =====
        public async Task<IActionResult> Addresses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var addresses = await _context.Addresses
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            return View(addresses);
        }
        // ===== Thêm địa chỉ mới =====
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(Address model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            model.UserId = user.Id;

            // Nếu đặt mặc định thì bỏ mặc định tất cả địa chỉ cũ
            if (model.IsDefault)
            {
                var existingAddresses = _context.Addresses.Where(a => a.UserId == user.Id);
                foreach (var addr in existingAddresses)
                    addr.IsDefault = false;
            }

            _context.Addresses.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm địa chỉ thành công!";
            return RedirectToAction(nameof(Addresses));
        }
        // ===== Thông tin tài khoản =====
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");
            return View(user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(string fullName, string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction(nameof(Profile));
        }
    }
}
