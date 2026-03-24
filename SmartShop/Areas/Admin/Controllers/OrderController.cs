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

        public async Task<IActionResult> Index()
        {
            var orders = await _unitOfWork.Orders.GetAllWithDetailsAsync();
            return View(orders);
        }

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

            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            if (order == null) return NotFound();

            order.Status    = status;
            order.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Order #{Code} status updated to {Status}", order.OrderCode, status);
            TempData["Success"] = $"Đơn #{order.OrderCode} đã được cập nhật sang: {status}";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Detail(int id)
        {
            var order = await _unitOfWork.Orders.GetWithDetailsAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}
