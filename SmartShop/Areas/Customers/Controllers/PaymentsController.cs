using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Models.ViewModels;
using SmartShop.Services;

namespace SmartShop.Areas.Customers.Controllers
{
    [Area("Customers")]
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IQrCodeService _qrCodeService;
        private readonly VietQRSettings _vietQR;

        public PaymentsController(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IQrCodeService qrCodeService,
            IOptions<VietQRSettings> vietQROptions)
        {
            _unitOfWork    = unitOfWork;
            _userManager   = userManager;
            _qrCodeService = qrCodeService;
            _vietQR        = vietQROptions.Value;
        }

        // GET /Customers/Payments/VietQR?orderId={id}
        public async Task<IActionResult> VietQR(int orderId)
        {
            var order = await _unitOfWork.Orders.GetWithDetailsAsync(orderId);
            if (order == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (order.UserId != userId) return Forbid();

            if (!string.Equals(order.PaymentMethod, "vietqr", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Details", "Orders", new { orderId });

            var vm = new VietQRViewModel
            {
                Order           = order,
                BankName        = _vietQR.BankName,
                AccountNumber   = _vietQR.AccountNumber,
                AccountName     = _vietQR.AccountName,
                TransferContent = $"ORDER_{order.OrderCode}"
            };

            return View(vm);
        }

        // GET /Customers/Payments/VietQRImage?orderId={id}
        // Returns image/png — owner-only
        public async Task<IActionResult> VietQRImage(int orderId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (order.UserId != userId) return Forbid();

            var transferContent = $"ORDER_{order.OrderCode}";
            var payload = BuildQrPayload(order, transferContent);
            var png = _qrCodeService.GeneratePng(payload, pixelsPerModule: 8);

            return File(png, "image/png");
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private string BuildQrPayload(Order order, string transferContent)
        {
            // Plain-text payload for demo QR — real integration would use EMVCo / VietQR spec
            return string.Join("\n",
                $"Ngan hang : {_vietQR.BankName}",
                $"So TK     : {_vietQR.AccountNumber}",
                $"Chu TK    : {_vietQR.AccountName}",
                $"So tien   : {order.TotalPrice:N0} VND",
                $"Noi dung  : {transferContent}");
        }
    }
}
