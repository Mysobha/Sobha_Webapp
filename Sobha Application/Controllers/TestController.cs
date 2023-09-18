using Microsoft.AspNetCore.Mvc;

namespace Sobha_Application.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
