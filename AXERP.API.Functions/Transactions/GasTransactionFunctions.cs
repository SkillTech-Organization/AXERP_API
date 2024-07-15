using AutoMapper;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.GoogleSheetModels;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.GoogleHelper.Managers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Runtime.InteropServices;

namespace AXERP.API.Functions.Transactions
{
    public class GasTransactionFunctions
    {
        private readonly ILogger<GasTransactionFunctions> _logger;

        private readonly IMapper _mapper;

        public GasTransactionFunctions(ILogger<GasTransactionFunctions> logger, IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
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
            var importResult = await sheetService.ReadGoogleSheet<GasTransactionSheetModel>(sheet_id, $"{tab_name}{(range?.Length > 0 ? "!" : "")}{range}", sheetCulture);

            var postImportMiscInvalidCount = importResult.Data.Count(x => string.IsNullOrWhiteSpace(x.DeliveryID?.Trim()));
            importResult.InvalidRows += postImportMiscInvalidCount;

            var result = _mapper.Map<List<GasTransaction>>(importResult.Data.Where(x => !string.IsNullOrWhiteSpace(x.DeliveryID?.Trim())));

            var stats = $"GasTransactions imported. Row count: {importResult.RowCount}. Failed to import: {importResult.InvalidRows}";
            _logger.LogInformation(stats);

            return new GasTransactionImportResponse
            {
                Transactions = result,
                HttpResponse = req.CreateResponse(HttpStatusCode.OK),
                Message = stats
            };
        }
    }
}
