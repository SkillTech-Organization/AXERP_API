using AXERP.API.Domain.Entities;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;

namespace AXERP.API.Domain.ServiceContracts.Responses
{
    public class GasTransactionImportResponse
    {
        [SqlOutput("dbo.GasTransactions", connectionStringSetting: "SqlConnectionString")]
        public List<GasTransaction> Transactions { get; set; }

        public HttpResponseData HttpResponse { get; set; }
    }
}
