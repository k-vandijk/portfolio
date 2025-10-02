using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Web.Controllers;

public class LocalizationController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetCulture(string culture, string? returnUrl = null)
    {
        var requestCulture = new RequestCulture(culture: "nl-NL", uiCulture: culture);

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(requestCulture),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

        return LocalRedirect(returnUrl ?? "/");
    }   
}