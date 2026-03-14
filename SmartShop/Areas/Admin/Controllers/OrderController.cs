using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Models;
using SmartShop.Utility;

namespace SmartShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        public OrderController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return View(orders);
        }

        // AJAX: Cập nhật trạng thái đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken] // ✅ FIX: Thêm bảo mật CSRF
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            // ✅ FIX: Validate status hợp lệ
            var validStatuses = new[] { 
                OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Processing,
                OrderStatus.Shipping, OrderStatus.Delivered, OrderStatus.Cancelled 
            };
            
            if (string.IsNullOrEmpty(status) || !validStatuses.Contains(status))
            {
                TempData["Error"] = "Trạng thái không hợp lệ!";
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đơn #{order.OrderCode} đã được cập nhật sang: {status}";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Detail(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }
    }
}
