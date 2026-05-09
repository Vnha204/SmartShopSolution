using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Utility;

namespace SmartShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IUnitOfWork unitOfWork, ILogger<OrderController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET /Admin/Order?q=&status=&paymentStatus=&from=&to=
        public async Task<IActionResult> Index(
            string? q             = null,
            string? status        = null,
            string? paymentStatus = null,
            string? from          = null,
            string? to            = null)
        {
            var orders = (await _unitOfWork.Orders.GetAllWithDetailsAsync()).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(q))
                orders = orders.Where(o => o.OrderCode.Contains(q, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(status))
                orders = orders.Where(o => o.Status == status);

            if (!string.IsNullOrWhiteSpace(paymentStatus))
                orders = orders.Where(o => o.PaymentStatus == paymentStatus);

            if (DateTime.TryParse(from, out var fromDate))
                orders = orders.Where(o => o.CreatedAt.Date >= fromDate.Date);

            if (DateTime.TryParse(to, out var toDate))
                orders = orders.Where(o => o.CreatedAt.Date <= toDate.Date);

            ViewBag.Q             = q;
            ViewBag.Status        = status;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.From          = from;
            ViewBag.To            = to;

            return View(orders.ToList());
        }

        // GET /Admin/Order/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var order = await _unitOfWork.Orders.GetWithDetailsAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        // POST /Admin/Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var validStatuses = new[]
            {
                OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Processing,
                OrderStatus.Shipping, OrderStatus.Delivered, OrderStatus.Cancelled
            };

            if (string.IsNullOrEmpty(status) || !validStatuses.Contains(status))
            {
                TempData["Error"] = "Trạng thái không hợp lệ!";
                return RedirectToAction(nameof(Index));
            }

            var order = await _unitOfWork.Orders.GetWithDetailsAsync(id);
            if (order == null) return NotFound();

            // Restore stock exactly once when cancelling
            if (status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock    += item.Quantity;
                        product.UpdatedAt = TimeHelper.VietnamNow();
                        _unitOfWork.Products.Update(product);
                    }
                }
                _logger.LogInformation("Stock restored for cancelled order #{Code}", order.OrderCode);
            }

            order.Status    = status;
            order.UpdatedAt = TimeHelper.VietnamNow();
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Order #{Code} status → {Status}", order.OrderCode, status);
            TempData["Success"] = $"Đơn #{order.OrderCode} → {OrderStatus.DisplayNames.GetValueOrDefault(status, status)}";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /Admin/Order/UpdatePaymentStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int id, string paymentStatus)
        {
            var valid = new[] { "unpaid", "pending", "paid" };
            if (string.IsNullOrEmpty(paymentStatus) || !valid.Contains(paymentStatus))
            {
                TempData["Error"] = "Trạng thái thanh toán không hợp lệ!";
                return RedirectToAction(nameof(Details), new { id });
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            if (order == null) return NotFound();

            order.PaymentStatus = paymentStatus;
            order.UpdatedAt     = DateTime.UtcNow;
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveAsync();

            var label = paymentStatus switch {
                "paid"    => "Đã thanh toán",
                "pending" => "Chờ thanh toán",
                _         => "Chưa thanh toán"
            };
            _logger.LogInformation("Order #{Code} PaymentStatus → {PS}", order.OrderCode, paymentStatus);
            TempData["Success"] = $"Đơn #{order.OrderCode} — thanh toán: {label}";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
