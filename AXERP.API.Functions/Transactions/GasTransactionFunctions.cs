using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.GoogleHelper.Managers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;

namespace AXERP.API.Functions.Transactions
{
    public class GasTransactionFunctions
    {
        private readonly ILogger<GasTransactionFunctions> _logger;

        public GasTransactionFunctions(ILogger<GasTransactionFunctions> logger)
        {
            _logger = logger;
        }

        [Function(nameof(ImportGasTransactions))]
        [OpenApiOperation(operationId: nameof(ImportGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<GasTransactionImportResponse> ImportGasTransactions([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Importing GasTransactions...");

            var sheet_id = Environment.GetEnvironmentVariable("BulkDeliveriesSheetData:SheetId");
            var tab_name = Environment.GetEnvironmentVariable("BulkDeliveriesSheetData:GasTransactionsTab");
            var range = Environment.GetEnvironmentVariable("BulkDeliveriesSheetData:GasTransactionRange");
            var sheetCulture = Environment.GetEnvironmentVariable("SheetCulture") ?? "fr-FR";

            var sheetService = new GoogleSheetManager();
            var sheetData = await sheetService.ReadGoogleSheet<GasTransaction>(sheet_id, $"{tab_name}!{range}", sheetCulture);

            var stats = $"GasTransactions imported. Row count: {sheetData.Count}";
            _logger.LogInformation(stats);

            return new GasTransactionImportResponse
            {
                Transactions = sheetData,
                HttpResponse = req.CreateResponse(HttpStatusCode.OK),
                Message = stats
            };
        }
    }
}
