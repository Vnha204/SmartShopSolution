using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Models;
using SmartShop.Utility;

namespace SmartShop.Areas.Admin.Controllers
{
    /// <summary>
    /// Lightweight JSON API used by Admin product Create/Edit views
    /// for the hierarchical category drill-down and dynamic attribute loading.
    ///
    /// Routes (all under /Admin/api/categories/...):
    ///   GET /Admin/api/categories/children?parentId=   → child categories
    ///   GET /Admin/api/categories/breadcrumb?categoryId=  → root→leaf path
    ///   GET /Admin/api/categories/{id}/attributes?productId=  → applicable attrs + values
    /// </summary>
    [Area("Admin")]
    [Route("Admin/api/categories")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryApiController : Controller
    {
        private readonly AppDbContext _context;
        public CategoryApiController(AppDbContext context) => _context = context;

        // =====================================================================
        // GET /Admin/api/categories/children?parentId=
        // Returns direct children of parentId (or root categories when omitted).
        // =====================================================================
        [HttpGet("children")]
        public async Task<IActionResult> GetChildren([FromQuery] int? parentId)
        {
            // Load all active categories once — avoids N+1 for hasChildren check
            var allCats = await _context.Categories
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
        // GET /Admin/api/categories/breadcrumb?categoryId=
        // Returns ordered array root → leaf for the given category.
        // =====================================================================
        [HttpGet("breadcrumb")]
        public async Task<IActionResult> GetBreadcrumb([FromQuery] int categoryId)
        {
            var allCats = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();

            var breadcrumb = BuildBreadcrumb(allCats, categoryId);
            return Json(breadcrumb.Select(c => new { id = c.Id, name = c.Name, slug = c.Slug }));
        }

        // =====================================================================
        // GET /Admin/api/categories/{categoryId}/attributes?productId=
        // Returns applicable attributes (category + ancestors) with their values.
        // When productId is supplied, each value includes selected:true/false.
        // =====================================================================
        [HttpGet("{categoryId}/attributes")]
        public async Task<IActionResult> GetAttributes(int categoryId,
            [FromQuery] int? productId)
        {
            var allCats = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();

            // Build ancestor chain (leaf first, then up to root)
            var ancestorIds = GetAncestorIds(allCats, categoryId);
            if (!ancestorIds.Any())
                return Json(Array.Empty<object>());

            // Load CategoryAttributes for entire ancestor chain
            var catAttrs = await _context.CategoryAttributes
                .Where(ca => ancestorIds.Contains(ca.CategoryId))
                .Include(ca => ca.Attribute)
                    .ThenInclude(a => a.Values)
                .ToListAsync();

            // De-duplicate: keep the one closest to the leaf (leaf overrides ancestor)
            var uniqueAttrs = catAttrs
                .GroupBy(ca => ca.AttributeId)
                .Select(g =>
                {
                    // prefer mapping from leaf category; fall back to closest ancestor
                    var leafOrder = ancestorIds.IndexOf(categoryId); // 0 = leaf
                    return g.OrderBy(ca => ancestorIds.IndexOf(ca.CategoryId)).First();
                })
                .OrderBy(ca => ca.SortOrder)
                .ThenBy(ca => ca.Attribute.DisplayName)
                .ToList();

            // Selected values (only needed when editing an existing product)
            var selectedValueIds = new HashSet<int>();
            if (productId.HasValue)
            {
                selectedValueIds = (await _context.ProductAttributeValues
                    .Where(pav => pav.ProductId == productId.Value)
                    .Select(pav => pav.AttributeValueId)
                    .ToListAsync())
                    .ToHashSet();
            }

            var result = uniqueAttrs.Select(ca => new
            {
                attributeId = ca.AttributeId,
                displayName = ca.Attribute.DisplayName,
                values = ca.Attribute.Values
                    .OrderBy(v => v.SortOrder)
                    .ThenBy(v => v.DisplayName)
                    .Select(v => new
                    {
                        id          = v.Id,
                        value       = v.Value,
                        displayName = v.DisplayName,
                        selected    = selectedValueIds.Contains(v.Id)
                    })
                    .ToList()
            }).ToList();

            return Json(result);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Returns [leaf, parent, grandparent, …, root] (leaf at index 0).
        /// </summary>
        private static List<int> GetAncestorIds(List<Category> allCats, int categoryId)
        {
            var ids = new List<int>();
            var current = allCats.FirstOrDefault(c => c.Id == categoryId);
            while (current != null)
            {
                ids.Add(current.Id);
                current = current.ParentId.HasValue
                    ? allCats.FirstOrDefault(c => c.Id == current.ParentId.Value)
                    : null;
            }
            return ids;
        }

        /// <summary>
        /// Returns [root, …, leaf] breadcrumb.
        /// </summary>
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
