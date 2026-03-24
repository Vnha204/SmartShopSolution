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
    public class CheckoutController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public CheckoutController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // GET /Customers/Checkout
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var cart = await _unitOfWork.Carts.GetWithItemsAsync(userId);

            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            var addresses = (await _unitOfWork.Addresses.GetByUserIdAsync(userId)).ToList();

            var vm = new CheckoutViewModel
            {
                Items = cart.Items.Select(ci => new CartItemViewModel
                {
                    CartItemId        = ci.CartItemId,
                    ProductId         = ci.ProductId,
                    ProductName       = ci.Product?.Name ?? string.Empty,
                    ThumbnailUrl      = ci.Product?.ThumbnailUrl,
                    Quantity          = ci.Quantity,
                    Stock             = ci.Product?.Stock ?? 0,
                    UnitPriceSnapshot = ci.UnitPriceSnapshot
                }).ToList(),
                Addresses         = addresses,
                SelectedAddressId = addresses.FirstOrDefault(a => a.IsDefault)?.Id
                                 ?? addresses.FirstOrDefault()?.Id
            };

            return View(vm);
        }

        // POST /Customers/Checkout/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel vm)
        {
            var userId = _userManager.GetUserId(User)!;

            // Reload live cart (never trust the posted items)
            var cart = await _unitOfWork.Carts.GetWithItemsAsync(userId);
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            // ── Resolve / validate address ────────────────────────────────────
            Address? address = null;
            if (vm.SelectedAddressId.HasValue && vm.SelectedAddressId > 0)
            {
                address = await _unitOfWork.Addresses.GetByIdAsync(vm.SelectedAddressId.Value);
                if (address == null || address.UserId != userId)
                    ModelState.AddModelError("SelectedAddressId", "Địa chỉ không hợp lệ.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(vm.NewFullName))
                    ModelState.AddModelError("NewFullName", "Vui lòng nhập họ tên.");
                if (string.IsNullOrWhiteSpace(vm.NewPhone))
                    ModelState.AddModelError("NewPhone", "Vui lòng nhập số điện thoại.");
                if (string.IsNullOrWhiteSpace(vm.NewProvince))
                    ModelState.AddModelError("NewProvince", "Vui lòng nhập tỉnh/thành phố.");
                if (string.IsNullOrWhiteSpace(vm.NewDistrict))
                    ModelState.AddModelError("NewDistrict", "Vui lòng nhập quận/huyện.");
                if (string.IsNullOrWhiteSpace(vm.NewWard))
                    ModelState.AddModelError("NewWard", "Vui lòng nhập phường/xã.");
                if (string.IsNullOrWhiteSpace(vm.NewStreetLine))
                    ModelState.AddModelError("NewStreetLine", "Vui lòng nhập địa chỉ cụ thể.");
            }

            // ── Sanitise payment method ───────────────────────────────────────
            if (vm.PaymentMethod != "cod" && vm.PaymentMethod != "vietqr")
                vm.PaymentMethod = "cod";

            if (!ModelState.IsValid)
            {
                vm.Addresses = (await _unitOfWork.Addresses.GetByUserIdAsync(userId)).ToList();
                vm.Items = cart.Items.Select(ci => new CartItemViewModel
                {
                    CartItemId        = ci.CartItemId,
                    ProductId         = ci.ProductId,
                    ProductName       = ci.Product?.Name ?? string.Empty,
                    ThumbnailUrl      = ci.Product?.ThumbnailUrl,
                    Quantity          = ci.Quantity,
                    Stock             = ci.Product?.Stock ?? 0,
                    UnitPriceSnapshot = ci.UnitPriceSnapshot
                }).ToList();
                return View("Index", vm);
            }

            // ── Stock validation ──────────────────────────────────────────────
            foreach (var ci in cart.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(ci.ProductId);
                if (product == null || !product.IsActive)
                {
                    TempData["Error"] = $"Sản phẩm \"{ci.Product?.Name ?? ci.ProductId.ToString()}\" không còn tồn tại.";
                    return RedirectToAction("Index", "Cart");
                }
                if (product.Stock < ci.Quantity)
                {
                    TempData["Error"] = $"Sản phẩm \"{product.Name}\" chỉ còn {product.Stock} trong kho, không đủ để đặt {ci.Quantity}.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // ── Save inline address if provided ───────────────────────────────
            if (address == null)
            {
                address = new Address
                {
                    UserId      = userId,
                    FullName    = vm.NewFullName!.Trim(),
                    PhoneNumber = vm.NewPhone!.Trim(),
                    Province    = vm.NewProvince!.Trim(),
                    District    = vm.NewDistrict!.Trim(),
                    Ward        = vm.NewWard!.Trim(),
                    StreetLine  = vm.NewStreetLine!.Trim(),
                    IsDefault   = true
                };
                await _unitOfWork.Addresses.AddAsync(address);
                await _unitOfWork.SaveAsync();
            }

            // ── Generate unique OrderCode ─────────────────────────────────────
            string orderCode;
            var rng = new Random();
            do
            {
                var suffix = new string(Enumerable.Range(0, 5)
                    .Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[rng.Next(36)])
                    .ToArray());
                orderCode = "OD" + DateTime.UtcNow.ToString("yyyyMMdd") + suffix;
            }
            while (await _unitOfWork.Orders.AnyAsync(o => o.OrderCode == orderCode));

            // ── Create Order ──────────────────────────────────────────────────
            var order = new Order
            {
                UserId        = userId,
                AddressId     = address.Id,
                OrderCode     = orderCode,
                Status        = OrderStatus.Pending,
                PaymentMethod = vm.PaymentMethod,
                PaymentStatus = vm.PaymentMethod == "vietqr" ? "pending" : "unpaid",
                TotalPrice    = cart.Items.Sum(ci => ci.UnitPriceSnapshot * ci.Quantity),
                Note          = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim(),
                CreatedAt     = DateTime.UtcNow,
                UpdatedAt     = DateTime.UtcNow
            };
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveAsync(); // flush to get Order.Id

            // ── Create OrderItems + decrement stock ───────────────────────────
            foreach (var ci in cart.Items)
            {
                await _unitOfWork.OrderItems.AddAsync(new OrderItem
                {
                    OrderId     = order.Id,
                    ProductId   = ci.ProductId,
                    ProductName = ci.Product?.Name ?? string.Empty,
                    UnitPrice   = ci.UnitPriceSnapshot,
                    Quantity    = ci.Quantity
                });

                var product = await _unitOfWork.Products.GetByIdAsync(ci.ProductId);
                if (product != null)
                {
                    product.Stock     = Math.Max(0, product.Stock - ci.Quantity);
                    product.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Products.Update(product);
                }
            }

            // ── Clear cart ────────────────────────────────────────────────────
            _unitOfWork.CartItems.RemoveRange(cart.Items);
            cart.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Carts.Update(cart);

            await _unitOfWork.SaveAsync();

            // ── Redirect ──────────────────────────────────────────────────────
            TempData["Success"] = $"Đặt hàng thành công! Mã đơn: {order.OrderCode}";

            if (vm.PaymentMethod == "vietqr")
                return RedirectToAction("VietQR", "Payments", new { area = "Customers", orderId = order.Id });

            return RedirectToAction("Details", "Orders", new { area = "Customers", orderId = order.Id });
        }
    }
}
