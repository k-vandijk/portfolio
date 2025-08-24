using AutoMapper;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers;

public class TransactionsController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;

    public TransactionsController(IConfiguration configuration, IMapper mapper)
    {
        _configuration = configuration;
        _mapper = mapper;
    }

    public IActionResult Transactions()
    {
        var transactions = GetTransactions();

        return View(transactions);
    }

    private List<Transaction> GetTransactions()
    {
        string connectionString = _configuration["transactions-table-connection-string"] ?? throw new InvalidOperationException("transactions-table-connection-string is not configured.");

        var serviceClient = new TableServiceClient(connectionString);
        var tableClient = serviceClient.GetTableClient("transactions");

        Pageable<TransactionEntity> transactions = tableClient.Query<TransactionEntity>(
            filter: "PartitionKey eq 'transactions'"
        );

        return _mapper.Map<List<Transaction>>(transactions.ToList()).OrderBy(t => t.Date).ToList();
    }
}