using AutoMapper;
using AXERP.API.AppInsightsHelper.Managers;
using AXERP.API.AppInsightsHelper.Models;
using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.Persistence.Queries;
using AXERP.API.Persistence.Utils;
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
        [OpenApiOperation(operationId: nameof(QueryAppInsights), tags: new[] { "logs" })]
        [OpenApiParameter(name: "Search", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Search in message")]
        [OpenApiParameter(name: "OrderBy", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Order by column, default is TimeStamp")]
        [OpenApiParameter(name: "OrderByDesc", In = ParameterLocation.Query, Required = false, Type = typeof(bool), Description = "Descending order, false by default")]
        [OpenApiParameter(name: "From", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Description = "Query time range from - default is a week before today")]
        [OpenApiParameter(name: "To", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Description = "Query time range to - default is today")]
        [OpenApiParameter(name: "PageSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Returned row count, default is 5")]
        [OpenApiParameter(name: "Page", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page index, starting from 1 (0 will be interpreted as 1), default is 1")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> QueryAppInsights(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            try
            {
                var workSpaceID = Environment.GetEnvironmentVariable("AppInsightsWorkspaceID");

                if (string.IsNullOrWhiteSpace(workSpaceID))
                {
                    var msg = "AppInsightsWorkspaceID environment variable is missing.";
                    _logger.LogError(msg);
                    throw new Exception(msg);
                }

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

                if (!DateTime.TryParse(req.Query["From"], out DateTime fromDate))
                {
                    fromDate = DateTime.Now - TimeSpan.FromDays(7);
                }
                if (!DateTime.TryParse(req.Query["To"], out DateTime toDate))
                {
                    toDate = DateTime.Now;
                }

                var result = await _appInsightsManager.QueryLogs(new PagedAppInsightsQueryRequest
                {
                    OrderBy = req.Query["OrderBy"] ?? "TimeStamp",
                    OrderDesc = bool.Parse(req.Query["OrderByDesc"] ?? "false"),
                    Page = page,
                    PageSize = pageSize,
                    From = fromDate,
                    To = toDate
                }, workSpaceID);

                var response = new GenericPagedQueryResponse<AppInsightsLogEntry>
                {
                    TotalCount = result.Item1,
                    Data = result.Item2,
                    HttpStatusCode = HttpStatusCode.OK,
                    PageIndex = page,
                    PageSize = pageSize,
                    Columns = typeof(AppInsightsLogEntry).GetColumnDatas(),
                };

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
