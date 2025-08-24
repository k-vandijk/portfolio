using Microsoft.AspNetCore.Mvc;
using Web.Services;

namespace Web.Controllers;

public class TransactionsController : Controller
{
    private readonly IAzureTableService _service;

    public TransactionsController(IAzureTableService service)
    {
        _service = service;
    }

    [HttpGet("/transactions")]
    public IActionResult Transactions()
    {
        var transactions = _service.GetTransactions();

        return View(transactions);
    }
}