using Microsoft.AspNetCore.Mvc;

namespace SmartShop.Areas.Customers.Controllers
{
    [Area("Customers")]
    public class ContactController : Controller
    {
        private readonly ILogger<ContactController> _logger;

        public ContactController(ILogger<ContactController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
