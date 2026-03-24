using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Utility;

namespace SmartShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class DashboardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager,
            ILogger<DashboardController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalProducts   = await _unitOfWork.Products.CountAsync();
            ViewBag.TotalCategories = await _unitOfWork.Categories.CountAsync();
            ViewBag.TotalOrders     = await _unitOfWork.Orders.CountAsync();
            ViewBag.PendingOrders   = await _unitOfWork.Orders.CountAsync(o => o.Status == "pending");
            ViewBag.TotalUsers      = _userManager.Users.Count();

            return View();
        }
    }
}
