using Microsoft.AspNetCore.Mvc;

namespace CDT.Cosmos.Cms.Controllers
{
    public class PingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Test()
        {
            return View();
        }
    }
}