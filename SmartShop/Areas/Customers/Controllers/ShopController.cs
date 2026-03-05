using Microsoft.AspNetCore.Mvc;

namespace SmartShop.Areas.Customers.Controllers
{
    public class ShopController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
