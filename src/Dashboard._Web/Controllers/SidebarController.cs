using Dashboard.Domain.Utils;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Web.Controllers;

public class SidebarController : Controller
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

    [HttpPost]
    public IActionResult ToggleSidebarCollapsedState()
    {
        var isCollapsed = Request.Cookies[StaticDetails.SidebarStateCookie] == "true";

        Response.Cookies.Append(
            StaticDetails.SidebarStateCookie,
            (!isCollapsed).ToString().ToLower(),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

        return Ok();
    }
}