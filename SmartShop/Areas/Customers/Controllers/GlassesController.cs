using Microsoft.AspNetCore.Mvc;

namespace SmartShop.Areas.Customers.Controllers
{
    public class GlassesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
