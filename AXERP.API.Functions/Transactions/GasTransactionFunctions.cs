using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.Functions.SheetProcessors;
using AXERP.API.GoogleHelper.Managers;
using AXERP.API.GoogleHelper.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using YamlDotNet.Core;
using AXERP.API.Persistence.Repositories;
using AXERP.API.Persistence.ServiceContracts.Requests;
using AXERP.API.Persistence.Utils;

namespace AXERP.API.Functions.Transactions
{
    public class GasTransactionFunctions
    {
        private readonly ILogger<GasTransactionFunctions> _logger;
        private readonly GasTransactionSheetProcessor _gasTransactionSheetProcessor;
        private readonly GenericRepository _genericRepository;

        public GasTransactionFunctions(
            ILogger<GasTransactionFunctions> logger,
            GasTransactionSheetProcessor gasTransactionSheetProcessor,
            GenericRepository genericRepository)
        {
            _logger = logger;
            _gasTransactionSheetProcessor = gasTransactionSheetProcessor;
            _genericRepository = genericRepository;
        }

        #region SQL Scripts

        public readonly string Sql_Insert_GasTransaction = @"
                INSERT INTO GasTransactions
                       (DeliveryID
                       ,DateLoadedEnd
                       ,DateDelivered
                       ,SalesContractID
                       ,SalesStatus
                       ,Terminal
                       ,QtyLoaded
                       ,ToDeliveryID
                       ,Status
                       ,SpecificDeliveryPoint
                       ,DeliveryPoint
                       ,Transporter
                       ,DeliveryUP
                       ,TransportCharges
                       ,UnitSlotCharge
                       ,ServiceCharges
                       ,UnitStorageCharge
                       ,StorageCharge
                       ,OtherCharges
                       ,Sales
                       ,CMR
                       ,BioMWh
                       ,BillOfLading
                       ,BioAddendum
                       ,Comment
                       ,CustomerNote
                       ,Customer
                       ,Reference
                       ,Reference2
                       ,Reference3
                       ,TruckLoadingCompanyComment
                       ,TruckCompany)
                 VALUES
                       (@DeliveryID
                       ,@DateLoadedEnd
                       ,@DateDelivered
                       ,@SalesContractID
                       ,@SalesStatus
                       ,@Terminal
                       ,@QtyLoaded
                       ,@ToDeliveryID
                       ,@Status
                       ,@SpecificDeliveryPoint
                       ,@DeliveryPoint
                       ,@Transporter
                       ,@DeliveryUP
                       ,@TransportCharges
                       ,@UnitSlotCharge
                       ,@ServiceCharges
                       ,@UnitStorageCharge
                       ,@StorageCharge
                       ,@OtherCharges
                       ,@Sales
                       ,@CMR
                       ,@BioMWh
                       ,@BillOfLading
                       ,@BioAddendum
                       ,@Comment
                       ,@CustomerNote
                       ,@Customer
                       ,@Reference
                       ,@Reference2
                       ,@Reference3
                       ,@TruckLoadingCompanyComment
                       ,@TruckCompany)
            ";

        public readonly string Sql_Select_GasTransaction_IDs = @"
                select DeliveryID from GasTransactions
            ";

        public readonly string Sql_Query_Paged_GasTransactions = 
            @"
            select X.* from 
                (select _table.*, ROW_NUMBER() OVER (/**orderby**/) AS RowNumber from GasTransactions _table /**where**/)
            as X where RowNumber between @start and @finish
            ";

        public readonly string Sql_Query_Paged_GasTransactions_Dynamic_Columns =
            @"
            select /**select**/ from 
                (select _table.*, ROW_NUMBER() OVER (/**orderby**/) AS RowNumber from GasTransactions _table /**where**/)
            as X where RowNumber between @start and @finish
            ";

        public readonly string Sql_Query_Count_GasTransactions = "SELECT COUNT(*) FROM GasTransactions";

        #endregion

        private ImportGasTransactionResponse InsertTransactions(GenericSheetImportResult<GasTransaction> importResult)
        {
            var res = new ImportGasTransactionResponse
            {
                InvalidRows = importResult.InvalidRows,
                NewRowsInsertedIntoDatabase = 0,
                TotalDataRowsInSheet = importResult.TotalRowsInSheet,
                ImportErrors = importResult.Errors
            };

            if (importResult == null || importResult.Data == null)
            {
                throw new Exception("Failed google sheet import.");
            }

            using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
            {
                var ids = conn.Query<string>(Sql_Select_GasTransaction_IDs);
                var newRows = importResult.Data.Where(x => !ids.Contains(x.DeliveryID));
                res.NewRowsInsertedIntoDatabase += conn.Execute(Sql_Insert_GasTransaction, newRows);
            }

            return res;
        }

