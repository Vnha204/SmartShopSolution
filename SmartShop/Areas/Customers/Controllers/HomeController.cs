using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models.ViewModels;

namespace SmartShop.Areas.Customers.Controllers
{
    [Area("Customers")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IUnitOfWork unitOfWork, ILogger<HomeController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // sp nổi bật
            var featured = (await _unitOfWork.Products.GetFeaturedAsync(8)).ToList();

            if (!featured.Any())
            {
                featured = await _unitOfWork.Products
                    .Query(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync();
            }

            ViewBag.FeaturedProducts = featured.Select(p => new ProductItemVM
            {
                Id           = p.Id,
                Name         = p.Name,
                Slug         = p.Slug,
                ThumbnailUrl = p.ThumbnailUrl,
                BasePrice    = p.BasePrice,
                SalePrice    = p.SalePrice,
                Stock        = p.Stock,
                IsFeatured   = p.IsFeatured
            }).ToList();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
