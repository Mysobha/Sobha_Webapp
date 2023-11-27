using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;

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


        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            return View("SignedOut");
        }
       
        public IActionResult Login()
        {

            return RedirectToAction("Index", "Account");
        }
    }
}
