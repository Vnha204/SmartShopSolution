using Microsoft.AspNetCore.Mvc;

namespace SmartShop.Areas.Customers.Controllers
{
    public class AboutController : Controller
    {
        [Area("Customers")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
