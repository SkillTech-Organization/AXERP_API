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

namespace AXERP.API.Functions.Transactions
{
    public class GasTransactionFunctions
    {
        private readonly ILogger<GasTransactionFunctions> _logger;
        private readonly GasTransactionSheetProcessor _processor;

        public GasTransactionFunctions(ILogger<GasTransactionFunctions> logger, GasTransactionSheetProcessor processor)
        {
            _logger = logger;
            _processor = processor;
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
                var importResult = _processor.ProcessRows(rows, sheetCulture);

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

        //TODO: külön segédosztályba / generic repositoryba
        private int CountTableRows(string tableName)
        {
            int count = 0;
            using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
            {
                var builder = new SqlBuilder();

                var countTemplate = builder.AddTemplate(
                    // Query
                    Environment.GetEnvironmentVariable("Sql_Count_Generic"), // "SELECT COUNT(*) FROM @tableName",

                    // Parameters
                    new DynamicParameters(new Dictionary<string, object>
                    {
                        { "@tableName", tableName }
                    })
                );
                //builder.AddParameters(new
                //{
                //    tableName = tableName
                //});
                count = conn.ExecuteScalar<int>(countTemplate.RawSql, countTemplate.Parameters);
            }
            return count;
        }

        public class PagedQueryRequest
        {
            public virtual string QueryTemplate { get; set; }
            
            public virtual string Table { get; set; }
            
            public virtual int Page { get; set; }
            
            public virtual int PageSize { get; set; }
            
            public virtual string OrderBy { get; set; }

            public virtual bool OrderDesc { get; set; }

            public virtual List<string> Columns { get; set; }

            public virtual int RowNumberStart => PageSize * Page;

            public virtual int RowNumberFinish => RowNumberStart + PageSize - 1;
        }

        public GenericQueryResponse<RowType> PagedQuery<RowType>(PagedQueryRequest request)
        {
            var result = new List<RowType>();
            var totalCount = 0;

            try
            {
                totalCount = CountTableRows(request.Table);

                using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    var builder = new SqlBuilder();

                    var cols = request.Columns.Select(x => "X." + x);
                    var _cols = string.Join(", ", cols);

                    var selectTemplate = builder.AddTemplate(
                        // Query
                        request.QueryTemplate,
                        //@"select X.* from (
                        //    select _table.*, ROW_NUMBER() OVER (/**orderby**/) AS RowNumber 
                        //    from @table _table 
                        //    /**where**/
                        //) as X 
                        //where RowNumber between @start and @finish",

                        // Parameters
                        new DynamicParameters(new Dictionary<string, object>
                        {
                            { "@table", request.Table },
                            { "@columns", _cols },
                            { "@start", request.RowNumberStart },
                            { "@finish", request.RowNumberFinish },
                        })
                        //new {
                        //    table = request.Table,
                        //    start = request.RowNumberStart,
                        //    finish = request.RowNumberFinish
                        //}
                    );
                    //builder.AddParameters(new
                    //{
                    //    table = request.Table,
                    //    start = request.RowNumberStart,
                    //    finish = request.RowNumberFinish
                    //});

                    builder.OrderBy(string.Format("_table.{0} {1}", request.OrderBy, request.OrderDesc ? "desc" : "asc"));

                    result = conn.Query<RowType>(selectTemplate.RawSql, selectTemplate.Parameters).ToList();
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error: ");
                throw;
            }

            return new GenericQueryResponse<RowType>
            {
                Data = result,
                PageIndex = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }


        [Function(nameof(QueryGasTransactions))]
        [OpenApiOperation(operationId: nameof(QueryGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiParameter(name: "Columns", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "List of columns, separated by ',' character")]
        [OpenApiParameter(name: "PageSize", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Returned row count")]
        [OpenApiParameter(name: "OrderBy", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Order by")]
        [OpenApiParameter(name: "OrderByDesc", In = ParameterLocation.Query, Required = true, Type = typeof(bool), Description = "Order by desc")]
        [OpenApiParameter(name: "Page", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Page")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public IActionResult QueryGasTransactions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var dapperTemplate = Environment.GetEnvironmentVariable("Sql_Query_Paged_GasTransactions_Dapper").ToString(); // Sql_Query_Paged_GasTransactions_Dapper_Dynamic_Columns

            var cols = req.Query["Columns"]?.ToString() ?? "";

            var result = PagedQuery<GasTransaction>(new PagedQueryRequest
            {
                Table = "GasTransactions",
                QueryTemplate = dapperTemplate,
                Columns = cols.Split(",").ToList(),
                OrderBy = req.Query["OrderBy"],
                OrderDesc = bool.Parse(req.Query["OrderByDesc"]),
                Page = int.Parse(req.Query["Page"]),
                PageSize = int.Parse(req.Query["PageSize"]),
            });

            return new OkObjectResult(result);
        }

    }
}
