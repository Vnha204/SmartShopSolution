using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Models.ViewModels;
using SmartShop.Utility;

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

        // GET thanh toán
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

            var paymentHint = Request.Query["payment"].ToString();
            var preselected  = paymentHint is "payos" or "cod" ? paymentHint : "cod";

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
                                 ?? addresses.FirstOrDefault()?.Id,
                PaymentMethod     = preselected
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel vm)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;

                var cart = await _unitOfWork.Carts.GetWithItemsAsync(userId);
                if (cart == null || !cart.Items.Any())
                {
                    TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction("Index", "Cart");
                }

                bool isModeA = vm.SelectedAddressId.HasValue && vm.SelectedAddressId > 0;
                bool isModeB = !isModeA;

                Address? address = null;

                if (isModeA)
                {
                    address = await _unitOfWork.Addresses.GetByIdAsync(vm.SelectedAddressId!.Value);
                    if (address == null)
                        ModelState.AddModelError("SelectedAddressId", "Không tìm thấy địa chỉ giao hàng");
                    else if (address.UserId != userId)
                        ModelState.AddModelError("SelectedAddressId", "Địa chỉ không hợp lệ");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(vm.NewFullName) ||
                        string.IsNullOrWhiteSpace(vm.NewPhone) ||
                        string.IsNullOrWhiteSpace(vm.NewProvince) ||
                        string.IsNullOrWhiteSpace(vm.NewDistrict) ||
                        string.IsNullOrWhiteSpace(vm.NewWard) ||
                        string.IsNullOrWhiteSpace(vm.NewStreetLine))
                        ModelState.AddModelError("NewFullName", "Vui lòng nhập đầy đủ thông tin địa chỉ giao hàng");
                }

                if (vm.PaymentMethod != "cod" && vm.PaymentMethod != "payos")
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

                if (isModeB)
                {
                    var newAddress = new Address
                    {
                        UserId      = userId,
                        FullName    = vm.NewFullName!.Trim(),
                        PhoneNumber = vm.NewPhone!.Trim(),
                        Province    = vm.NewProvince!.Trim(),
                        District    = vm.NewDistrict!.Trim(),
                        Ward        = vm.NewWard!.Trim(),
                        StreetLine  = vm.NewStreetLine!.Trim(),
                        IsDefault   = false
                    };
                    await _unitOfWork.Addresses.AddAsync(newAddress);
                    await _unitOfWork.SaveAsync();
                    address = newAddress;
                }

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

                string orderCode;
                var rng = new Random();
                do
                {
                    var suffix = new string(Enumerable.Range(0, 5)
                        .Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[rng.Next(36)])
                        .ToArray());
                    orderCode = "OD" + TimeHelper.VietnamNow().ToString("yyyyMMdd") + suffix;
                }
                while (await _unitOfWork.Orders.AnyAsync(o => o.OrderCode == orderCode));

                var order = new Order
                {
                    UserId        = userId,
                    AddressId     = address!.Id,
                    OrderCode     = orderCode,
                    Status        = OrderStatus.Pending,
                    PaymentMethod = vm.PaymentMethod,
                    PaymentStatus = vm.PaymentMethod == "payos" ? "pending" : "unpaid",
                    TotalPrice    = cart.Items.Sum(ci => ci.UnitPriceSnapshot * ci.Quantity),
                    Note          = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim(),
                    CreatedAt     = TimeHelper.VietnamNow(),
                    UpdatedAt     = TimeHelper.VietnamNow()
                };
                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.SaveAsync();

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
                        product.UpdatedAt = TimeHelper.VietnamNow();
                        _unitOfWork.Products.Update(product);
                    }
                }

                _unitOfWork.CartItems.RemoveRange(cart.Items);
                cart.UpdatedAt = TimeHelper.VietnamNow();
                _unitOfWork.Carts.Update(cart);

                await _unitOfWork.SaveAsync();

                TempData["Success"] = $"Đặt hàng thành công! Mã đơn: {order.OrderCode}";

                if (vm.PaymentMethod == "payos")
                    return RedirectToAction("PayOS", "Payments", new { area = "Customers", orderId = order.Id });

                return RedirectToAction("OrderDetail", "Account", new { area = "", orderId = order.Id });
            }
            catch
            {
                TempData["Error"] = "Có lỗi xảy ra, vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }
    }
}
