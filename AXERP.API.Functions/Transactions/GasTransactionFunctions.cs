using AutoMapper;
using AXERP.API.Business.Commands;
using AXERP.API.Persistence.Factories;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.Business.SheetProcessors;
using AXERP.API.GoogleHelper.Managers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using Newtonsoft.Json;
using AXERP.API.Persistence.Queries;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace AXERP.API.Functions.Transactions
{
    public class GasTransactionFunctions
    {
        private readonly ILogger<GasTransactionFunctions> _logger;
        private readonly GasTransactionSheetProcessor _gasTransactionSheetProcessor;
        private readonly UnitOfWorkFactory _unitOfWorkFactory;
        private readonly InsertTransactionsCommand _insertTransactionsCommand;
        private readonly DeleteTransactionsCommand _deleteTransactionsCommand;
        private readonly IMapper _mapper;

        public GasTransactionFunctions(
            ILogger<GasTransactionFunctions> logger,
            GasTransactionSheetProcessor gasTransactionSheetProcessor,
            UnitOfWorkFactory unitOfWorkFactory,
            InsertTransactionsCommand insertTransactionsCommand,
            DeleteTransactionsCommand deleteTransactionsCommand,
            IMapper mapper)
        {
            _logger = logger;
            _gasTransactionSheetProcessor = gasTransactionSheetProcessor;
            _unitOfWorkFactory = unitOfWorkFactory;
            _mapper = mapper;
            _insertTransactionsCommand = insertTransactionsCommand;
            _deleteTransactionsCommand = deleteTransactionsCommand;
        }

        [Function(nameof(ImportGasTransactions))]
        [OpenApiOperation(operationId: nameof(ImportGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> ImportGasTransactions([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Importing GasTransactions...");

            try
            {
                _logger.LogInformation("Checking parameters...");

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

                _logger.LogInformation("Fetching rows from GoogleSheet...");

                var sheetService = new GoogleSheetManager(credentials: credentialsJson, format: CredentialsFormats.Text);
                var rows = await sheetService.ReadGoogleSheetRaw(sheet_id, $"{tab_name}{(range?.Length > 0 ? "!" : "")}{range}");

                _logger.LogInformation("Importing GoogleSheet rows...");

                var importResult = _gasTransactionSheetProcessor.ProcessRows(rows, sheetCulture);

                _logger.LogInformation("Updating DataBase with GoogleSheet rows...");

                var result = _insertTransactionsCommand.Execute(importResult);

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

        [Function(nameof(DeleteGasTransactions))]
        [OpenApiOperation(operationId: nameof(DeleteGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiRequestBody("application/json", typeof(DeleteTransactionRequest), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> DeleteGasTransactions(
                [HttpTrigger(AuthorizationLevel.Anonymous, "delete")] HttpRequestData req, [FromBody] DeleteTransactionRequest data)
        {
            try
            {
                if (data == null)
                {
                    throw new Exception("Request is null");
                }

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
            using (var uow = _unitOfWorkFactory.Create())
            {
                return uow.GenericRepository.CountAll<Delivery>();
            }
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
            var queryTemplate = Environment.GetEnvironmentVariable(
                nameof(TransactionQueries.Sql_Query_Paged_GasTransactions_Dynamic_Columns)) ?? TransactionQueries.Sql_Query_Paged_GasTransactions_Dynamic_Columns;
            var countTemplate = Environment.GetEnvironmentVariable(
                nameof(TransactionQueries.Sql_Query_Count_GasTransactions)) ?? TransactionQueries.Sql_Query_Count_GasTransactions;

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
                    var result = uow.GenericRepository.PagedQuery<Delivery>(new PagedQueryRequest
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

    }
}
