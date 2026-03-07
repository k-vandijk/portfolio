using Microsoft.AspNetCore.Mvc;
using Kvandijk.Portfolio.Application.Dtos;
using Kvandijk.Portfolio.Application.Mappers;
using Kvandijk.Portfolio.Application.RepositoryInterfaces;

namespace Kvandijk.Portfolio._Web.Controllers;

public class TransactionsController : Controller
{
    private readonly ITransactionsRepository _transactionsRepository;

    public TransactionsController(ITransactionsRepository transactionsRepository)
    {
        _transactionsRepository = transactionsRepository;
    }

    [HttpGet("/transactions")]
    [HttpGet("/transacties")]
    public IActionResult Index() => View();

    [HttpGet("/transactions/content")]
    public async Task<IActionResult> TransactionsContent()
    {
        var transactionEntities = await _transactionsRepository.GetAllAsync();
        var transactionDtos = transactionEntities.Select(e => e.ToModel()).ToList();

        return PartialView("_TransactionsContent", transactionDtos);
    }

    [HttpPost]
    public async Task<IActionResult> AddTransaction([FromBody] TransactionDto transaction)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid transaction data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        await _transactionsRepository.AddAsync(transaction.ToEntity());
        return Ok(new { success = true, message = "Transaction added" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTransaction([FromBody] string rowKey)
    {
        if (string.IsNullOrWhiteSpace(rowKey))
            return BadRequest(new { success = false, message = "Invalid RowKey" });

        await _transactionsRepository.DeleteByRowKeyAsync(rowKey);
        return Ok(new { success = true, message = "Transaction deleted" });
    }
}