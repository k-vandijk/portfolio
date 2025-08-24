using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class InvestmentController : Controller
{
    [HttpGet("/investment")]
    public IActionResult Investment()
    {
        return View();
    }
}