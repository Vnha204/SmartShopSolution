using Microsoft.AspNetCore.Mvc;

namespace SmartShop.Areas.Customers.Controllers
{
    [Area("Customers")]
    public class AboutController : Controller
    {
        private readonly ILogger<AboutController> _logger;

        public AboutController(ILogger<AboutController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
