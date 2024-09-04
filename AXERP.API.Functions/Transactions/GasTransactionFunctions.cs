using AutoMapper;
using AXERP.API.Business.Commands;
using AXERP.API.Business.Queries;
using AXERP.API.Business.SheetProcessors;
using AXERP.API.Domain;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Requests.Transactions;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.Functions.Base;
using AXERP.API.GoogleHelper.Managers;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;
using AXERP.API.Persistence.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace AXERP.API.Functions.Transactions
{
    [ForSystem("Google Sheet, SQL Server", LogConstants.FUNCTION_GOOGLE_SYNC)]
    public class GasTransactionFunctions : BaseFunctions<GasTransactionFunctions>
    {
        private readonly GasTransactionSheetProcessor _gasTransactionSheetProcessor;
        private readonly UnitOfWorkFactory _unitOfWorkFactory;
        private readonly InsertTransactionsCommand _insertTransactionsCommand;
        private readonly DeleteTransactionsCommand _deleteTransactionsCommand;
        private readonly GetGasTransactionCsvQuery _getGasTransactionCsvQuery;
        private readonly GetPagedGasTransactionsQuery _getPagedGasTransactionsQuery;
        private readonly IMapper _mapper;

        public GasTransactionFunctions(
            AxerpLoggerFactory loggerFactory,
            GasTransactionSheetProcessor gasTransactionSheetProcessor,
            UnitOfWorkFactory unitOfWorkFactory,
            InsertTransactionsCommand insertTransactionsCommand,
            DeleteTransactionsCommand deleteTransactionsCommand,
            GetGasTransactionCsvQuery getGasTransactionCsvQuery,
            GetPagedGasTransactionsQuery getPagedGasTransactionsQuery,
            IMapper mapper) : base(loggerFactory)
        {
            _gasTransactionSheetProcessor = gasTransactionSheetProcessor;
            _unitOfWorkFactory = unitOfWorkFactory;
            _mapper = mapper;
            _insertTransactionsCommand = insertTransactionsCommand;
            _deleteTransactionsCommand = deleteTransactionsCommand;
            _getGasTransactionCsvQuery = getGasTransactionCsvQuery;
            _getPagedGasTransactionsQuery = getPagedGasTransactionsQuery;
        }

        [Function(nameof(ImportGasTransactions))]
        [OpenApiOperation(operationId: nameof(ImportGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> ImportGasTransactions([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            try
            {
                SetLoggerProcessData(req);

                _logger.LogInformation("Importing GasTransactions...");
                _logger.LogInformation("Checking parameters...");

                var credentialsJson = EnvironmentHelper.TryGetParameter("GoogleCredentials");

                var sheet_id = EnvironmentHelper.TryGetParameter("BulkDeliveriesSheetDataSheetId");
                var tab_name = EnvironmentHelper.TryGetParameter("BulkDeliveriesSheetDataGasTransactionsTab");
                var range = EnvironmentHelper.TryGetOptionalParameter("BulkDeliveriesSheetDataGasTransactionRange");
                var sheetCulture = Environment.GetEnvironmentVariable("SheetCulture") ?? "fr-FR";

                _logger.LogInformation("Fetching rows from GoogleSheet...");

                var sheetService = new GoogleSheetManager(credentials: credentialsJson, format: CredentialsFormats.Text);
                var rows = await sheetService.ReadGoogleSheetRaw(sheet_id, $"{tab_name}{(range?.Length > 0 ? "!" : "")}{range}");

                _logger.LogInformation("Google Sheet unprocessed rowcount (including header): {0}", rows.Count);
                _logger.LogInformation("Importing GoogleSheet rows...");

                _gasTransactionSheetProcessor.SetLoggerProcessData(UserName, id: _logger.ProcessId);
                var importResult = _gasTransactionSheetProcessor.ProcessRows(rows, sheetCulture);

                _logger.LogInformation("Updating DataBase with GoogleSheet rows...");

                _insertTransactionsCommand.SetLoggerProcessData(UserName, id: _logger.ProcessId);
                var result = _insertTransactionsCommand.Execute(importResult);

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

        [Function(nameof(DeleteGasTransactions))]
        [OpenApiOperation(operationId: nameof(DeleteGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiRequestBody("application/json", typeof(DeleteTransactionRequest), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> DeleteGasTransactions(
                [HttpTrigger(AuthorizationLevel.Anonymous, "delete")] HttpRequestData req, [FromBody] DeleteTransactionRequest data)
        {
            try
            {
                SetLoggerProcessData(req);

                if (data == null)
                {
                    throw new Exception("Request is null");
                }

                _deleteTransactionsCommand.SetLoggerProcessData(UserName, id: _logger.ProcessId);
                var response = _deleteTransactionsCommand.Execute(data);
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting transaction(s)");
                var res = new ObjectResult(new BaseResponse
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

        [Function(nameof(CountGasTransactions))]
        [OpenApiOperation(operationId: nameof(CountGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public int CountGasTransactions(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            SetLoggerProcessData(base.UserName);

            using (var uow = _unitOfWorkFactory.Create())
            {
                _logger.LogInformation("Counting Gas Transactions...");

                var result = uow.GenericRepository.CountAll<Delivery>();
                _logger.LogInformation("Gas Transaction count: {0}", result);

                return result;
            }
        }

        [Function(nameof(QueryGasTransactions))]
        [OpenApiOperation(operationId: nameof(QueryGasTransactions), tags: new[] { "gas-transactions" })]
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
            SetLoggerProcessData(req);

            _logger.LogInformation("Querying GasTransactions...");
            _logger.LogInformation("Checking parameters...");

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

            try
            {
                using (var uow = _unitOfWorkFactory.Create())
                {
                    var request = new PagedQueryRequest
                    {
                        Columns = cols,
                        OrderBy = req.Query["OrderBy"] ?? "DeliveryID",
                        OrderDesc = bool.Parse(req.Query["OrderByDesc"] ?? "false"),
                        Page = page,
                        PageSize = pageSize,
                        Search = req.Query["Search"],
                        SearchOnlyInSelectedColumns = bool.Parse(req.Query["SearchOnlyInSelectedColumns"] ?? "false")
                    };

                    _logger.LogInformation("Executing query with request: {0}", Newtonsoft.Json.JsonConvert.SerializeObject(request));

                    var result = uow.GenericRepository.PagedQuery<Delivery>(request);

                    _logger.LogInformation("Query finished. Total rows in DB: {0}, queried rows: {1}", result.TotalCount, result.DataCount);

                    return new OkObjectResult(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while querying GasTransactions");
                var res = new ObjectResult(new GenericPagedQueryResponse<dynamic>
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

        [Function(nameof(QueryPagedGasTransactions))]
        [OpenApiOperation(operationId: nameof(QueryPagedGasTransactions), tags: new[] { "customer-ui" })]
        [OpenApiParameter(name: "Customer", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "FromDate", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime))]
        [OpenApiParameter(name: "ToDate", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime))]
        [OpenApiParameter(name: "SearchOnlyInSelectedColumns", In = ParameterLocation.Query, Required = false, Type = typeof(bool), Description = "Search only in columns provided in the Columns parameter - ignored if Search is written for specific column")]
        [OpenApiParameter(name: "OrderBy", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Order by column, default is DeliveryID")]
        [OpenApiParameter(name: "OrderByDesc", In = ParameterLocation.Query, Required = false, Type = typeof(bool), Description = "Descending order, false by default")]
        [OpenApiParameter(name: "PageSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Returned row count, default is 5")]
        [OpenApiParameter(name: "Page", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page index, starting from 1 (0 will be interpreted as 1), default is 1")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public IActionResult QueryPagedGasTransactions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            SetLoggerProcessData(req);

            _logger.LogInformation("Querying GasTransactions...");
            _logger.LogInformation("Checking parameters...");

            var customerId = req.Query["Customer"];

            var page = int.Parse(req.Query["Page"] ?? "0");
            if (page < 0)
            {
                page = 0;
            }

            var pageSize = int.Parse(req.Query["PageSize"] ?? "5");
            if (pageSize <= 0)
            {
                pageSize = 5;
            }

            var isFrom = DateTime.TryParse(req.Query["FromDate"], out DateTime FromDate);
            var isTo = DateTime.TryParse(req.Query["ToDate"], out DateTime ToDate);

            try
            {
                using (var uow = _unitOfWorkFactory.Create())
                {
                    var request = new GetPagedGasTransactionsQueryRequest
                    {
                        OrderBy = req.Query["OrderBy"] ?? "DeliveryID",
                        OrderDesc = bool.Parse(req.Query["OrderByDesc"] ?? "false"),
                        Page = page,
                        PageSize = pageSize,
                        FromDate = isFrom ? FromDate : default,
                        ToDate = isTo ? ToDate : default,
                        Customer = customerId
                    };

                    _logger.LogInformation("Executing query with request: {0}", Newtonsoft.Json.JsonConvert.SerializeObject(request));

                    var paged = _getPagedGasTransactionsQuery.Execute(request);

                    var result = new GenericPagedQueryResponse<Delivery>
                    {
                        Columns = typeof(Delivery).GetColumnDatas(),
                        Data = paged,
                        PageIndex = page,
                        PageSize = pageSize,
                        TotalCount = paged.TotalCount,
                        HttpStatusCode = HttpStatusCode.OK
                    };

                    _logger.LogInformation("Query finished. Total rows in DB: {0}, queried rows: {1}", result.TotalCount, result.DataCount);

                    return new OkObjectResult(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while querying GasTransactions");
                var res = new ObjectResult(new GenericPagedQueryResponse<dynamic>
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

        [Function(nameof(GasTransactionCsv))]
        [OpenApiOperation(operationId: nameof(GasTransactionCsv), tags: new[] { "gas-transactions" })]
        [OpenApiParameter(name: "FromDate", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime))]
        [OpenApiParameter(name: "ToDate", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime))]
        [OpenApiParameter(name: "OrderBy", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "OrderByDesc", In = ParameterLocation.Query, Required = false, Type = typeof(bool))]
        [OpenApiParameter(name: "Columns", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        public HttpResponseData GasTransactionCsv(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            SetLoggerProcessData(req);

            _logger.LogInformation("Exporting GasTransactions to CSV...");
            _logger.LogInformation("Checking parameters...");

            var isFrom = DateTime.TryParse(req.Query["FromDate"], out DateTime FromDate);
            var isTo = DateTime.TryParse(req.Query["ToDate"], out DateTime ToDate);
            if (isTo)
            {
                ToDate = ToDate.Date.AddDays(1).AddSeconds(-1);
            }
            var orderBy = req.Query["OrderBy"] ?? "DeliveryID";
            var orderDesc = bool.Parse(req.Query["OrderByDesc"] ?? "false");

            var cols = req.Query["Columns"]?.ToString()?.Split(",", StringSplitOptions.TrimEntries)?.ToList() ?? new List<string>();

            try
            {
                var bytes = _getGasTransactionCsvQuery.Execute(new Domain.ServiceContracts.Requests.Transactions.GasTransactionCsvRequest
                {
                    FromDate = FromDate,
                    ToDate = ToDate,
                    Order = orderBy,
                    OrderDesc = orderDesc,
                    Columns = cols
                });

                _logger.LogInformation("CSV export finished.");

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.WriteBytes(bytes);
                response.Headers.Add("Content-Type", "text/csv");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while exporting GasTransactions CSV");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString(ex.Message);
                return response;
            }
        }
    }
}
