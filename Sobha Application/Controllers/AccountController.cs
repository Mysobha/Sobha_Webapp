using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Sobha_Application.Controllers
{
    public class AccountController : Controller
    {
      public IActionResult Index()
      {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "HomePage");
            }
            else
            {
                return View();
            }

        }
   

    }
}
