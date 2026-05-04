using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Models.ViewModels;

namespace SmartShop.Areas.Customers.Controllers
{
    [Area("Customers")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CartController> _logger;

        public CartController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager, ILogger<CartController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        // GET /Customers/Cart
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var cart = await _unitOfWork.Carts.GetWithItemsAsync(userId);

            var vm = new CartViewModel();
            if (cart != null)
            {
                vm.CartId = cart.CartId;
                vm.Items = cart.Items.Select(ci => new CartItemViewModel
                {
                    CartItemId       = ci.CartItemId,
                    ProductId        = ci.ProductId,
                    ProductName      = ci.Product?.Name ?? string.Empty,
                    ThumbnailUrl     = ci.Product?.ThumbnailUrl,
                    Quantity         = ci.Quantity,
                    Stock            = ci.Product?.Stock ?? 0,
                    UnitPriceSnapshot = ci.UnitPriceSnapshot
                }).ToList();
            }

            return View(vm);
        }

        // POST /Customers/Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null || !product.IsActive)
            {
                if (isAjax) return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                TempData["Error"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index");
            }

            if (product.Stock <= 0)
            {
                if (isAjax) return Json(new { success = false, message = $"\"{product.Name}\" đã hết hàng." });
                TempData["Error"] = $"\"{product.Name}\" đã hết hàng.";
                return RedirectToAction("Index");
            }

            var userId = _userManager.GetUserId(User)!;

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                await _unitOfWork.Carts.AddAsync(cart);
                await _unitOfWork.SaveAsync();
            }

            var existing = await _unitOfWork.CartItems.GetByCartAndProductAsync(cart.CartId, productId);
            if (existing != null)
            {
                existing.Quantity = Math.Min(existing.Quantity + quantity, product.Stock);
                _unitOfWork.CartItems.Update(existing);
            }
            else
            {
                await _unitOfWork.CartItems.AddAsync(new CartItem
                {
                    CartId            = cart.CartId,
                    ProductId         = productId,
                    Quantity          = Math.Min(quantity, product.Stock),
                    UnitPriceSnapshot = product.SalePrice ?? product.BasePrice,
                    AddedAt           = DateTime.UtcNow
                });
            }

            cart.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Carts.Update(cart);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("User {UserId} added product {ProductId} to cart.", userId, productId);

            if (isAjax) return Json(new { success = true, message = $"Đã thêm \"{product.Name}\" vào giỏ hàng." });
            TempData["Success"] = $"Đã thêm \"{product.Name}\" vào giỏ hàng.";
            return RedirectToAction("Index");
        }

        // POST /Customers/Cart/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var userId = _userManager.GetUserId(User)!;
            var item = await _unitOfWork.CartItems.GetByIdAsync(cartItemId);

            if (item == null)
            {
                if (isAjax) return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ." });
                TempData["Error"] = "Không tìm thấy sản phẩm trong giỏ.";
                return RedirectToAction("Index");
            }

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
            if (cart == null || item.CartId != cart.CartId)
            {
                if (isAjax) return Json(new { success = false, message = "Thao tác không hợp lệ." });
                TempData["Error"] = "Thao tác không hợp lệ.";
                return RedirectToAction("Index");
            }

            if (quantity <= 0)
            {
                _unitOfWork.CartItems.Remove(item);
            }
            else
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                item.Quantity = product != null ? Math.Min(quantity, product.Stock) : quantity;
                _unitOfWork.CartItems.Update(item);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Carts.Update(cart);
            await _unitOfWork.SaveAsync();

            if (isAjax) return Json(new { success = true, message = "Đã cập nhật giỏ hàng.", cart = await BuildCartAjaxDataAsync(userId) });
            TempData["Success"] = "Đã cập nhật giỏ hàng.";
            return RedirectToAction("Index");
        }

        // POST /Customers/Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var userId = _userManager.GetUserId(User)!;
            var item = await _unitOfWork.CartItems.GetByIdAsync(cartItemId);

            if (item == null)
            {
                if (isAjax) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index");
            }

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
            if (cart == null || item.CartId != cart.CartId)
            {
                if (isAjax) return Json(new { success = false, message = "Thao tác không hợp lệ." });
                TempData["Error"] = "Thao tác không hợp lệ.";
                return RedirectToAction("Index");
            }

            _unitOfWork.CartItems.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Carts.Update(cart);
            await _unitOfWork.SaveAsync();

            if (isAjax) return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng.", cart = await BuildCartAjaxDataAsync(userId) });
            TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            return RedirectToAction("Index");
        }

        // POST /Customers/Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var userId = _userManager.GetUserId(User)!;
            var cart = await _unitOfWork.Carts.GetWithItemsAsync(userId);

            if (cart != null && cart.Items.Any())
            {
                _unitOfWork.CartItems.RemoveRange(cart.Items);
                cart.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Carts.Update(cart);
                await _unitOfWork.SaveAsync();
            }

            if (isAjax) return Json(new { success = true, message = "Đã xóa tất cả sản phẩm trong giỏ hàng.", cart = await BuildCartAjaxDataAsync(userId) });
            TempData["Success"] = "Đã xóa tất cả sản phẩm trong giỏ hàng.";
            return RedirectToAction("Index");
        }

        private async Task<object> BuildCartAjaxDataAsync(string userId)
        {
            var cart = await _unitOfWork.Carts.GetWithItemsAsync(userId);
            if (cart == null || !cart.Items.Any())
                return new { badgeCount = 0, totalQuantity = 0, total = 0m, isEmpty = true, items = Array.Empty<object>() };

            var items = cart.Items.Select(ci => new
            {
                cartItemId  = ci.CartItemId,
                productName = ci.Product?.Name ?? string.Empty,
                thumbnailUrl = ci.Product?.ThumbnailUrl,
                quantity    = ci.Quantity,
                stock       = ci.Product?.Stock ?? 0,
                unitPrice   = ci.UnitPriceSnapshot,
                subtotal    = ci.UnitPriceSnapshot * ci.Quantity
            }).ToArray();

            var total    = items.Sum(i => i.subtotal);
            var totalQty = items.Sum(i => i.quantity);

            return new { badgeCount = totalQty, totalQuantity = totalQty, total, isEmpty = false, items };
        }
    }
}
