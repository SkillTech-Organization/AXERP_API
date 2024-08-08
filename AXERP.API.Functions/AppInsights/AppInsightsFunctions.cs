using AutoMapper;
using AXERP.API.AppInsightsHelper.Managers;
using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.Persistence.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;

namespace AXERP.API.Functions.Transactions
{
    public class AppInsightsFunctions
    {
        private readonly ILogger<AppInsightsFunctions> _logger;
        private readonly IMapper _mapper;
        private readonly AppInsightsManager _appInsightsManager;

        public AppInsightsFunctions(
            ILogger<AppInsightsFunctions> logger,
            AppInsightsManager appInsightsManager,
            IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
            _appInsightsManager = appInsightsManager;
        }

        [Function(nameof(QueryAppInsights))]
        [OpenApiOperation(operationId: nameof(QueryAppInsights), tags: new[] { "gas-transactions" })]
        [OpenApiParameter(name: "Search", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Search in message")]
        [OpenApiParameter(name: "OrderBy", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Order by column, default is TimeStamp")]
        [OpenApiParameter(name: "OrderByDesc", In = ParameterLocation.Query, Required = false, Type = typeof(bool), Description = "Descending order, false by default")]
        [OpenApiParameter(name: "PageSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Returned row count, default is 5")]
        [OpenApiParameter(name: "Page", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page index, starting from 1 (0 will be interpreted as 1), default is 1")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> QueryAppInsights(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var queryTemplate = Environment.GetEnvironmentVariable(
                nameof(TransactionQueries.Sql_Query_Paged_GasTransactions_Dynamic_Columns)) ?? TransactionQueries.Sql_Query_Paged_GasTransactions_Dynamic_Columns;

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
                var result = await _appInsightsManager.QueryLogs(new PagedQueryRequest
                {
                    QueryTemplate = queryTemplate,
                    OrderBy = req.Query["OrderBy"] ?? "TimeStamp",
                    OrderDesc = bool.Parse(req.Query["OrderByDesc"] ?? "false"),
                    Page = page,
                    PageSize = pageSize,
                    Search = req.Query["Search"]
                });

                return new OkObjectResult(result);
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
