using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;

namespace SmartShop.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
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

        public async Task<IActionResult> ProfilePartial()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            return PartialView("_ProfilePartial", user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy tài khoản!" });

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
                return Json(new { success = true, message = "Cập nhật thông tin thành công!" });

            return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại!" });
        }

        // ===== ORDERS =====
        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var orders = await _unitOfWork.Orders.GetByUserIdAsync(user.Id);
            return View(orders);
        }

        public async Task<IActionResult> OrdersPartial()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var orders = await _unitOfWork.Orders.GetByUserIdAsync(user.Id);
            return PartialView("_OrdersPartial", orders);
        }

        public async Task<IActionResult> OrderDetail(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var order = await _unitOfWork.Orders.GetWithDetailsAsync(orderId);
            if (order == null) return NotFound();
            if (order.UserId != user.Id) return Forbid();

            return View(order);
        }

        // ===== ADDRESSES =====
        public async Task<IActionResult> Addresses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var addresses = await _unitOfWork.Addresses.GetByUserIdAsync(user.Id);
            return View(addresses);
        }

        public async Task<IActionResult> AddressesPartial()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var addresses = await _unitOfWork.Addresses.GetByUserIdAsync(user.Id);
            return PartialView("_AddressesPartial", addresses);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddressAjax(Address model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Chưa đăng nhập!" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin!" });

            model.UserId = user.Id;

            if (model.IsDefault)
            {
                var existing = (await _unitOfWork.Addresses.FindAsync(a => a.UserId == user.Id)).ToList();
                foreach (var addr in existing)
                    addr.IsDefault = false;
            }

            await _unitOfWork.Addresses.AddAsync(model);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Đã thêm địa chỉ thành công!" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddressAjax(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Chưa đăng nhập!" });

            var address = await _unitOfWork.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (address == null)
                return Json(new { success = false, message = "Địa chỉ không tồn tại!" });

            if (await _unitOfWork.Addresses.IsUsedInOrderAsync(id))
                return Json(new { success = false, message = "Không thể xóa địa chỉ đang được sử dụng trong đơn hàng!" });

            _unitOfWork.Addresses.Remove(address);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Đã xóa địa chỉ thành công!" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddressAjax(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Chưa đăng nhập!" });

            var addresses = (await _unitOfWork.Addresses.FindAsync(a => a.UserId == user.Id)).ToList();
            foreach (var addr in addresses)
                addr.IsDefault = (addr.Id == id);

            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Đã cập nhật địa chỉ mặc định!" });
        }

    }
}
