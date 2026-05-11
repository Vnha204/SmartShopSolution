using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Services;
using SmartShop.Utility;

namespace SmartShop.Areas.Customers.Controllers
{
    [Area("Customers")]
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly IUnitOfWork                 _unitOfWork;
        private readonly UserManager<AppUser>        _userManager;
        private readonly IPayOSService               _payOS;
        private readonly IConfiguration              _config;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IUnitOfWork                 unitOfWork,
            UserManager<AppUser>        userManager,
            IPayOSService               payOS,
            IConfiguration              config,
            ILogger<PaymentsController> logger)
        {
            _unitOfWork  = unitOfWork;
            _userManager = userManager;
            _payOS       = payOS;
            _config      = config;
            _logger      = logger;
        }

        // Customers/Payments/PayOS?orderId={id}
        // tạo PayOS payment link, chuyển route
        public async Task<IActionResult> PayOS(int orderId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (order.UserId != userId) return Forbid();

            if (!string.Equals(order.PaymentMethod, "payos", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("OrderDetail", "Account", new { area = "", orderId });

            
            if (string.Equals(order.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(PayOSReturn), new { orderId });

            // Tái sử dụng link thanh toán cũ để tránh lỗi PayOS 231
            if (!string.IsNullOrWhiteSpace(order.PayOSPaymentLinkId) &&
                !string.IsNullOrWhiteSpace(order.PayOSCheckoutUrl) &&
                (string.Equals(order.PaymentStatus, "pending", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(order.PaymentStatus, "unpaid",  StringComparison.OrdinalIgnoreCase)))
            {
                return Redirect(order.PayOSCheckoutUrl);
            }

            var baseUrl =
                !string.IsNullOrWhiteSpace(_config["App:PublicBaseUrl"]) ? _config["App:PublicBaseUrl"]!
                : $"{Request.Scheme}://{Request.Host}";

            var returnUrl = $"{baseUrl}/Customers/Payments/PayOSReturn?orderId={orderId}";
            var cancelUrl = $"{baseUrl}/Customers/Payments/PayOSReturn?orderId={orderId}&cancelled=true";

            var description = order.OrderCode.Length > 25
                ? order.OrderCode[..25]
                : order.OrderCode;

            var result = await _payOS.CreatePaymentLinkAsync(
                orderCode:   order.Id,
                amount:      (int)order.TotalPrice,
                description: description,
                returnUrl:   returnUrl,
                cancelUrl:   cancelUrl);

            if (!result.Success || string.IsNullOrEmpty(result.CheckoutUrl))
            {
                _logger.LogError("Failed to create PayOS link for order {OrderId}: {Error}",
                    orderId, result.ErrorMessage);
                TempData["Error"] = result.ErrorMessage ?? "Không thể tạo liên kết thanh toán. Vui lòng thử lại.";
                return RedirectToAction("OrderDetail", "Account", new { area = "", orderId });
            }

            // Lưu ID link thanh toán để webhook xác định đúng đơn hàng
            order.PayOSPaymentLinkId = result.PaymentLinkId;
            order.PayOSOrderCode     = result.OrderCode;
            order.PayOSCheckoutUrl   = result.CheckoutUrl;
            order.UpdatedAt          = TimeHelper.VietnamNow();
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveAsync();

            return Redirect(result.CheckoutUrl);
        }
        // Trang xử lý khi khách hàng quay lại từ PayOS sau thanh toán/hủy thanh toán
        public async Task<IActionResult> PayOSReturn(int orderId, bool cancelled = false)
        {
            var order = await _unitOfWork.Orders.GetWithDetailsAsync(orderId);
            if (order == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (order.UserId != userId) return Forbid();

            ViewData["Cancelled"] = cancelled;
            return View("PayOS", order);
        }

        // GET /Customers/Payments/PaymentStatus?orderId={id}
        [HttpGet]
        public async Task<IActionResult> PaymentStatus(int orderId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (order.UserId != userId) return Forbid();

            return Json(new
            {
                status  = order.PaymentStatus,
                orderId = order.Id
            });
        }
    }
}
