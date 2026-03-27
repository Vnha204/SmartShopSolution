using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Models.ViewModels;

namespace SmartShop.Areas.Customers.Controllers
{
    [Area("Customers")]
    public class GlassesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GlassesController> _logger;

        public GlassesController(IUnitOfWork unitOfWork, ILogger<GlassesController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET /Customers/Glasses  (full page)
        public async Task<IActionResult> Index(string filter = "all")
        {
            var vm = await BuildViewModelAsync(filter);
            return View(vm);
        }

        // GET /Customers/Glasses/Grid?filter=…  (AJAX partial)
        [HttpGet]
        public async Task<IActionResult> Grid(string filter = "all")
        {
            var vm = await BuildViewModelAsync(filter);
            return PartialView("_GlassesGrid", vm);
        }

        // ─── Private helpers ──────────────────────────────────────────────────

        private async Task<GlassesViewModel> BuildViewModelAsync(string filter)
        {
            filter = (filter ?? "all").ToLowerInvariant().Trim();

            var query = _unitOfWork.Products.Query(p => p.IsActive);

            if (filter != "all")
            {
                var category = await _unitOfWork.Categories.GetBySlugAsync(filter);
                if (category != null)
                {
                    var allCats = await _unitOfWork.Categories
                        .Query(c => c.IsActive)
                        .ToListAsync();

                    var catIds = GetDescendantIds(allCats, category.Id);
                    query = query.Where(p =>
                        p.ProductCategories.Any(pc => catIds.Contains(pc.CategoryId)));
                }
                else
                {
                    query = query.Where(p => false);
                }
            }

            var products = await query
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            return new GlassesViewModel
            {
                ActiveFilter = filter,
                Products = products.Select(p => new ProductItemVM
                {
                    Id           = p.Id,
                    Name         = p.Name,
                    Slug         = p.Slug,
                    ThumbnailUrl = p.ThumbnailUrl,
                    BasePrice    = p.BasePrice,
                    SalePrice    = p.SalePrice,
                    Stock        = p.Stock,
                    IsFeatured   = p.IsFeatured
                }).ToList()
            };
        }

        private static List<int> GetDescendantIds(List<Category> allCats, int categoryId)
        {
            var result = new List<int>();
            var queue  = new Queue<int>();
            queue.Enqueue(categoryId);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                result.Add(id);
                foreach (var child in allCats.Where(c => c.ParentId == id))
                    queue.Enqueue(child.Id);
            }
            return result;
        }
    }
}
