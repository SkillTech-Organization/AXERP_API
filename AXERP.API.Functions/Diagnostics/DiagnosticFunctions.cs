using AXERP.API.Domain;
using AXERP.API.Domain.ServiceContracts.Responses.Diagnostics;
using AXERP.API.Functions.Base;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Factories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;

namespace AXERP.API.Functions.Diagnostics
{
    [ForSystem("AXERP.API", LogConstants.FUNCTION_DIAGNOSTICS)]
    public class DiagnosticFunctions : BaseFunctions<DiagnosticFunctions>
    {
        public DiagnosticFunctions(AxerpLoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        [Function(nameof(GetVersionInfo))]
        [OpenApiOperation(operationId: nameof(GetVersionInfo), tags: new[] { "diagnostics" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(GetVersionInfoResponse), Description = "The OK response")]
        public IActionResult GetVersionInfo([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            SetLoggerProcessData(req);

            _logger.LogInformation("Calling {name}", nameof(GetVersionInfo));

            var res = new GetVersionInfoResponse { NETRunTimeVersion = Environment.Version, AppVersion = GetType().Assembly.GetName().Version };
            _logger.LogInformation("NETRunTimeVersion: {0}, AppVersion: {1}", res.NETRunTimeVersion, res.AppVersion);

            return new OkObjectResult(res);
        }
    }
}
