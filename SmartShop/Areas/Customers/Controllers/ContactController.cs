using Microsoft.AspNetCore.Mvc;

namespace SmartShop.Areas.Customers.Controllers
{
    public class ContactController : Controller
    {
        [Area("Customers")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