        [Function(nameof(ImportGasTransactions))]
        [OpenApiOperation(operationId: nameof(ImportGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> ImportGasTransactions([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Importing GasTransactions...");

            try
            {
                // Get parameters
                var credentialsJson = Environment.GetEnvironmentVariable("GoogleCredentials");

                if (string.IsNullOrWhiteSpace(credentialsJson))
                {
                    var msg = "GoogleCredentials environment variable is missing.";
                    _logger.LogError(msg);
                    return new BadRequestObjectResult(msg);
                }

                var sheet_id = Environment.GetEnvironmentVariable("BulkDeliveriesSheetDataSheetId");
                var tab_name = Environment.GetEnvironmentVariable("BulkDeliveriesSheetDataGasTransactionsTab");
                var range = Environment.GetEnvironmentVariable("BulkDeliveriesSheetDataGasTransactionRange");
                var sheetCulture = Environment.GetEnvironmentVariable("SheetCulture") ?? "fr-FR";

                // Sheet import
                var sheetService = new GoogleSheetManager(credentials: credentialsJson, format: CredentialsFormats.Text);

                var rows = await sheetService.ReadGoogleSheetRaw(sheet_id, $"{tab_name}{(range?.Length > 0 ? "!" : "")}{range}");
                var importResult = _gasTransactionSheetProcessor.ProcessRows(rows, sheetCulture);

                // Process
                var result = InsertTransactions(importResult);

                _logger.LogInformation("GasTransactions imported. Stats: {stats}", Newtonsoft.Json.JsonConvert.SerializeObject(result));

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while importing GasTransactions");
                var res = new ObjectResult(new ImportGasTransactionResponse
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError,
                    RequestError = ex.Message
                })
                {
                    StatusCode = 500
                };
                return res;
            }
        }

        /*
        [Function(nameof(GetAllGasTransactions))]
        [OpenApiOperation(operationId: nameof(GetAllGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public IActionResult GetAllGasTransactions(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
                [SqlInput("%Sql_Query_All_GasTransactions%", "SqlConnectionString")] IEnumerable<GasTransaction> items)
        {
            _logger.LogInformation("Querying GasTransactions. Row count: {count}", items.Count());

            return new OkObjectResult(items);
        }

        [Function(nameof(QueryPagedGasTransactions))]
        [OpenApiOperation(operationId: nameof(QueryPagedGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiParameter(name: "PageSize", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Returned row count")]
        //[OpenApiParameter(name: "OrderBy", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Order by")]
        [OpenApiParameter(name: "Page", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Page")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public IActionResult QueryPagedGasTransactions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            //[SqlInput("%Sql_Query_Count_GasTransactions%", "SqlConnectionString")] Dictionary<string, int> totalCount, // cannot be int or string!
            [SqlInput(
                commandText: "%Sql_Query_Paged_GasTransactions%",
                parameters: "@PageSize={Query.PageSize},@Page={Query.Page}", // @OrderBy={Query.OrderBy},
                connectionStringSetting: "SqlConnectionString")] IEnumerable<GasTransaction> items)
        {
            _logger.LogInformation("Querying GasTransactions. Row count: {count}, Page: {page}, PageSize: {pageSize}", items.Count(), req.Query["Page"], req.Query["PageSize"]);

            var totalCount = CountTableRows("GasTransactions");

            _logger.LogInformation("Querying GasTransactions. TotalCount: {totalCount}", totalCount);

            return new OkObjectResult(new GenericQueryResponse<GasTransaction>
            {
                Data = items,
                PageIndex = int.Parse(req.Query["Page"]),
                PageSize = int.Parse(req.Query["PageSize"]),
                TotalCount = totalCount
            });
        }
        */

        [Function(nameof(CountGasTransactions))]
        [OpenApiOperation(operationId: nameof(CountGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public int CountGasTransactions(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            return _genericRepository.Count(Environment.GetEnvironmentVariable("Sql_Query_Count_GasTransactions") ?? Sql_Query_Count_GasTransactions);
        }

        [Function(nameof(QueryGasTransactions))]
        [OpenApiOperation(operationId: nameof(QueryGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiParameter(name: "Search", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Search in all columns, type Column = Search for specific search, eg. DeliveryID = 5")]
        [OpenApiParameter(name: "SearchOnlyInSelectedColumns", In = ParameterLocation.Query, Required = false, Type = typeof(bool), Description = "Search only in columns provided in the Columns parameter - ignored if Search is written for specific column")]
        [OpenApiParameter(name: "Columns", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "List of columns, separated by ',' character, all columns will be used by default")]
        [OpenApiParameter(name: "OrderBy", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Order by column, default is DeliveryID")]
        [OpenApiParameter(name: "OrderByDesc", In = ParameterLocation.Query, Required = false, Type = typeof(bool), Description = "Descending order, false by default")]
        [OpenApiParameter(name: "PageSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Returned row count, default is 5")]
        [OpenApiParameter(name: "Page", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page index, starting from 1 (0 will be interpreted as 1), default is 1")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public IActionResult QueryGasTransactions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var queryTemplate = Environment.GetEnvironmentVariable("Sql_Query_Paged_GasTransactions_Dynamic_Columns") ?? Sql_Query_Paged_GasTransactions_Dynamic_Columns;
            var countTemplate = Environment.GetEnvironmentVariable("Sql_Query_Count_GasTransactions") ?? Sql_Query_Count_GasTransactions;

            var cols = req.Query["Columns"]?.ToString()?.Split(",", StringSplitOptions.TrimEntries)?.ToList() ?? new List<string>();

            var page = int.Parse(req.Query["Page"] ?? "1");
            if (page <= 0)
            {
                page = 1;
            }

            var pageSize = int.Parse(req.Query["PageSize"] ?? "5");
            if (pageSize <= 0)
            {
                pageSize = 1;
            }

            var result = _genericRepository.PagedQuery<GasTransaction>(new PagedQueryRequest
            {
                QueryTemplate = queryTemplate,
                CountTemplate = countTemplate,
                Columns = cols,
                OrderBy = req.Query["OrderBy"] ?? "DeliveryID",
                OrderDesc = bool.Parse(req.Query["OrderByDesc"] ?? "false"),
                Page = page,
                PageSize = pageSize,
                Search = req.Query["Search"],
                SearchOnlyInSelectedColumns = bool.Parse(req.Query["SearchOnlyInSelectedColumns"] ?? "false")
            });

            return new OkObjectResult(result);
        }

    }
}
