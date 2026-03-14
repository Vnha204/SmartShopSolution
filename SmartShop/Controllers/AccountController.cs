using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Models;

namespace SmartShop.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public AccountController(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // ===== INDEX - Redirect to Profile =====
        public IActionResult Index() => RedirectToAction(nameof(Profile));

        // ===== PROFILE =====
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");
            return View(user);
        }

        // AJAX Partial - Profile
        public async Task<IActionResult> ProfilePartial()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            return PartialView("_ProfilePartial", user);
        }

        // AJAX Update Profile
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy tài khoản!" });

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;

            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
            }
            
            return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại!" });
        }

        // ===== ORDERS =====
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

        // AJAX Partial - Orders
        public async Task<IActionResult> OrdersPartial()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return PartialView("_OrdersPartial", orders);
        }

        // ===== ADDRESSES =====
        public async Task<IActionResult> Addresses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var addresses = await _context.Addresses
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

            return View(addresses);
        }

        // AJAX Partial - Addresses
        public async Task<IActionResult> AddressesPartial()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var addresses = await _context.Addresses
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

            return PartialView("_AddressesPartial", addresses);
        }

        // AJAX Add Address
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddressAjax(Address model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Chưa đăng nhập!" });

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin!" });
            }

            model.UserId = user.Id;

            if (model.IsDefault)
            {
                var existingAddresses = await _context.Addresses
                    .Where(a => a.UserId == user.Id)
                    .ToListAsync();
                foreach (var addr in existingAddresses)
                    addr.IsDefault = false;
            }

            _context.Addresses.Add(model);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã thêm địa chỉ thành công!" });
        }

        // AJAX Delete Address
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddressAjax(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Chưa đăng nhập!" });

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

            if (address == null)
            {
                return Json(new { success = false, message = "Địa chỉ không tồn tại!" });
            }

            var isUsedInOrder = await _context.Orders.AnyAsync(o => o.AddressId == id);
            if (isUsedInOrder)
            {
                return Json(new { success = false, message = "Không thể xóa địa chỉ đang được sử dụng trong đơn hàng!" });
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa địa chỉ thành công!" });
        }

        // AJAX Set Default Address
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddressAjax(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Chưa đăng nhập!" });

            var addresses = await _context.Addresses
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            foreach (var addr in addresses)
                addr.IsDefault = (addr.Id == id);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã cập nhật địa chỉ mặc định!" });
        }

        // ===== Các method cũ để giữ backward compatibility =====
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(Address model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin địa chỉ!";
                return RedirectToAction(nameof(Addresses));
            }

            model.UserId = user.Id;

            if (model.IsDefault)
            {
                var existingAddresses = await _context.Addresses
                    .Where(a => a.UserId == user.Id)
                    .ToListAsync();
                foreach (var addr in existingAddresses)
                    addr.IsDefault = false;
            }

            _context.Addresses.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm địa chỉ thành công!";
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

            if (address == null)
            {
                TempData["Error"] = "Địa chỉ không tồn tại!";
                return RedirectToAction(nameof(Addresses));
            }

            var isUsedInOrder = await _context.Orders.AnyAsync(o => o.AddressId == id);
            if (isUsedInOrder)
            {
                TempData["Error"] = "Không thể xóa địa chỉ đang được sử dụng trong đơn hàng!";
                return RedirectToAction(nameof(Addresses));
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa địa chỉ thành công!";
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var addresses = await _context.Addresses
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            foreach (var addr in addresses)
                addr.IsDefault = (addr.Id == id);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật địa chỉ mặc định!";
            return RedirectToAction(nameof(Addresses));
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
