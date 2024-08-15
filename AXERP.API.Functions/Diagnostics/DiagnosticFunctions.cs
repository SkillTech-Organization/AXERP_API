using AXERP.API.Domain;
using AXERP.API.Domain.ServiceContracts.Responses.Diagnostics;
using AXERP.API.LogHelper.Factories;
using AXERP.API.LogHelper.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;

namespace AXERP.API.Functions.Diagnostics
{
    public class DiagnosticFunctions
    {
        private readonly AxerpLogger<DiagnosticFunctions> _logger;

        private string userName = "Unknown";

        public DiagnosticFunctions(AxerpLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.Create<DiagnosticFunctions>();
        }

        [Function(nameof(GetVersionInfo))]
        [OpenApiOperation(operationId: nameof(GetVersionInfo), tags: new[] { "diagnostics" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(GetVersionInfoResponse), Description = "The OK response")]
        public IActionResult GetVersionInfo([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.SetData(user: userName, system: "AXERP.API", function: LogConstants.FUNCTION_DIAGNOSTICS);
            _logger.LogInformation("Calling {name}", nameof(GetVersionInfo));

            var res = new GetVersionInfoResponse { NETRunTimeVersion = Environment.Version, AppVersion = GetType().Assembly.GetName().Version };
            _logger.LogInformation("NETRunTimeVersion: {0}, AppVersion: {1}", res.NETRunTimeVersion, res.AppVersion);

            return new OkObjectResult(res);
        }
    }
}
