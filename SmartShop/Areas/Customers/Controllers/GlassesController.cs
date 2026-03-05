using Microsoft.AspNetCore.Mvc;

namespace SmartShop.Areas.Customers.Controllers
{
    [Area("Customers")]
    public class GlassesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
