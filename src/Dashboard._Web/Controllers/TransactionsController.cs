using Dashboard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Dashboard.Application.Dtos;

namespace Dashboard._Web.Controllers;

public class TransactionsController : Controller
{
    private readonly IAzureTableService _service;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(IAzureTableService service, ILogger<TransactionsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("/transactions")]
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
        if (transaction == null)
        {
            return StatusCode(500, new { success = false, message = "No transaction data provided." });
        }

        if (!ModelState.IsValid)
        {
            return StatusCode(500, new { success = false, message = "Invalid transaction data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            await _service.AddTransactionAsync(transaction);
            return Ok(new { success = true, message = "Transaction added" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error saving transaction.", detail = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTransaction([FromBody] string rowKey)
    {
        if (string.IsNullOrWhiteSpace(rowKey))
        {
            return BadRequest(new { success = false, message = "Invalid RowKey" });
        }

        try
        {
            await _service.DeleteTransactionAsync(rowKey); // make sure you have this in your service
            return Ok(new { success = true, message = "Transaction deleted" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error deleting transaction", detail = ex.Message });
        }
    }
}