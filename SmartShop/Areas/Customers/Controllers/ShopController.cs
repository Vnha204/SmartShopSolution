using Microsoft.AspNetCore.Mvc;

namespace SmartShop.Areas.Customers.Controllers
{
    public class ShopController : Controller
    {
        [Area("Customers")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
