using Microsoft.AspNetCore.Mvc;

namespace SmartShop.Areas.Customers.Controllers
{
    [Area("Customers")]
    public class GlassesController : Controller
    {
        private readonly ILogger<GlassesController> _logger;

        public GlassesController(ILogger<GlassesController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
