using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;

namespace SmartShop.Areas.Customers.Controllers
{
    [Area("Customers")]
    [Route("Customers/api/categories")]
    public class CustomerCategoryApiController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CustomerCategoryApiController> _logger;

        public CustomerCategoryApiController(IUnitOfWork unitOfWork,
            ILogger<CustomerCategoryApiController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        //Customers/api/categories/children?parentId=
        [HttpGet("children")]
        public async Task<IActionResult> GetChildren([FromQuery] int? parentId)
        {
            var allCats = await _unitOfWork.Categories.Query(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var children = allCats
                .Where(c => c.ParentId == parentId)
                .Select(c => new
                {
                    id          = c.Id,
                    name        = c.Name,
                    slug        = c.Slug,
                    hasChildren = allCats.Any(ch => ch.ParentId == c.Id)
                })
                .ToList();

            return Json(children);
        }

        //Customers/api/categories/breadcrumb?categoryId=
        [HttpGet("breadcrumb")]
        public async Task<IActionResult> GetBreadcrumb([FromQuery] int categoryId)
        {
            var allCats = await _unitOfWork.Categories.Query(c => c.IsActive).ToListAsync();
            var breadcrumb = BuildBreadcrumb(allCats, categoryId);
            return Json(breadcrumb.Select(c => new { id = c.Id, name = c.Name, slug = c.Slug }));
        }

        private static List<Category> BuildBreadcrumb(List<Category> allCats, int categoryId)
        {
            var chain = new List<Category>();
            var current = allCats.FirstOrDefault(c => c.Id == categoryId);
            while (current != null)
            {
                chain.Insert(0, current);
                current = current.ParentId.HasValue
                    ? allCats.FirstOrDefault(c => c.Id == current.ParentId.Value)
                    : null;
            }
            return chain;
        }
    }
}
