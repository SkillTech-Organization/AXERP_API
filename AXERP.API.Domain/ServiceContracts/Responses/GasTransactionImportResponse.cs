using AXERP.API.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;

namespace AXERP.API.Domain.ServiceContracts.Responses
{
    public class GasTransactionImportResponse
    {
        [SqlOutput("dbo.GasTransactions", connectionStringSetting: "SqlConnectionString")]
        public List<GasTransaction> Transactions { get; set; }

        public IActionResult HttpResponse { get; set; }
    }
}
