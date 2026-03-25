using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IUnitOfWork        _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IVietQrApiClient   _vietQrApi;
        private readonly VietQRSettings     _vietQR;
        private readonly IMemoryCache       _cache;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IVietQrApiClient vietQrApi,
            IOptions<VietQRSettings> vietQROptions,
            IMemoryCache cache,
            ILogger<PaymentsController> logger)
        {
            _unitOfWork  = unitOfWork;
            _userManager = userManager;
            _vietQrApi   = vietQrApi;
            _vietQR      = vietQROptions.Value;
            _cache       = cache;
            _logger      = logger;
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

            var transferContent = $"ORDER_{order.OrderCode}";

            string? qrDataUrl = null;
            if (!string.Equals(order.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                var cacheKey = $"vietqr_qr_{orderId}";
                if (!_cache.TryGetValue(cacheKey, out qrDataUrl))
                {
                    try
                    {
                        qrDataUrl = await _vietQrApi.GetQrDataUrlAsync(
                            bankBin:     _vietQR.BankBin,
                            accountNo:   _vietQR.AccountNumber,
                            accountName: _vietQR.AccountName,
                            amount:      (long)order.TotalPrice,
                            addInfo:     transferContent);

                        _cache.Set(cacheKey, qrDataUrl, TimeSpan.FromMinutes(10));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to fetch VietQR for order {OrderId}", orderId);
                        // qrDataUrl remains null — view will show an error message instead of the QR
                    }
                }
            }

            var vm = new VietQRViewModel
            {
                Order           = order,
                BankName        = _vietQR.BankName,
                AccountNumber   = _vietQR.AccountNumber,
                AccountName     = _vietQR.AccountName,
                TransferContent = transferContent,
                QrDataUrl       = qrDataUrl
            };

            return View(vm);
        }
    }
}
