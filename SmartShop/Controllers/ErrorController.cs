using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Models;

namespace SmartShop.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (feature?.Error != null)
                _logger.LogError(feature.Error, "Unhandled exception at {Path}", feature.Path);

            return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }

        [Route("Error/{statusCode:int}")]
        public new IActionResult StatusCode(int statusCode)
        {
            return View("StatusCode", statusCode);
        }
    }
}
