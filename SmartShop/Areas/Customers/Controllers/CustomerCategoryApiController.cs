using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Models;

namespace SmartShop.Areas.Customers.Controllers
{
    /// <summary>
    /// Lightweight JSON API used by the Customer Shop view
    /// for the hierarchical category drill-down sidebar.
    ///
    /// Routes (all under /Customers/api/categories/...):
    ///   GET /Customers/api/categories/children?parentId=   → child categories
    ///   GET /Customers/api/categories/breadcrumb?categoryId=  → root→leaf path
    /// </summary>
    [Area("Customers")]
    [Route("Customers/api/categories")]
    public class CustomerCategoryApiController : Controller
    {
        private readonly AppDbContext _db;
        public CustomerCategoryApiController(AppDbContext db) => _db = db;

        // =====================================================================
        // GET /Customers/api/categories/children?parentId=
        // Returns direct active children of parentId (or root when omitted).
        // =====================================================================
        [HttpGet("children")]
        public async Task<IActionResult> GetChildren([FromQuery] int? parentId)
        {
            var allCats = await _db.Categories
                .Where(c => c.IsActive)
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

        // =====================================================================
        // GET /Customers/api/categories/breadcrumb?categoryId=
        // Returns ordered array root → leaf for the given category.
        // =====================================================================
        [HttpGet("breadcrumb")]
        public async Task<IActionResult> GetBreadcrumb([FromQuery] int categoryId)
        {
            var allCats = await _db.Categories
                .Where(c => c.IsActive)
                .ToListAsync();

            var breadcrumb = BuildBreadcrumb(allCats, categoryId);
            return Json(breadcrumb.Select(c => new { id = c.Id, name = c.Name, slug = c.Slug }));
        }

        // ── Private helpers ───────────────────────────────────────────────────

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
