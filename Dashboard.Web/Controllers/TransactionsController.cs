using Dashboard.Application.Interfaces;
using Dashboard.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Dashboard.Web.Controllers;

public class TransactionsController : Controller
{
    private readonly IAzureTableService _service;
    private readonly IConfiguration _config;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(IAzureTableService service, IConfiguration config, ILogger<TransactionsController> logger)
    {
        _service = service;
        _config = config;
        _logger = logger;
    }

    [HttpGet("/transactions")]
    public IActionResult Index() => View();

    [HttpGet("/transactions/content")]
    public async Task<IActionResult> TransactionsContent()
    {
        var sw = Stopwatch.StartNew();

        var connectionString = _config["Secrets:TransactionsTableConnectionString"]
            ?? throw new ArgumentNullException("Secrets:TransactionsTableConnectionString", "Please set the connection string in the configuration.");

        var transactions = await _service.GetTransactionsAsync(connectionString);

        sw.Stop();
        _logger.LogInformation("Transactions view rendered in {Elapsed} ms", sw.ElapsedMilliseconds);
        return PartialView("_TransactionsContent", transactions);
    }

    [HttpPost]
    public async Task<IActionResult> AddTransaction([FromBody] Transaction transaction)
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
            var connectionString = _config["Secrets:TransactionsTableConnectionString"]
                ?? throw new ArgumentNullException("Secrets:TransactionsTableConnectionString", "Please set the connection string in the configuration.");

            await _service.AddTransactionAsync(connectionString, transaction);
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
            var connectionString = _config["Secrets:TransactionsTableConnectionString"]
                ?? throw new ArgumentNullException("Secrets:TransactionsTableConnectionString", "Please set the connection string in the configuration.");

            await _service.DeleteTransactionAsync(connectionString, rowKey); // make sure you have this in your service
            return Ok(new { success = true, message = "Transaction deleted" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error deleting transaction", detail = ex.Message });
        }
    }
}