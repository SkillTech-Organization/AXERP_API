using AutoMapper;
using AXERP.API.Domain;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.Functions.Base;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;

namespace AXERP.API.Functions.Transactions
{
    [ForSystem("SQL Server", LogConstants.FUNCTION_LOGGING)]
    public class LogFunctions : BaseFunctions<LogFunctions>
    {
        private readonly IMapper _mapper;
        private readonly UnitOfWorkFactory _unitOfWorkFactory;

        public LogFunctions(
            AxerpLoggerFactory loggerFactory,
            UnitOfWorkFactory unitOfWorkFactory,
            IMapper mapper) : base(loggerFactory)
        {
            _mapper = mapper;
            _unitOfWorkFactory = unitOfWorkFactory;
        }

        [Function(nameof(QueryLogEvents))]
        [OpenApiOperation(operationId: nameof(QueryLogEvents), tags: new[] { "logs" })]
        [OpenApiParameter(name: "Search", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Search in message")]
        [OpenApiParameter(name: "OrderBy", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Order by column, default is TimeStamp")]
        [OpenApiParameter(name: "OrderByDesc", In = ParameterLocation.Query, Required = false, Type = typeof(bool), Description = "Descending order, false by default")]
        [OpenApiParameter(name: "From", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Description = "Query time range from - default is a week before today")]
        [OpenApiParameter(name: "To", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Description = "Query time range to - default is today")]
        [OpenApiParameter(name: "PageSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Returned row count, default is 5")]
        [OpenApiParameter(name: "Page", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page index, starting from 1 (0 will be interpreted as 1), default is 1")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> QueryLogEvents(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            try
            {
                SetLoggerProcessData(UserName);

                var cols = "ProcessId,Result,System,Function,Who,When,Description".Split(",", StringSplitOptions.TrimEntries)?.ToList();

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

                using (var uow = _unitOfWorkFactory.Create())
                {
                    var request = new PagedQueryRequest
                    {
                        Columns = cols,
                        OrderBy = req.Query["OrderBy"] ?? "When",
                        OrderDesc = bool.Parse(req.Query["OrderByDesc"] ?? "false"),
                        Page = page,
                        PageSize = pageSize,
                        Search = req.Query["Search"],
                        SearchOnlyInSelectedColumns = bool.Parse(req.Query["SearchOnlyInSelectedColumns"] ?? "false")
                    };

                    var result = uow.GenericRepository.PagedQuery<LogEvent>(request);

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
