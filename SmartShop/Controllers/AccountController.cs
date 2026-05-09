using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Models.ViewModels;

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

        public IActionResult Index() => RedirectToAction(nameof(Profile));

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
        public async Task<IActionResult> AddAddressAjax(AddAddressViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Chưa đăng nhập!" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin!" });

            if (model.IsDefault)
            {
                var existing = (await _unitOfWork.Addresses.FindAsync(a => a.UserId == user.Id)).ToList();
                foreach (var addr in existing)
                    addr.IsDefault = false;
            }

            var address = new Address
            {
                UserId = user.Id,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Province = model.Province,
                District = model.District,
                Ward = model.Ward,
                StreetLine = model.StreetLine,
                IsDefault = model.IsDefault
            };

            await _unitOfWork.Addresses.AddAsync(address);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Thêm địa chỉ thành công!", addressId = address.Id, isDefault = address.IsDefault });
        }

        [HttpGet]
        public async Task<IActionResult> AddressCard(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var address = await _unitOfWork.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (address == null) return NotFound();

            return PartialView("_AddressCard", address);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddressAjax(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Json(new { success = false, message = "Chưa đăng nhập!" });

                var address = await _unitOfWork.Addresses.FirstOrDefaultAsync(a => a.Id == id);
                if (address == null)
                    return Json(new { success = false, message = "Không tìm thấy địa chỉ" });
                if (address.UserId != user.Id)
                    return Json(new { success = false, message = "Bạn không có quyền xóa địa chỉ này" });

                if (address.IsDefault)
                {
                    var remaining = (await _unitOfWork.Addresses.FindAsync(
                        a => a.UserId == user.Id && a.Id != id)).ToList();
                    if (remaining.Count > 0)
                        remaining[0].IsDefault = true;
                }

                address.IsDeleted = true;
                await _unitOfWork.SaveAsync();

                return Json(new { success = true, message = "Xóa địa chỉ thành công" });
            }
            catch
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, vui lòng thử lại." });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Có lỗi xảy ra, vui lòng thử lại." });

            var addresses = (await _unitOfWork.Addresses.FindAsync(a => a.UserId == user.Id)).ToList();
            if (!addresses.Any(a => a.Id == id))
                return Json(new { success = false, message = "Có lỗi xảy ra, vui lòng thử lại." });

            foreach (var addr in addresses)
                addr.IsDefault = (addr.Id == id);

            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Đã đặt làm địa chỉ mặc định" });
        }
    }
}
