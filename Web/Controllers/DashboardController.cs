using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class DashboardController : Controller
{
    [HttpGet("/dashboard")]
    public IActionResult Dashboard()
    {
        return View();
    }
}