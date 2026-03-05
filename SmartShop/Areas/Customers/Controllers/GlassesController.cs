using Microsoft.AspNetCore.Mvc;

namespace SmartShop.Areas.Customers.Controllers
{
    public class GlassesController : Controller
    {
        [Area("Customers")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
