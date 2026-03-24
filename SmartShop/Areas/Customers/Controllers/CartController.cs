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
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null || !product.IsActive)
            {
                TempData["Error"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index");
            }

            if (product.Stock <= 0)
            {
                TempData["Error"] = $"\"{product.Name}\" đã hết hàng.";
                return RedirectToAction("Index");
            }

            var userId = _userManager.GetUserId(User)!;

            // Get or create cart
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
            TempData["Success"] = $"Đã thêm \"{product.Name}\" vào giỏ hàng.";
            return RedirectToAction("Index");
        }

        // POST /Customers/Cart/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            var userId = _userManager.GetUserId(User)!;
            var item = await _unitOfWork.CartItems.GetByIdAsync(cartItemId);

            if (item == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm trong giỏ.";
                return RedirectToAction("Index");
            }

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
            if (cart == null || item.CartId != cart.CartId)
            {
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

            TempData["Success"] = "Đã cập nhật giỏ hàng.";
            return RedirectToAction("Index");
        }

        // POST /Customers/Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var userId = _userManager.GetUserId(User)!;
            var item = await _unitOfWork.CartItems.GetByIdAsync(cartItemId);

            if (item == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index");
            }

            var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
            if (cart == null || item.CartId != cart.CartId)
            {
                TempData["Error"] = "Thao tác không hợp lệ.";
                return RedirectToAction("Index");
            }

            _unitOfWork.CartItems.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Carts.Update(cart);
            await _unitOfWork.SaveAsync();

            TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            return RedirectToAction("Index");
        }

        // POST /Customers/Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var userId = _userManager.GetUserId(User)!;
            var cart = await _unitOfWork.Carts.GetWithItemsAsync(userId);

            if (cart != null && cart.Items.Any())
            {
                _unitOfWork.CartItems.RemoveRange(cart.Items);
                cart.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Carts.Update(cart);
                await _unitOfWork.SaveAsync();
            }

            TempData["Success"] = "Đã xóa tất cả sản phẩm trong giỏ hàng.";
            return RedirectToAction("Index");
        }
    }
}
