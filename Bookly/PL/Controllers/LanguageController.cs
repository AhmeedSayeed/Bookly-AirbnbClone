using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    public class LanguageController : Controller
    {
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(
                    new RequestCulture(culture)
                )
            );

            return LocalRedirect(returnUrl);
        }
    }
}