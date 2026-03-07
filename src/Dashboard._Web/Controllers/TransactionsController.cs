using Dashboard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Dashboard.Application.Dtos;

namespace Dashboard._Web.Controllers;

public class TransactionsController : Controller
{
    private readonly ITransactionService _service;

    public TransactionsController(ITransactionService service)
    {
        _service = service;
    }

    [HttpGet("/transactions")]
    [HttpGet("/transacties")]
    public IActionResult Index() => View();

    [HttpGet("/transactions/content")]
    public async Task<IActionResult> TransactionsContent()
    {
        var transactions = await _service.GetTransactionsAsync();

        return PartialView("_TransactionsContent", transactions);
    }

    [HttpPost]
    public async Task<IActionResult> AddTransaction([FromBody] TransactionDto transaction)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid transaction data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        await _service.AddTransactionAsync(transaction);
        return Ok(new { success = true, message = "Transaction added" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTransaction([FromBody] string rowKey)
    {
        if (string.IsNullOrWhiteSpace(rowKey))
            return BadRequest(new { success = false, message = "Invalid RowKey" });

        await _service.DeleteTransactionAsync(rowKey);
        return Ok(new { success = true, message = "Transaction deleted" });
    }
}