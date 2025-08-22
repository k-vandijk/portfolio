using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Web.ViewModels;

namespace Web.Controllers;

public class HomeController : Controller
{
    [HttpGet("/dashboard")]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet("/investment")]
    public IActionResult Investment()
    {
        return View();
    }

    [HttpGet("/market-history")]
    public IActionResult MarketHistory()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}