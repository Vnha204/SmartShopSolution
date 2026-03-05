using Microsoft.AspNetCore.Mvc;

namespace SmartShop.Areas.Customers.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
